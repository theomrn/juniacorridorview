using Microsoft.AspNetCore.Mvc;
using DataBaseApi.Services;
namespace DataBaseApi.Controllers;
[ApiController]
[Route("api/translations")]
public class TranslationController : ControllerBase
{

    private readonly DatabaseService _db;
    private readonly ILogger<TranslationController> _logger;

    public TranslationController(DatabaseService db, ILogger<TranslationController> logger)
    {
        _db = db;
        _logger = logger;
    }

     [HttpGet("{id_languages}")]
    public async Task<IActionResult> GetTranslations(int id_languages)
    {
        var res = await _db.GetTranslationsByLanguageAsync(id_languages);
        return Ok(res);
    }

    [HttpPut("update-translation")]
    public async Task<IActionResult> UpdateTranslation([FromBody] UpdateTranslationDto dto)
    {
        await _db.UpdateTranslationAsync(dto.id_translation, dto.text);
        return Ok();
    }

    [HttpPost("insert-translation")]
    public async Task<IActionResult> InsertTranslation([FromBody] InsertTranslationDto dto)
    {
        var id = await _db.CreateTranslationAsync(dto.id_language, dto.translation_namespace, dto.translation_key, dto.text);
        return Ok(new { id });
    }

    [HttpDelete("delete-translation/{id}")]
    public async Task<IActionResult> DeleteTranslation(int id)
    {
        await _db.DeleteTranslationAsync(id);
        return Ok();
    }
}