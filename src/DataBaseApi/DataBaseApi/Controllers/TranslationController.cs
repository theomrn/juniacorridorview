using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataBaseApi.Services;
namespace DataBaseApi.Controllers;
[ApiController]
[Route("api/translations")]
[Authorize]
public class TranslationController : ControllerBase
{

    private readonly DatabaseService _db;
    private readonly LibreTranslateService _libreTranslate;
    private readonly ILogger<TranslationController> _logger;

    public TranslationController(DatabaseService db, LibreTranslateService libreTranslate, ILogger<TranslationController> logger)
    {
        _db = db;
        _libreTranslate = libreTranslate;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("{id_languages}")]
    public async Task<IActionResult> GetTranslations(int id_languages)
    {
        var res = await _db.GetTranslationsByLanguageAsync(id_languages);
        return Ok(res);
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
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
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<int>>> GetLanguagesId()
    {
        var res = await _db.GetLanguagesIdAsync();

        var ids = res.Select(x => x.id_language).ToList();

        return Ok(ids);
    }

    [AllowAnonymous]
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

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetTranslationDashboard()
    {
        var result = await _db.GetTranslationDashboardAsync();
        return Ok(result);
    }

    [HttpPost("auto-translate")]
    public async Task<IActionResult> AutoTranslate([FromBody] AutoTranslateDto dto)
    {
        if (!_libreTranslate.IsConfigured)
            return StatusCode(503, new { error = "LibreTranslate not configured — set LibreTranslate:Url in appsettings.json" });

        var languages = await _db.GetLanguagesAsync();
        var sourceLang = languages.FirstOrDefault(l => l.id_language == dto.SourceLangId);
        var targetLang = languages.FirstOrDefault(l => l.id_language == dto.TargetLangId);

        if (sourceLang == null || targetLang == null)
            return BadRequest(new { error = "Unknown language ID" });

        var sourceCode = _libreTranslate.GetIsoCode(sourceLang.name_language);
        var targetCode = _libreTranslate.GetIsoCode(targetLang.name_language);

        if (sourceCode == null || targetCode == null)
            return BadRequest(new { error = $"Unsupported language: add the ISO code mapping in LibreTranslateService.cs" });

        var result = await _libreTranslate.TranslateAsync(dto.Texts, sourceCode, targetCode);
        if (result == null)
            return StatusCode(502, new { error = "LibreTranslate request failed — check the instance is running" });

        return Ok(new { translations = result });
    }
}