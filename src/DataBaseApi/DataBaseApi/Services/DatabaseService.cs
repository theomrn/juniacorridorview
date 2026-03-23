using Dapper;
using DataBaseApi.Controllers;
using MySqlConnector;
using System.Data;
using System.Text.Json;

namespace DataBaseApi.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly string _imagesFolder;
    private readonly string _previewsFolder;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        _imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        _previewsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "previews");
        if (!Directory.Exists(_imagesFolder)) Directory.CreateDirectory(_imagesFolder);
        if (!Directory.Exists(_previewsFolder)) Directory.CreateDirectory(_previewsFolder);
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    private async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var relPath = Path.Combine(folder == "images" ? "images" : "previews", fileName).Replace("\\", "/");
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder == "images" ? "images" : "previews", fileName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);
        return relPath; // path relative to wwwroot
    }

    private void TryDeleteFile(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        try
        {
            if (File.Exists(full)) File.Delete(full);
        }
        catch
        {
            // log if needed, ignore deletion errors to avoid throwing
        }
    }

    public async Task<IEnumerable<string>> GetTablesAsync()
    {
        using var conn = CreateConnection();
        var rows = await conn.QueryAsync("SHOW TABLES");
        return rows.Select(r => ((IDictionary<string, object>)r).Values.First().ToString());
    }

    public async Task<IEnumerable<object>> GetAllPicturesAsync()
    {
        using var conn = CreateConnection();
        var rows = await conn.QueryAsync("SELECT id_pictures FROM Pictures");
        return rows.Select(r => new { id_pictures = ((dynamic)r).id_pictures });
    }

    public async Task<int> InsertImageFileAsync(int id_rooms, IFormFile file)
    {
        var relPath = await SaveFileAsync(file, "images");
        using var conn = CreateConnection();
        var sql = "INSERT INTO Pictures (id_rooms, picture_path) VALUES (@IdRooms, @Path)";
        var res = await conn.ExecuteAsync(sql, new { IdRooms = id_rooms, Path = relPath });
        return res;
    }

    public async Task<int> UpdateImageFileAsync(int id_pictures, IFormFile file)
    {
        var relPath = await SaveFileAsync(file, "images");
        using var conn = CreateConnection();
        // delete old file if exists
        var old = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT picture_path FROM Pictures WHERE id_pictures = @Id", new { Id = id_pictures });
        if (old != null && old.picture_path != null)
        {
            TryDeleteFile((string)old.picture_path);
        }
        var sql = "UPDATE Pictures SET picture_path = @Path WHERE id_pictures = @Id";
        var res = await conn.ExecuteAsync(sql, new { Path = relPath, Id = id_pictures });
        return res;
    }

    public async Task<int> DeleteImageAsync(int id_pictures)
    {
        using var conn = CreateConnection();
        // fetch path to delete file
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT picture_path FROM Pictures WHERE id_pictures = @Id", new { Id = id_pictures });
        if (row != null && row.picture_path != null)
        {
            TryDeleteFile((string)row.picture_path);
        }
        var sql = "DELETE FROM Pictures WHERE id_pictures = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id_pictures });
    }

    public async Task<string?> FetchImageByIdAsync(int id)
    {
        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT picture_path FROM Pictures WHERE id_pictures = @Id", new { Id = id });
        if (row == null) return (null);
        var relativePath = (string)row.picture_path;
        if (relativePath == null) return (null);
        return row.picture_path;
    }

    // Rooms
    public async Task<IEnumerable<dynamic>> GetRoomsAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT * FROM Rooms ORDER BY name ASC");
    }

    public async Task<dynamic?> GetRoomByIdAsync(int id_rooms)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync("SELECT * FROM Rooms WHERE id_rooms = @Id", new { Id = id_rooms });
    }

    public async Task<dynamic?> GetRoomIdByIdFloorAsync(int id_floors)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync("SELECT id_rooms FROM Rooms WHERE id_floors = @Id", new { Id = id_floors });
    }

    public async Task<int> AddRoomAsync(string name, string number, int id_floors, double? plan_x, double? plan_y)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Rooms (name, number, id_floors, plan_x, plan_y) VALUES (@Name, @Number, @IdFloors, @Px, @Py); SELECT LAST_INSERT_ID();";
        var id = await conn.ExecuteScalarAsync<int>(sql, new { Name = name, Number = number, IdFloors = id_floors, Px = plan_x, Py = plan_y });
        return id;
    }

    public async Task<int> UpdateRoomAsync(int id_rooms, string name, string number, int id_floors, double? plan_x, double? plan_y)
    {
        using var conn = CreateConnection();
        var sql = "UPDATE Rooms SET name = @Name, number = @Number, id_floors = @IdFloors, plan_x = @Px, plan_y = @Py WHERE id_rooms = @Id";
        return await conn.ExecuteAsync(sql, new { Name = name, Number = number, IdFloors = id_floors, Px = plan_x, Py = plan_y, Id = id_rooms });
    }

    public async Task<int> DeleteRoomAsync(int id_rooms)
    {
        using var conn = CreateConnection();

        // delete pictures files and records
        var pictures = await conn.QueryAsync<dynamic>("SELECT id_pictures, picture_path FROM Pictures WHERE id_rooms = @Id", new { Id = id_rooms });
        foreach (var p in pictures)
        {
            if (p.picture_path != null)
            {
                TryDeleteFile((string)p.picture_path);
            }
        }
        await conn.ExecuteAsync("DELETE FROM Pictures WHERE id_rooms = @Id", new { Id = id_rooms });

        // delete room preview file and record
        var preview = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT preview_path FROM Room_Previews WHERE id_rooms = @Id", new { Id = id_rooms });
        if (preview != null && preview.preview_path != null)
        {
            TryDeleteFile((string)preview.preview_path);
            await conn.ExecuteAsync("DELETE FROM Room_Previews WHERE id_rooms = @Id", new { Id = id_rooms });
        }

        // delete infospots images related to pictures in this room
        var infospots = await conn.QueryAsync<dynamic>("SELECT image_path FROM Info_Popup WHERE id_pictures IN (SELECT id_pictures FROM Pictures WHERE id_rooms = @Id)", new { Id = id_rooms });
        foreach (var ip in infospots)
        {
            if (ip.image_path != null) TryDeleteFile((string)ip.image_path);
        }
        await conn.ExecuteAsync("DELETE FROM Info_Popup WHERE id_pictures IN (SELECT id_pictures FROM Pictures WHERE id_rooms = @Id)", new { Id = id_rooms });

        // finally delete room
        return await conn.ExecuteAsync("DELETE FROM Rooms WHERE id_rooms = @Id", new { Id = id_rooms });
    }

    public async Task<int> UpdateRoomVisibilityAsync(int id_rooms, bool hidden)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("UPDATE Rooms SET hidden = @Hidden WHERE id_rooms = @Id", new { Hidden = hidden, Id = id_rooms });
    }

    // Room previews
    public async Task<int> InsertRoomPreviewFileAsync(int id_rooms, IFormFile file)
    {
        var relPath = await SaveFileAsync(file, "previews");
        using var conn = CreateConnection();
        var exists = await conn.QueryFirstOrDefaultAsync<int?>("SELECT COUNT(1) FROM Room_Previews WHERE id_rooms = @Id", new { Id = id_rooms });
        if (exists.GetValueOrDefault() > 0)
        {
            // delete old file
            var old = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT preview_path FROM Room_Previews WHERE id_rooms = @Id", new { Id = id_rooms });
            if (old != null && old.preview_path != null) TryDeleteFile((string)old.preview_path);
            return await conn.ExecuteAsync("UPDATE Room_Previews SET preview_path = @Path WHERE id_rooms = @Id", new { Path = relPath, Id = id_rooms });
        }
        else
        {
            return await conn.ExecuteAsync("INSERT INTO Room_Previews (id_rooms, preview_path) VALUES (@Id, @Path)", new { Id = id_rooms, Path = relPath });
        }
    }

    public async Task<int> DeleteRoomPreviewAsync(int id_rooms)
    {
        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT preview_path FROM Room_Previews WHERE id_rooms = @Id", new { Id = id_rooms });
        if (row != null && row.preview_path != null)
        {
            TryDeleteFile((string)row.preview_path);
        }
        var sql = "DELETE FROM Room_Previews WHERE id_rooms = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id_rooms });
    }

    public async Task<string?> GetRoomPreviewAsync(int id_rooms)
    {
        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT preview_path FROM Room_Previews WHERE id_rooms = @Id LIMIT 1", new { Id = id_rooms });
        if (row == null) return (null);
        var rel = (string)row.preview_path;
        //var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rel.Replace("/", Path.DirectorySeparatorChar.ToString()));
        //if (!File.Exists(full)) return (null);
        return rel;
        //return (await File.ReadAllBytesAsync(full), Path.GetExtension(full));
    }

    // Links
    public async Task<IEnumerable<dynamic>> RetrieveLinkByIdPictureAsync(int id_pictures)
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT * FROM Links WHERE id_pictures = @Id", new { Id = id_pictures });
    }

    public async Task<int> InsertLinkAsync(int id_pictures, double posX, double posY, double posZ, int id_pictures_destination)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Links (id_pictures, position_x, position_y, position_z, id_pictures_destination) VALUES (@P, @X, @Y, @Z, @Dest)";
        return await conn.ExecuteAsync(sql, new { P = id_pictures, X = posX, Y = posY, Z = posZ, Dest = id_pictures_destination });
    }

    public async Task<int> UpdateLinkAsync(int id_links, int id_pictures, double posX, double posY, double posZ, int id_pictures_destination)
    {
        using var conn = CreateConnection();
        var sql = "UPDATE Links SET id_pictures = @P, position_x = @X, position_y = @Y, position_z = @Z, id_pictures_destination = @Dest WHERE id_links = @Id";
        return await conn.ExecuteAsync(sql, new { P = id_pictures, X = posX, Y = posY, Z = posZ, Dest = id_pictures_destination, Id = id_links });
    }

    public async Task<int> DeleteLinkAsync(int id_links)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Links WHERE id_links = @Id", new { Id = id_links });
    }

    // Info popups
    public async Task<int> InsertInfoPopUpAsync(int id_pictures, double posX, double posY, double posZ, IFormFile? imageFile)
    {
        string? path = null;
        if (imageFile != null) path = await SaveFileAsync(imageFile, "images");
        using var conn = CreateConnection();
        var sql = "INSERT INTO Info_Popup (id_pictures, position_x, position_y, position_z, image_path) VALUES (@Id, @X, @Y, @Z, @Img); SELECT LAST_INSERT_ID();";
        return await conn.ExecuteScalarAsync<int>(sql, new { Id = id_pictures, X = posX, Y = posY, Z = posZ, Img = path });
    }

    public async Task<int> InsertInfoPopUpTranslationAsync(int id_info_popup, string text, string title, int id_languages, int? id_visitor_type)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Info_popup_translation (id_info_popup, id_languages, title, text, id_visitor_type) VALUES (@Id, @Lang, @Title, @Text, @VisitorType)";
        return await conn.ExecuteAsync(sql, new { Id = id_info_popup, Lang = id_languages, Title = title, Text = text, VisitorType = id_visitor_type });
    }

    public async Task<IEnumerable<dynamic>> RetrieveInfoPopUpByIdPictureAsync(int id_pictures, int? id_languages = null)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT
                ip.id_info_popup,
                ip.id_pictures,
                ip.position_x,
                ip.position_y,
                ip.position_z,
                ip.image_path,
                ipt.id_languages,
                ipt.title,
                ipt.text,
                ipt.id_visitor_type
            FROM Info_Popup ip
            LEFT JOIN Info_popup_translation ipt
                ON ip.id_info_popup = ipt.id_info_popup
            WHERE ip.id_pictures = @Id";

        if (id_languages.HasValue)
        {
            sql += " AND (ipt.id_languages = @Lang OR ipt.id_languages IS NULL)";
            return await conn.QueryAsync(sql, new { Id = id_pictures, Lang = id_languages.Value });
        }

        return await conn.QueryAsync(sql, new { Id = id_pictures });
    }

    public async Task<int> UpdateInfospotAsync(int id_info_popup, int id_pictures, double posX, double posY, double posZ, string text, string title, IFormFile? imageFile, int? id_languages, int? id_visitor_type)
    {
        using var conn = CreateConnection();

        // Update Info_Popup table (position and image)
        if (imageFile != null)
        {
            // Delete old image if exists
            var old = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT image_path FROM Info_Popup WHERE id_info_popup = @Id", new { Id = id_info_popup });
            if (old != null && old.image_path != null)
            {
                TryDeleteFile((string)old.image_path);
            }
            var path = await SaveFileAsync(imageFile, "images");
            var sqlInfoPopup = "UPDATE Info_Popup SET id_pictures = @IdP, position_x = @X, position_y = @Y, position_z = @Z, image_path = @Img WHERE id_info_popup = @Id";
            await conn.ExecuteAsync(sqlInfoPopup, new { IdP = id_pictures, X = posX, Y = posY, Z = posZ, Img = path, Id = id_info_popup });
        }
        else
        {
            var sqlInfoPopup = "UPDATE Info_Popup SET id_pictures = @IdP, position_x = @X, position_y = @Y, position_z = @Z WHERE id_info_popup = @Id";
            await conn.ExecuteAsync(sqlInfoPopup, new { IdP = id_pictures, X = posX, Y = posY, Z = posZ, Id = id_info_popup });
        }

        // Update Info_popup_translation table
        if (id_languages.HasValue)
        {
            // Check if translation exists for this language
            var existingTranslation = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT 1 FROM Info_popup_translation WHERE id_info_popup = @Id AND id_languages = @Lang",
                new { Id = id_info_popup, Lang = id_languages.Value });

            if (existingTranslation != null)
            {
                var sqlTranslation = "UPDATE Info_popup_translation SET title = @Title, text = @Text, id_visitor_type = @VisitorType WHERE id_info_popup = @Id AND id_languages = @Lang";
                return await conn.ExecuteAsync(sqlTranslation, new { Title = title, Text = text, VisitorType = id_visitor_type, Id = id_info_popup, Lang = id_languages.Value });
            }
            else
            {
                // Insert new translation if it doesn't exist
                return await InsertInfoPopUpTranslationAsync(id_info_popup, text, title, id_languages.Value, id_visitor_type);
            }
        }
        else
        {
            // Update first translation found (backward compatibility)
            var sqlTranslation = "UPDATE Info_popup_translation SET title = @Title, text = @Text, id_visitor_type = @VisitorType WHERE id_info_popup = @Id LIMIT 1";
            return await conn.ExecuteAsync(sqlTranslation, new { Title = title, Text = text, VisitorType = id_visitor_type, Id = id_info_popup });
        }
    }

    public void DeletePlansFiles(List<string> listFloorsPath)
    {
        try
        {
            foreach (string path in listFloorsPath)
            {
                TryDeleteFile(path);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<string>> GetPlansToDelete(int idBuilding)
    {
        List<string> listFloorsPath = new List<string>();

        try
        {
            listFloorsPath = await GetPlanPathByIdBuildingAsync(idBuilding);
            foreach (string path in listFloorsPath)
            {
                listFloorsPath.Add(path);
            }
            return listFloorsPath;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int> DeleteInfoPopUpAsync(int id_info_popup)
    {
        using var conn = CreateConnection();
        // delete associated image file if present
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT image_path FROM Info_Popup WHERE id_info_popup = @Id", new { Id = id_info_popup });
        if (row != null && row.image_path != null)
        {
            TryDeleteFile((string)row.image_path);
        }
        return await conn.ExecuteAsync("DELETE FROM Info_Popup WHERE id_info_popup = @Id", new { Id = id_info_popup });
    }

    // Get all translations for a specific InfoPopup
    public async Task<IEnumerable<dynamic>> GetInfoPopUpTranslationsAsync(int id_info_popup)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT
                ipt.id_info_popup,
                ipt.id_languages,
                l.name_language,
                ipt.title,
                ipt.text,
                ipt.id_visitor_type,
                vt.name_visitor_type
            FROM Info_popup_translation ipt
            LEFT JOIN Languages l ON ipt.id_languages = l.id_language
            LEFT JOIN Visitor_type vt ON ipt.id_visitor_type = vt.id_visitor_type
            WHERE ipt.id_info_popup = @Id";
        return await conn.QueryAsync(sql, new { Id = id_info_popup });
    }

    // Delete a specific translation
    public async Task<int> DeleteInfoPopUpTranslationAsync(int id_info_popup, int id_languages)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync(
            "DELETE FROM Info_popup_translation WHERE id_info_popup = @Id AND id_languages = @Lang",
            new { Id = id_info_popup, Lang = id_languages });
    }

    // Get InfoPopup base info (without translations)
    public async Task<dynamic?> GetInfoPopUpByIdAsync(int id_info_popup)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM Info_Popup WHERE id_info_popup = @Id",
            new { Id = id_info_popup });
    }

    // Tours & steps
    public async Task<IEnumerable<dynamic>> GetToursAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT * FROM Tours");
    }

    public async Task<IEnumerable<dynamic>> GetTourStepsWithRoomInfoAsync(int tourId)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT Tour_Steps.*, Rooms.name as room_name, Rooms.number as room_number, Rooms.hidden as room_hidden
                    FROM Tour_Steps
                    JOIN Rooms ON Tour_Steps.id_rooms = Rooms.id_rooms
                    WHERE Tour_Steps.id_tours = @Id
                    ORDER BY Tour_Steps.step_number";
        return await conn.QueryAsync(sql, new { Id = tourId });
    }

    public async Task<int> UpdateTourStepsAsync(int id_tours, IEnumerable<dynamic> steps, string? title, string? description)
    {
        var connection = (MySqlConnection)CreateConnection();
        await connection.OpenAsync();
        using var tran = await connection.BeginTransactionAsync();
        try
        {
            // update tour meta
            await connection.ExecuteAsync("UPDATE Tours SET title = @Title, description = @Desc WHERE id_tours = @Id", new { Title = title, Desc = description, Id = id_tours }, tran);

            // remove existing steps
            await connection.ExecuteAsync("DELETE FROM Tour_Steps WHERE id_tours = @Id", new { Id = id_tours }, tran);

            // insert new steps
            var insertSql = "INSERT INTO Tour_Steps (id_tours, id_rooms, step_number) VALUES (@IdTour, @IdRoom, @Num)";
            foreach (var s in steps)
            {
                await InsertOneStepAsync(connection, s, insertSql, id_tours, tran);
            }
            await tran.CommitAsync();
            return 1;
        }
        catch
        {
            try { await tran.RollbackAsync(); } catch { }
            throw;
        }
    }

    public async Task<int> InsertOneStepAsync(MySqlConnection connection,dynamic s,string insertSql, int id_tours, MySqlTransaction tran)
    {
        var elem = (JsonElement)s;

        string? idStep = null;
        if (elem.TryGetProperty("id_tour_steps", out var pStep))
            idStep = pStep.GetString();

        if (string.IsNullOrEmpty(idStep) || idStep.StartsWith("new_"))
            idStep = Guid.NewGuid().ToString();

        int idRoom = int.Parse(elem.GetProperty("id_rooms").GetString());
        int stepNumber = elem.GetProperty("step_number").GetInt32();
        
        return await connection.ExecuteAsync(insertSql, new
        {
            //IdStep = idStep,
            IdTour = id_tours,
            IdRoom = idRoom,
            Num = stepNumber
        }, tran);
    }

    public async Task<int> AddTourStepAsync(int id_tours, dynamic step)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Tour_Steps (id_tours, id_rooms, step_number) VALUES (@IdTour, @IdRoom, @Num)";
        return await conn.ExecuteAsync(sql, new { IdTour = id_tours, IdRoom = step.id_rooms, Num = step.step_number });
    }

    public async Task<int> CreateTourWithStepsAsync(
        string title,
        string description,
        IEnumerable<StepDto> steps)
    {
        using var conn = CreateConnection();

        var sql = "INSERT INTO Tours (title, description) VALUES (@Title, @Desc); SELECT LAST_INSERT_ID();";
        var tourId = await conn.ExecuteScalarAsync<int>(sql, new { Title = title, Desc = description });

        var insertStepSql =
            "INSERT INTO Tour_Steps (id_tours, id_rooms, step_number)" +
            " VALUES (@IdTour, @IdRoom, @Num)";

        foreach (var s in steps)
        {
            //var idStep = Guid.NewGuid().ToString();

            await conn.ExecuteAsync(insertStepSql, new
            {
                //IdStep = idStep,
                IdTour = tourId,
                IdRoom = s.id_rooms,
                Num = s.step_number
            });
        }

        return tourId;
    }


    public async Task<int> DeleteTourAsync(int id_tours)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Tour_Steps WHERE id_tours = @Id", new { Id = id_tours });
        return await conn.ExecuteAsync("DELETE FROM Tours WHERE id_tours = @Id", new { Id = id_tours });
    }

    public async Task<int> UpdateTourVisibilityAsync(int id_tours, bool hidden)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("UPDATE Tours SET hidden = @Hidden WHERE id_tours = @Id", new { Hidden = hidden, Id = id_tours });
    }

    // Room/picture helpers
    public async Task<int?> GetRoomIdByPictureIdAsync(int id_pictures)
    {
        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT id_rooms FROM Pictures WHERE id_pictures = @Id", new { Id = id_pictures });
        if (row == null) return null;
        return (int)row.id_rooms;
    }

    public async Task<IEnumerable<dynamic>> GetPicturesByRoomIdAsync(int id_rooms)
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT id_pictures FROM Pictures WHERE id_rooms = @Id", new { Id = id_rooms });
    }

    public async Task<dynamic?> GetFirstPictureByRoomIdAsync(int id_rooms)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync("SELECT id_pictures FROM Pictures WHERE id_rooms = @Id LIMIT 1", new { Id = id_rooms });
    }

    // Buildings
    public async Task<IEnumerable<dynamic>> GetBuildingsAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT id_buildings, name FROM Buildings");
    }

    public async Task<int> AddBuildingAsync(string name)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Buildings (name) VALUES (@Name); SELECT LAST_INSERT_ID();";
        return await conn.ExecuteScalarAsync<int>(sql, new { Name = name });
    }

    public async Task<int> UpdateBuildingAsync(int id_buildings, string name)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("UPDATE Buildings SET name = @Name WHERE id_buildings = @Id", new { Name = name, Id = id_buildings });
    }

    public async Task<int> DeleteBuildingAsync(int id_buildings)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Buildings WHERE id_buildings = @Id", new { Id = id_buildings });
    }

    // Floors
    public async Task<IEnumerable<dynamic>> GetFloorsAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT * FROM Floors ORDER BY name ASC");
    }

    public async Task<dynamic?> GetFloorByIdAsync(int id_floors)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync("SELECT * FROM Floors WHERE id_floors = @Id", new { Id = id_floors });
    }

    public async Task<dynamic?> GetFloorIdByIdBuildingAsync(int id_buildings)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync("SELECT id_floors FROM Floors WHERE id_buildings = @Id", new { Id = id_buildings });
    }

    public async Task<dynamic?> GetPlanPathByIdBuildingAsync(int id_building)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync("SELECT plan_path FROM Floors WHERE id_buildings = @Id", new { Id = id_building });
    }

    public async Task<int> AddFloorAsync(string name, int id_buildings, IFormFile? planFile)
    {
        string? path = null;
        if (planFile != null) path = await SaveFileAsync(planFile, "previews");
        using var conn = CreateConnection();
        var sql = "INSERT INTO Floors (name, id_buildings, plan_path) VALUES (@Name, @Building, @Plan); SELECT LAST_INSERT_ID();";
        return await conn.ExecuteScalarAsync<int>(sql, new { Name = name, Building = id_buildings, Plan = path });
    }

    public async Task<int> UpdateFloorAsync(int id_floors, string name, int id_buildings, IFormFile? planFile)
    {
        using var conn = CreateConnection();
        if (planFile != null)
        {
            var path = await SaveFileAsync(planFile, "previews");
            // delete old plan file if present
            var old = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT plan_path FROM Floors WHERE id_floors = @Id", new { Id = id_floors });
            if (old != null && old.plan_path != null) TryDeleteFile((string)old.plan_path);
            return await conn.ExecuteAsync("UPDATE Floors SET name = @Name, id_buildings = @Building, plan_path = @Plan WHERE id_floors = @Id", new { Name = name, Building = id_buildings, Plan = path, Id = id_floors });
        }
        else
        {
            return await conn.ExecuteAsync("UPDATE Floors SET name = @Name, id_buildings = @Building WHERE id_floors = @Id", new { Name = name, Building = id_buildings, Id = id_floors });
        }
    }

    public async Task<int> DeleteFloorAsync(int id_floors)
    {
        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT plan_path FROM Floors WHERE id_floors = @Id", new { Id = id_floors });
        if (row != null && row.plan_path != null)
        {
            TryDeleteFile((string)row.plan_path);
        }
        return await conn.ExecuteAsync("DELETE FROM Floors WHERE id_floors = @Id", new { Id = id_floors });
    }

    // Languages
    public async Task<int> InsertLanguageAsync(string name_language, string code_language)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Languages (name_language, code_language) VALUES (@Name, @Code)";
        return await conn.ExecuteAsync(sql, new { Name = name_language, Code = code_language });
    }

    public async Task<IEnumerable<dynamic>> GetLanguagesAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT * FROM Languages");
    }

    public async Task<int> DeleteLanguageAsync(int id_language)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Languages WHERE id_language = @Id", new { Id = id_language });
    }

    public async Task<int> UpdateLanguageAsync(int id_language, string name_language, string code_language)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("UPDATE Languages SET name_language = @Name, code_language = @Code WHERE id_language = @Id", new { Name = name_language, Code = code_language, Id = id_language });
    }

    // Visitor Types
    public async Task<int> InsertVisitorTypeAsync(string name_visitor_type)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Visitor_type (name_visitor_type) VALUES (@Name)";
        return await conn.ExecuteAsync(sql, new { Name = name_visitor_type });
    }

    public async Task<IEnumerable<dynamic>> GetVisitorTypesAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT * FROM Visitor_type");
    }

    public async Task<int> DeleteVisitorTypeAsync(int id_visitor_type)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Visitor_type WHERE id_visitor_type = @Id", new { Id = id_visitor_type });
    }

    public async Task<int> UpdateVisitorTypeAsync(int id_visitor_type, string name_visitor_type)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("UPDATE Visitor_type SET name_visitor_type = @Name WHERE id_visitor_type = @Id", new { Name = name_visitor_type, Id = id_visitor_type });
    }

    public async Task<int> CreateTranslationAsync(int id_language,string translation_namespace,string translation_key,string text)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Translations (id_language,namespace,translation_key,text) VALUES (@IdLanguage,@Namespace,@TranslationKey,@Text)";
        return await conn.ExecuteAsync(sql, new { IdLanguage = id_language, Namespace = @translation_namespace, TranslationKey = translation_key, Text = text });
    }

    public async Task<int> UpdateTranslationAsync(int id_translation,string text)
    {
        using var conn = CreateConnection();
        var sql = "UPDATE Translations SET text = @Text WHERE id_translation = @IdTranslation";
        return await conn.ExecuteAsync(sql, new { Text = text, IdTranslation = id_translation});
    }

    public async Task<int> DeleteTranslationAsync(int id_translation)
    {
        using var conn = CreateConnection();
        var sql = "DELETE FROM Translations WHERE id_translation = @IdTranslation";
        return await conn.ExecuteAsync(sql, new { IdTranslation = id_translation });
    }

    public async Task<IEnumerable<dynamic>> GetTranslationsByLanguageAsync(int id_language)
    {
        using var conn = CreateConnection();
        var sql = "SELECT * FROM Translations WHERE id_language = @IdLanguage";
        return await conn.QueryAsync(sql, new { IdLanguage = id_language });
    }

    public async Task<IEnumerable<dynamic>> GetTranslationsByNamespaceAsync(int id_language, string translation_namespace)
    {
        using var conn = CreateConnection();
        var sql = "SELECT * FROM Translations WHERE id_language = @IdLanguage AND namespace = @Namespace";
        return await conn.QueryAsync(sql, new { IdLanguage = id_language, Namespace = translation_namespace });
    }

    public async Task<IEnumerable<dynamic>> GetLanguagesIdAsync()
    {
        using var conn = CreateConnection();
        var sql = "SELECT id_language FROM languages";
        return await conn.QueryAsync(sql);
    }

    public async Task<TranslationDashboardDto> GetTranslationDashboardAsync()
    {
        using var conn = CreateConnection();

        var langRows = (await conn.QueryAsync("SELECT id_language, name_language FROM Languages")).ToList();
        var vtRows = (await conn.QueryAsync("SELECT id_visitor_type, name_visitor_type FROM Visitor_type")).ToList();

        var languages = langRows.Select(r => new DashboardLanguageDto((int)r.id_language, (string)r.name_language)).ToList();
        var visitorTypes = vtRows.Select(r => new DashboardVisitorTypeDto((int)r.id_visitor_type, (string)r.name_visitor_type)).ToList();

        // i18n: all translations across all languages
        var transRows = (await conn.QueryAsync(@"
            SELECT t.namespace AS ns, t.translation_key AS tkey, t.id_language, t.text, l.name_language
            FROM Translations t
            JOIN Languages l ON t.id_language = l.id_language")).ToList();

        var byNsKey = transRows
            .GroupBy(r => new { ns = (string)r.ns, key = (string)r.tkey })
            .ToList();

        var i18nMissing = new List<I18nMissingDto>();
        foreach (var group in byNsKey)
        {
            var coveredLangIds = group.Select(r => (int)r.id_language).ToHashSet();
            var emptyLangIds = group
                .Where(r => string.IsNullOrWhiteSpace((string)r.text))
                .Select(r => (int)r.id_language)
                .ToHashSet();

            var missingIn = languages.Where(l => !coveredLangIds.Contains(l.Id)).ToList();
            var emptyIn = languages.Where(l => emptyLangIds.Contains(l.Id)).ToList();

            if (missingIn.Any() || emptyIn.Any())
                i18nMissing.Add(new I18nMissingDto(group.Key.ns, group.Key.key, missingIn, emptyIn));
        }

        // Infospots: all infospots with room info and translations
        var isRows = (await conn.QueryAsync(@"
            SELECT
                ip.id_info_popup,
                r.name AS room_name,
                r.number AS room_number,
                r.id_rooms,
                ipt.id_languages,
                l.name_language,
                ipt.id_visitor_type,
                vt.name_visitor_type
            FROM Info_Popup ip
            LEFT JOIN Pictures p ON ip.id_pictures = p.id_pictures
            LEFT JOIN Rooms r ON p.id_rooms = r.id_rooms
            LEFT JOIN Info_popup_translation ipt ON ip.id_info_popup = ipt.id_info_popup
            LEFT JOIN Languages l ON ipt.id_languages = l.id_language
            LEFT JOIN Visitor_type vt ON ipt.id_visitor_type = vt.id_visitor_type
            ORDER BY ip.id_info_popup")).ToList();

        var infospotGroups = isRows.GroupBy(r => (int)r.id_info_popup).ToList();
        var infospots = new List<InfospotCoverageDto>();

        foreach (var group in infospotGroups)
        {
            var first = group.First();
            var coveredLangIds = group
                .Where(r => r.id_languages != null)
                .Select(r => (int)r.id_languages)
                .Distinct()
                .ToHashSet();

            var missingLangs = languages.Where(l => !coveredLangIds.Contains(l.Id)).ToList();

            var translations = group
                .Where(r => r.id_languages != null)
                .Select(r => new InfospotTranslationDto(
                    (int)r.id_languages,
                    (string)r.name_language,
                    r.id_visitor_type != null ? (int?)r.id_visitor_type : null,
                    r.name_visitor_type != null ? (string?)r.name_visitor_type : null
                ))
                .ToList();

            infospots.Add(new InfospotCoverageDto(
                (int)first.id_info_popup,
                first.room_name != null ? (string)first.room_name : "Salle inconnue",
                first.room_number != null ? (string)first.room_number : "",
                first.id_rooms != null ? (int?)first.id_rooms : null,
                missingLangs,
                translations
            ));
        }

        return new TranslationDashboardDto(languages, visitorTypes, i18nMissing, infospots);
    }






}
