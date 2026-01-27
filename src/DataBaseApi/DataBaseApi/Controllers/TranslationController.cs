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

    [HttpGet("{id_language}/{translation_namespace}")]
    public async Task<IActionResult> GetTranslationsByNamespace(int id_language, string translation_namespace)
    {
        var res = await _db.GetTranslationsByNamespaceAsync(id_language, translation_namespace);
        // Transformation de la liste en dictionnaire
        var formattedTranslations = res.ToDictionary(
            item => $"{item.translation_key}", // La cl� : "home.guidedTour"
            item => item.text                                  // La valeur : "Tour guid�"
        );

        return Ok(formattedTranslations);
    }

    [HttpGet("all/{id_language}/{translation_namespace}")]
    public async Task<IActionResult> GetEntiereTranslationsByNamespace(int id_language, string translation_namespace)
    {
        var res = await _db.GetTranslationsByNamespaceAsync(id_language, translation_namespace);
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
    [HttpGet]
    public async Task<ActionResult<List<int>>> GetLanguagesId()
    {
        var res = await _db.GetLanguagesIdAsync();

        var ids = res.Select(x => x.id_language).ToList();

        return Ok(ids);
    }

    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages()
    {
        var res = await _db.GetLanguagesAsync();
        return Ok(res);
    }

    [HttpPost("create-language")]
    public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageDto dto)
    {
        var id = await _db.InsertLanguageAsync(dto.name_language);
        return Ok(new { id });
    }

    [HttpPut("update-language")]
    public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageDto dto)
    {
        await _db.UpdateLanguageAsync(dto.id_language, dto.name_language);
        return Ok();
    }

    [HttpDelete("delete-language/{id}")]
    public async Task<IActionResult> DeleteLanguage(int id)
    {
        await _db.DeleteLanguageAsync(id);
        return Ok();
    }
}