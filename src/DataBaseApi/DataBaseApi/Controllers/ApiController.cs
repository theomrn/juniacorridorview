using DataBaseApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
namespace DataBaseApi.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly ILogger<ApiController> _logger;

    #region Constructor

    public ApiController(DatabaseService db, ILogger<ApiController> logger)
    {
        _db = db;
        _logger = logger;
    }

    #endregion

    #region Tables

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

    #endregion

    #region Pictures

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

    [HttpGet("pictures-by-room/{id}")]
    public async Task<IActionResult> GetPicturesByRoomId(int id)
    {
        try
        {
            var pics = await _db.GetPicturesByRoomIdAsync(id);
            return Ok(pics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error get room by id");
            return StatusCode(500, "Error get room by id");
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
        string data = await _db.FetchImageByIdAsync(id);
        if (data == null) return NotFound();
        return Ok(data);
    }

    #endregion

    #region Rooms

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


    [HttpGet("room-id/{id_pictures}")]
    public async Task<IActionResult> GetRoomIdByPictureID(int id_pictures)
    {
        try
        {
            var room = await _db.GetRoomIdByPictureIdAsync(id_pictures);
            return Ok(room);
        }catch(Exception ex)
        {
            _logger.LogError(ex, "error in GetRoomIdByPictureID");
            return StatusCode(500, "error in GetRoomIdByPictureID");
        }
    }

    [HttpGet("room/{id}")]
    public async Task<IActionResult> GetRoom(int id)
    {
        try
        {
            var room = await _db.GetRoomByIdAsync(id);
            if (room == null) return NotFound();
            return Ok(room);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error GetRoom");
            return StatusCode(500, "Error GetRoom");
            throw;
        }
    }

    [HttpPost("add-room")]
    public async Task<IActionResult> AddRoom([FromForm] string name, [FromForm] string number, [FromForm] int id_floors, [FromForm] string? plan_x, [FromForm] string? plan_y, IFormFile? previewImage)
    {
        try
        {
            var roomId = await _db.AddRoomAsync(name, number, id_floors, double.Parse(plan_x.Replace(".", ",")), double.Parse(plan_y.Replace(".", ",")));
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
    public async Task<IActionResult> UpdateRoom([FromForm] int id_rooms, [FromForm] string name, [FromForm] string number, [FromForm] int id_floors, [FromForm] string? plan_x, [FromForm] string? plan_y, IFormFile? previewImage)
    {
        try
        {
            var res = await _db.UpdateRoomAsync(id_rooms, name, number, id_floors, double.Parse(plan_x.Replace(".",",")), double.Parse(plan_y.Replace(".",",")));
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
            var listPicturesToDelete = await _db.GetPicturesByRoomIdAsync(id);

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
            var res = await _db.UpdateRoomVisibilityAsync(dto.id_rooms, dto.Hidden);
            return Ok(new { updated = res });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room visibility");
            return StatusCode(500, "Error updating room visibility");
        }
    }

    #endregion

    #region Room Previews

    [HttpGet("room-preview/{id}")]
    public async Task<IActionResult> RoomPreview(int id)
    {
        var fullUrl = await _db.GetRoomPreviewAsync(id);
        if (fullUrl == null) return NotFound();
        return Ok(fullUrl);
        //var contentType = ext switch
        //{
        //    ".png" => "image/png",
        //    ".jpg" or ".jpeg" => "image/jpeg",
        //    _ => "application/octet-stream"
        //};
        //return File(data, contentType);
    }

    [HttpDelete("room-preview/{id}")]
    public async Task<IActionResult> DeleteRoomPreview(int id)
    {
        var res = await _db.DeleteRoomPreviewAsync(id);
        return Ok(new { deleted = res });
    }

    #endregion

    #region Links

    [HttpPost("retrieveLinkByIdPicture")]
    public async Task<IActionResult> RetrieveLinkByIdPicture([FromBody] IdDto dto)
    {
        var res = await _db.RetrieveLinkByIdPictureAsync(dto.id_pictures);
        return Ok(res);
    }

    [HttpPost("insertLink")]
    public async Task<IActionResult> InsertLink([FromForm] InsertLinkDto dto)
    {
        var res = await _db.InsertLinkAsync(dto.id_pictures, double.Parse(dto.PosX.Replace(".",",")), double.Parse(dto.PosY.Replace(".",",")), double.Parse(dto.PosZ.Replace(".", ",")), dto.id_pictures_destination);
        return Ok(new { inserted = res });
    }

    [HttpPut("update-link")]
    public async Task<IActionResult> UpdateLink([FromForm] UpdateLinkDto dto)
    {
        var res = await _db.UpdateLinkAsync(dto.id_links, dto.id_pictures, double.Parse(dto.PosX.Replace(".", ",")), double.Parse(dto.PosY.Replace(".", ",")), double.Parse(dto.PosZ.Replace(".", ",")), dto.id_pictures_destination);
        return Ok(new { updated = res });
    }

    [HttpDelete("delete-link/{id}")]
    public async Task<IActionResult> DeleteLink(int id)
    {
        var res = await _db.DeleteLinkAsync(id);
        return Ok(new { deleted = res });
    }

    #endregion

    #region Info PopUps

    [HttpPost("retrieveInfoPopUpByIdPicture")]
    public async Task<IActionResult> RetrieveInfoPopUpByIdPicture([FromBody] IdDto dto)
    {
        var res = await _db.RetrieveInfoPopUpByIdPictureAsync(dto.id_pictures);
        return Ok(res);
    }

    [HttpPost("insertInfoPopUp")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> InsertInfoPopUp([FromForm] int id_pictures, [FromForm] string posX, [FromForm] string posY, [FromForm] string posZ)
    {
        var file = Request.Form.Files.FirstOrDefault();
        var res = await _db.InsertInfoPopUpAsync(id_pictures, double.Parse(posX.Replace(".",",")), double.Parse(posY.Replace(".", ",")), double.Parse(posZ.Replace(".", ",")), file);
        return Ok(new { inserted = res });
    }

    [HttpPut("update-infospot")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UpdateInfospot([FromForm] int id_info_popup, [FromForm] int id_pictures, [FromForm] double posX, [FromForm] double posY, [FromForm] double posZ, [FromForm] string text, [FromForm] string title, [FromForm] string id_languages, [FromForm] string id_visitor_type)
    {
        var file = Request.Form.Files.FirstOrDefault();
        var res = await _db.UpdateInfospotAsync(id_info_popup, id_pictures, posX, posY, posZ, text, title, file, id_languages, id_visitor_type);
        return Ok(new { updated = res });
    }

    [HttpDelete("delete-infospot/{id}")]
    public async Task<IActionResult> DeleteInfospot(int id)
    {
        var res = await _db.DeleteInfoPopUpAsync(id);
        return Ok(new { deleted = res });
    }

    [HttpGet("GetPlansToDelete/{id}")]
    public async Task<IActionResult> GetPlansToDelete(int id)
    {
        var res = await _db.GetPlansToDelete(id);

        return Ok(res);
    }

    #endregion

    #region Tours

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

    #endregion

    #region Buildings

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
        var listIdFloors = await _db.GetFloorIdByIdBuildingAsync(id);

        foreach (var idFloor in listIdFloors)
        {
            var listIdRooms = await _db.GetRoomIdByIdFloorAsync(idFloor.Value);
         
            foreach (var idRoom in listIdRooms)
            {
                var listIdPictures = await _db.GetPicturesByRoomIdAsync(idRoom.Value);
                await _db.DeleteRoomAsync(idRoom.Value);
            }


            await _db.DeleteFloorAsync(idFloor.Value);
        }

        await _db.DeleteBuildingAsync(id);
        return Ok();
    }

    [HttpPost("update-building")]
    public async Task<IActionResult> UpdateBuilding([FromForm] UpdateBuildingDto dto)
    {
        await _db.UpdateBuildingAsync(dto.IdBuildings, dto.Name);
        return Ok();
    }

    #endregion

    #region Floors

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

    #endregion

    #region Utility

    [HttpGet("ping")]
    public IActionResult Ping() => Ok("pong");


    [HttpPost("file-converter")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ConvertFile([FromForm] FileDto fileRequest)
    {
        if (fileRequest.file == null || fileRequest.file.Length == 0)
            return BadRequest("No file uploaded");

        var result = await FileOptimiserService.ConvertFileToAvifAsync(fileRequest.file);

        return File(
            result,
            "image/avif",
            Path.ChangeExtension(fileRequest.file.FileName, ".avif")
        );
    }

    #endregion

    #region Languages

    [HttpPost("insert-language")]
    public async Task<IActionResult> InsertLanguage([FromBody] NameDto dto)
    {
        var id = await _db.InsertLanguageAsync(dto.Name);
        return Ok(new { id });
    }

    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages()
    {
        var res = await _db.GetLanguagesAsync();
        return Ok(res);
    }

    [HttpDelete("delete-language/{id}")]
    public async Task<IActionResult> DeleteLanguage(int id)
    {
        await _db.DeleteLanguageAsync(id);
        return Ok();
    }

    [HttpPut("update-language/{id}")]
    public async Task<IActionResult> UpdateLanguage(int id, [FromBody] NameDto dto)
    {
        await _db.UpdateLanguageAsync(id, dto.Name);
        return Ok();
    }
    #endregion

    // Visitor Types
    #region Visitor Types

    [HttpGet("visitor-types")]
    public async Task<IActionResult> GetVisitorTypes()
    {
        var res = await _db.GetVisitorTypesAsync();
        return Ok(res);
    }

    [HttpPost("insert-visitor-type")]
    public async Task<IActionResult> InsertVisitorType([FromBody] NameDto dto)
    {
        var id = await _db.InsertVisitorTypeAsync(dto.Name);
        return Ok(new { id });
    }

    [HttpDelete("delete-visitor-type/{id}")]
    public async Task<IActionResult> DeleteVisitorType(int id)
    {
        await _db.DeleteVisitorTypeAsync(id);
        return Ok();    
    }

    [HttpPut("update-visitor-type/{id}")]
    public async Task<IActionResult> UpdateVisitorType(int id, [FromBody] NameDto dto)
    {
        await _db.UpdateVisitorTypeAsync(id, dto.Name);
        return Ok();    
    }

    #endregion
}