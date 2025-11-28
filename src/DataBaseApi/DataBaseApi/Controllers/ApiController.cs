using DataBaseApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBaseApi.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly ILogger<ApiController> _logger;

    public ApiController(DatabaseService db, ILogger<ApiController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        try
        {
            var tables = await _db.GetTablesAsync();
            return Ok(tables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tables");
            return StatusCode(500, "Error fetching tables");
        }
    }

    [HttpGet("pictures")]
    public async Task<IActionResult> GetPictures()
    {
        try
        {
            var pics = await _db.GetAllPicturesAsync();
            return Ok(pics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pictures");
            return StatusCode(500, "Error fetching pictures");
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadImage([FromForm] int id_rooms)
    {
        try
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null) return BadRequest("No file provided");
            var res = await _db.InsertImageFileAsync(id_rooms, file);
            return Ok(new { inserted = res });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return StatusCode(500, "Error uploading image");
        }
    }

    [HttpPut("update-image")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UpdateImage([FromForm] int id_pictures)
    {
        try
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null) return BadRequest("No file provided");
            var res = await _db.UpdateImageFileAsync(id_pictures, file);
            return Ok(new { updated = res });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating image");
            return StatusCode(500, "Error updating image");
        }
    }

    [HttpDelete("delete-image/{id}")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        try
        {
            var res = await _db.DeleteImageAsync(id);
            return Ok(new { deleted = res });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image");
            return StatusCode(500, "Error deleting image");
        }
    }

    [HttpGet("fetch/{id}")]
    public async Task<IActionResult> FetchImage(int id)
    {
        var (data, ext) = await _db.FetchImageByIdAsync(id);
        if (data == null) return NotFound();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
        return File(data, contentType);
    }

    // Rooms CRUD
    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        try
        {
            var rooms = await _db.GetRoomsAsync();
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rooms");
            return StatusCode(500, "Error fetching rooms");
        }
    }

    [HttpGet("room/{id}")]
    public async Task<IActionResult> GetRoom(int id)
    {
        var room = await _db.GetRoomByIdAsync(id);
        if (room == null) return NotFound();
        return Ok(room);
    }

    [HttpPost("add-room")]
    public async Task<IActionResult> AddRoom([FromForm] string name, [FromForm] string number, [FromForm] int id_floors, [FromForm] string? plan_x, [FromForm] string? plan_y, IFormFile? previewImage)
    {
        try
        {
            var roomId = await _db.AddRoomAsync(name, number, id_floors, double.Parse(plan_x), double.Parse(plan_y));
            if (previewImage != null)
            {
                await _db.InsertRoomPreviewFileAsync(roomId, previewImage);
            }
            return Ok(new { id_rooms = roomId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding room");
            return StatusCode(500, "Error adding room");
        }
    }

    [HttpPut("update-room")]
    public async Task<IActionResult> UpdateRoom([FromForm] int id_rooms, [FromForm] string name, [FromForm] string number, [FromForm] int id_floors, [FromForm] double? plan_x, [FromForm] double? plan_y, IFormFile? previewImage)
    {
        try
        {
            var res = await _db.UpdateRoomAsync(id_rooms, name, number, id_floors, plan_x, plan_y);
            if (previewImage != null)
            {
                await _db.InsertRoomPreviewFileAsync(id_rooms, previewImage);
            }
            return Ok(new { updated = res });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room");
            return StatusCode(500, "Error updating room");
        }
    }

    [HttpDelete("delete-room/{id}")]
    public async Task<IActionResult> DeleteRoomEndpoint(int id)
    {
        try
        {
            var res = await _db.DeleteRoomAsync(id);
            return Ok(new { deleted = res });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room");
            return StatusCode(500, "Error deleting room");
        }
    }

    [HttpPost("update-room-visibility")]
    public async Task<IActionResult> UpdateRoomVisibility([FromBody] UpdateVisibilityDto dto)
    {
        try
        {
            var res = await _db.UpdateRoomVisibilityAsync(dto.IdRooms, dto.Hidden);
            return Ok(new { updated = res });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room visibility");
            return StatusCode(500, "Error updating room visibility");
        }
    }

    // Room previews
    [HttpGet("room-preview/{id}")]
    public async Task<IActionResult> RoomPreview(int id)
    {
        var (data, ext) = await _db.GetRoomPreviewAsync(id);
        if (data == null) return NotFound();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
        return File(data, contentType);
    }

    [HttpDelete("room-preview/{id}")]
    public async Task<IActionResult> DeleteRoomPreview(int id)
    {
        var res = await _db.DeleteRoomPreviewAsync(id);
        return Ok(new { deleted = res });
    }

    // Links
    [HttpPost("retrieveLinkByIdPicture")]
    public async Task<IActionResult> RetrieveLinkByIdPicture([FromBody] IdDto dto)
    {
        var res = await _db.RetrieveLinkByIdPictureAsync(dto.Id);
        return Ok(res);
    }

    [HttpPost("insertLink")]
    public async Task<IActionResult> InsertLink([FromBody] InsertLinkDto dto)
    {
        var res = await _db.InsertLinkAsync(dto.IdPictures, dto.PosX, dto.PosY, dto.PosZ, dto.IdPicturesDestination);
        return Ok(new { inserted = res });
    }

    [HttpPut("update-link")]
    public async Task<IActionResult> UpdateLink([FromBody] UpdateLinkDto dto)
    {
        var res = await _db.UpdateLinkAsync(dto.IdLinks, dto.IdPictures, dto.PosX, dto.PosY, dto.PosZ, dto.IdPicturesDestination);
        return Ok(new { updated = res });
    }

    [HttpDelete("delete-link/{id}")]
    public async Task<IActionResult> DeleteLink(int id)
    {
        var res = await _db.DeleteLinkAsync(id);
        return Ok(new { deleted = res });
    }

    // Info popups
    [HttpPost("retrieveInfoPopUpByIdPicture")]
    public async Task<IActionResult> RetrieveInfoPopUpByIdPicture([FromBody] IdDto dto)
    {
        var res = await _db.RetrieveInfoPopUpByIdPictureAsync(dto.Id);
        return Ok(res);
    }

    [HttpPost("insertInfoPopUp")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> InsertInfoPopUp([FromForm] int id_pictures, [FromForm] double posX, [FromForm] double posY, [FromForm] double posZ, [FromForm] string text, [FromForm] string title)
    {
        var file = Request.Form.Files.FirstOrDefault();
        var res = await _db.InsertInfoPopUpAsync(id_pictures, posX, posY, posZ, text, title, file);
        return Ok(new { inserted = res });
    }

    [HttpPut("update-infospot")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UpdateInfospot([FromForm] int id_info_popup, [FromForm] int id_pictures, [FromForm] double posX, [FromForm] double posY, [FromForm] double posZ, [FromForm] string text, [FromForm] string title)
    {
        var file = Request.Form.Files.FirstOrDefault();
        var res = await _db.UpdateInfospotAsync(id_info_popup, id_pictures, posX, posY, posZ, text, title, file);
        return Ok(new { updated = res });
    }

    [HttpDelete("delete-infospot/{id}")]
    public async Task<IActionResult> DeleteInfospot(int id)
    {
        var res = await _db.DeleteInfoPopUpAsync(id);
        return Ok(new { deleted = res });
    }

    // Tours
    [HttpGet("tours")]
    public async Task<IActionResult> GetTours()
    {
        var res = await _db.GetToursAsync();
        return Ok(res);
    }

    [HttpGet("tour-steps/{id}")]
    public async Task<IActionResult> GetTourSteps(int id)
    {
        var res = await _db.GetTourStepsWithRoomInfoAsync(id);
        return Ok(res);
    }

    [HttpPost("update-tour-steps")]
    public async Task<IActionResult> UpdateTourSteps([FromBody] UpdateTourStepsDto dto)
    {
        await _db.UpdateTourStepsAsync(dto.IdTours, dto.Steps, dto.Title, dto.Description);
        return Ok();
    }

    [HttpPost("add-tour-step")]
    public async Task<IActionResult> AddTourStep([FromBody] AddTourStepDto dto)
    {
        await _db.AddTourStepAsync(dto.IdTours, dto.Step);
        return Ok();
    }

    [HttpPost("create-tour")]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourDto dto)
    {
        var id = await _db.CreateTourWithStepsAsync(dto.Title, dto.Description, dto.Steps);
        return Ok(new { id });
    }

    [HttpDelete("delete-tour/{id}")]
    public async Task<IActionResult> DeleteTour(int id)
    {
        await _db.DeleteTourAsync(id);
        return Ok();
    }

    [HttpPost("update-tour-visibility")]
    public async Task<IActionResult> UpdateTourVisibility([FromBody] UpdateVisibilityTourDto dto)
    {
        await _db.UpdateTourVisibilityAsync(dto.IdTours, dto.Hidden);
        return Ok();
    }

    // Buildings
    [HttpGet("buildings")]
    public async Task<IActionResult> GetBuildings()
    {
        var res = await _db.GetBuildingsAsync();
        return Ok(res);
    }

    [HttpPost("add-building")]
    public async Task<IActionResult> AddBuilding([FromForm] NameDto dto)
    {
        var id = await _db.AddBuildingAsync(dto.Name);
        return Ok(new { id });
    }

    [HttpDelete("building/{id}")]
    public async Task<IActionResult> DeleteBuilding(int id)
    {
        await _db.DeleteBuildingAsync(id);
        return Ok();
    }

    [HttpPost("update-building")]
    public async Task<IActionResult> UpdateBuilding([FromForm] UpdateBuildingDto dto)
    {
        await _db.UpdateBuildingAsync(dto.IdBuildings, dto.Name);
        return Ok();
    }

    // Floors
    [HttpGet("floors")]
    public async Task<IActionResult> GetFloors()
    {
        var res = await _db.GetFloorsAsync();
        return Ok(res);
    }

    [HttpGet("floors/{id}")]
    public async Task<IActionResult> GetFloor(int id)
    {
        var res = await _db.GetFloorByIdAsync(id);
        if (res == null) return NotFound();
        return Ok(res);
    }

    [HttpPost("add-floor")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> AddFloor([FromForm] string name, [FromForm] int id_buildings)
    {
        var file = Request.Form.Files.FirstOrDefault();
        var id = await _db.AddFloorAsync(name, id_buildings, file);
        return Ok(new { id });
    }

    [HttpDelete("floor/{id}")]
    public async Task<IActionResult> DeleteFloor(int id)
    {
        await _db.DeleteFloorAsync(id);
        return Ok();
    }

    [HttpPost("update-floor")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UpdateFloor([FromForm] int id_floors, [FromForm] string name, [FromForm] int id_buildings)
    {
        var file = Request.Form.Files.FirstOrDefault();
        await _db.UpdateFloorAsync(id_floors, name, id_buildings, file);
        return Ok();
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok("pong");
}

// DTOs
public record IdDto(int Id);
public record UpdateVisibilityDto(int IdRooms, bool Hidden);
public record UpdateVisibilityTourDto(int IdTours, bool Hidden);
public record NameDto(string Name);
public record UpdateBuildingDto(int IdBuildings, string Name);
public record InsertLinkDto(int IdPictures, double PosX, double PosY, double PosZ, int IdPicturesDestination);
public record UpdateLinkDto(int IdLinks, int IdPictures, double PosX, double PosY, double PosZ, int IdPicturesDestination);
public record UpdateTourStepsDto(int IdTours, IEnumerable<dynamic> Steps, string? Title, string? Description);
public record AddTourStepDto(int IdTours, dynamic Step);
public record CreateTourDto(string Title, string Description, IEnumerable<dynamic> Steps);
public record AddTourDto(string Title, string Description, IEnumerable<dynamic> Steps);
public record InsertInfoPopUpDto(int IdPictures, double PosX, double PosY, double PosZ, string Text, string Title);
