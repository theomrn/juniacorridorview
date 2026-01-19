using System.Data;
using Dapper;
using DataBaseApi.Controllers;
using MySqlConnector;

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
        var sql = "INSERT INTO Info_Popup (id_pictures, position_x, position_y, position_z, image_path) VALUES (@Id, @X, @Y, @Z, @Img)";
        return await conn.ExecuteAsync(sql, new { Id = id_pictures, X = posX, Y = posY, Z = posZ, Img = path });
    }

    public async Task<int> InsertInfoPopUpTranslationAsync(int id_info_popup, string text, string title, int id_languages, int id_visitor_type)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Info_popup_translation (id_info_popup, title, text) VALUES (@Id, @Title, @Text)";
        return await conn.ExecuteAsync(sql, new { Id = id_info_popup, Title = title, Text = text });
    }

    public async Task<IEnumerable<dynamic>> RetrieveInfoPopUpByIdPictureAsync(int id_pictures)
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
            INNER JOIN Info_popup_translation ipt 
                ON ip.id_info_popup = ipt.id_info_popup
            WHERE ip.id_pictures = @Id;
        ";

        return await conn.QueryAsync(sql, new { Id = id_pictures });
    }

    public async Task<int> UpdateInfospotAsync(int id_info_popup, int id_pictures, double posX, double posY, double posZ, string text, string title, IFormFile? imageFile, string id_languages, string id_visitor_type)
    {
        using var conn = CreateConnection();
        if (imageFile != null)
        {
            var path = await SaveFileAsync(imageFile, "images");
            var sqlInfoPopup = "UPDATE Info_Popup SET id_pictures = @IdP, position_x = @X, position_y = @Y, position_z = @Z, image_path = @Img WHERE id_info_popup = @Id AND id_languages = @Lang AND id_visitor_type = @Vis";
            var sqlTranslation = "UPDATE Info_popup_translation SET title = @Title, text = @Text WHERE id_info_popup = @Id";
            await conn.ExecuteAsync(sqlInfoPopup, new { IdP = id_pictures, X = posX, Y = posY, Z = posZ, Img = path, Id = id_info_popup, Lang = id_languages, Vis = id_visitor_type });
            return await conn.ExecuteAsync(sqlTranslation, new { Title = title, Text = text, Id = id_info_popup });
        }
        else
        {
            var sqlInfoPopup = "UPDATE Info_Popup SET id_pictures = @IdP, position_x = @X, position_y = @Y, position_z = @Z WHERE id_info_popup = @Id";
            var sqlTranslation = "UPDATE Info_popup_translation SET title = @Title, text = @Text WHERE id_info_popup = @Id";
            await conn.ExecuteAsync(sqlTranslation, new { Title = title, Text = text, Id = id_info_popup });
            return await conn.ExecuteAsync(sqlInfoPopup, new { IdP = id_pictures, X = posX, Y = posY, Z = posZ, Id = id_info_popup });
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
        using var conn = CreateConnection();
        await conn.ExecuteAsync("UPDATE Tours SET title = @Title, description = @Desc WHERE id_tours = @Id", new { Title = title, Desc = description, Id = id_tours });
        await conn.ExecuteAsync("DELETE FROM Tour_Steps WHERE id_tours = @Id", new { Id = id_tours });
        var insertSql = "INSERT INTO Tour_Steps (id_tour_steps, id_tours, id_rooms, step_number) VALUES (@IdStep, @IdTour, @IdRoom, @Num)";
        foreach (var s in steps)
        {
            var idStep = s.id_tour_steps ?? Guid.NewGuid().ToString();
            await conn.ExecuteAsync(insertSql, new { IdStep = idStep, IdTour = id_tours, IdRoom = s.id_rooms, Num = s.step_number });
        }
        return 1;
    }

    public async Task<int> AddTourStepAsync(int id_tours, dynamic step)
    {
        using var conn = CreateConnection();
        var id = Guid.NewGuid().ToString();
        var sql = "INSERT INTO Tour_Steps (id_tour_steps, id_tours, id_rooms, step_number) VALUES (@IdStep, @IdTour, @IdRoom, @Num)";
        return await conn.ExecuteAsync(sql, new { IdStep = id, IdTour = id_tours, IdRoom = step.id_rooms, Num = step.step_number });
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
    public async Task<int> InsertLanguageAsync(string name_language)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Languages (name_language) VALUES (@Name)";
        return await conn.ExecuteAsync(sql, new {Name = name_language });
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

    public async Task<int> UpdateLanguageAsync(int id_language, string name_language)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("UPDATE Languages SET name_language = @Name WHERE id_language = @Id", new { Name = name_language, Id = id_language });
    }

    // Visitor Types
    public async Task<int> InsertVisitorTypeAsync(string name_visitor_type)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Visitor_Type (name_visitor_type) VALUES (@Name)";
        return await conn.ExecuteAsync(sql, new { Name = name_visitor_type });
    }

    public async Task<IEnumerable<dynamic>> GetVisitorTypesAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT * FROM Visitor_Type");
    }

    public async Task<int> DeleteVisitorTypeAsync(int id_visitor_type)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Visitor_Type WHERE id_visitor_type = @Id", new { Id = id_visitor_type });
    }

    public async Task<int> UpdateVisitorTypeAsync(int id_visitor_type, string name_visitor_type)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync("UPDATE Visitor_Type SET name_visitor_type = @Name WHERE id_visitor_type = @Id", new { Name = name_visitor_type, Id = id_visitor_type });
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



}
