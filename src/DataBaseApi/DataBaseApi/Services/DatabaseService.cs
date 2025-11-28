using System.Data;
using Dapper;
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
        var sql = "UPDATE Pictures SET picture_path = @Path WHERE id_pictures = @Id";
        var res = await conn.ExecuteAsync(sql, new { Path = relPath, Id = id_pictures });
        return res;
    }

    public async Task<int> DeleteImageAsync(int id_pictures)
    {
        using var conn = CreateConnection();
        // Optionally fetch path to delete file
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT picture_path FROM Pictures WHERE id_pictures = @Id", new { Id = id_pictures });
        if (row != null)
        {
            var rel = (string)row.picture_path;
            var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rel.Replace("/", Path.DirectorySeparatorChar.ToString()));
            try { if (File.Exists(full)) File.Delete(full); } catch { }
        }
        var sql = "DELETE FROM Pictures WHERE id_pictures = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id_pictures });
    }

    public async Task<(byte[]?, string?)> FetchImageByIdAsync(int id)
    {
        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT picture_path FROM Pictures WHERE id_pictures = @Id", new { Id = id });
        if (row == null) return (null, null);
        var relativePath = (string)row.picture_path;
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(fullPath)) return (null, null);
        return (await File.ReadAllBytesAsync(fullPath), Path.GetExtension(fullPath));
    }

    // Rooms
    public async Task<IEnumerable<dynamic>> GetRoomsAsync()
    {
        using var conn = CreateConnection();
        var rows = await conn.QueryAsync("SELECT * FROM Rooms ORDER BY name ASC");
        return rows;
    }

    public async Task<dynamic?> GetRoomByIdAsync(int id_rooms)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync("SELECT * FROM Rooms WHERE id_rooms = @Id", new { Id = id_rooms });
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
        if (row != null)
        {
            var rel = (string)row.preview_path;
            var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rel.Replace("/", Path.DirectorySeparatorChar.ToString()));
            try { if (File.Exists(full)) File.Delete(full); } catch { }
        }
        return await conn.ExecuteAsync("DELETE FROM Room_Previews WHERE id_rooms = @Id", new { Id = id_rooms });
    }

    public async Task<(byte[]?, string?)> GetRoomPreviewAsync(int id_rooms)
    {
        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT preview_path FROM Room_Previews WHERE id_rooms = @Id LIMIT 1", new { Id = id_rooms });
        if (row == null) return (null, null);
        var rel = (string)row.preview_path;
        var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rel.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(full)) return (null, null);
        return (await File.ReadAllBytesAsync(full), Path.GetExtension(full));
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
    public async Task<int> InsertInfoPopUpAsync(int id_pictures, double posX, double posY, double posZ, string text, string title, IFormFile? imageFile)
    {
        string? path = null;
        if (imageFile != null) path = await SaveFileAsync(imageFile, "images");
        using var conn = CreateConnection();
        var sql = "INSERT INTO Info_Popup (id_pictures, position_x, position_y, position_z, text, title, image_path) VALUES (@Id, @X, @Y, @Z, @Text, @Title, @Img)";
        return await conn.ExecuteAsync(sql, new { Id = id_pictures, X = posX, Y = posY, Z = posZ, Text = text, Title = title, Img = path });
    }

    public async Task<IEnumerable<dynamic>> RetrieveInfoPopUpByIdPictureAsync(int id_pictures)
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync("SELECT * FROM Info_Popup WHERE id_pictures = @Id", new { Id = id_pictures });
    }

    public async Task<int> UpdateInfospotAsync(int id_info_popup, int id_pictures, double posX, double posY, double posZ, string text, string title, IFormFile? imageFile)
    {
        using var conn = CreateConnection();
        if (imageFile != null)
        {
            var path = await SaveFileAsync(imageFile, "images");
            var sql = "UPDATE Info_Popup SET id_pictures = @IdP, position_x = @X, position_y = @Y, position_z = @Z, text = @Text, title = @Title, image_path = @Img WHERE id_info_popup = @Id";
            return await conn.ExecuteAsync(sql, new { IdP = id_pictures, X = posX, Y = posY, Z = posZ, Text = text, Title = title, Img = path, Id = id_info_popup });
        }
        else
        {
            var sql = "UPDATE Info_Popup SET id_pictures = @IdP, position_x = @X, position_y = @Y, position_z = @Z, text = @Text, title = @Title WHERE id_info_popup = @Id";
            return await conn.ExecuteAsync(sql, new { IdP = id_pictures, X = posX, Y = posY, Z = posZ, Text = text, Title = title, Id = id_info_popup });
        }
    }

    public async Task<int> DeleteInfoPopUpAsync(int id_info_popup)
    {
        using var conn = CreateConnection();
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

    public async Task<int> CreateTourWithStepsAsync(string title, string description, IEnumerable<dynamic> steps)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Tours (title, description) VALUES (@Title, @Desc); SELECT LAST_INSERT_ID();";
        var tourId = await conn.ExecuteScalarAsync<int>(sql, new { Title = title, Desc = description });
        var insertStepSql = "INSERT INTO Tour_Steps (id_tour_steps, id_tours, id_rooms, step_number) VALUES (@IdStep, @IdTour, @IdRoom, @Num)";
        foreach (var s in steps)
        {
            var idStep = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(insertStepSql, new { IdStep = idStep, IdTour = tourId, IdRoom = s.id_rooms, Num = s.step_number });
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
        return await conn.ExecuteAsync("DELETE FROM Floors WHERE id_floors = @Id", new { Id = id_floors });
    }
}
