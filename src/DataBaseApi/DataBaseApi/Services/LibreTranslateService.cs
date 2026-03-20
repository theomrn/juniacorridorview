using System.Net.Http.Json;

namespace DataBaseApi.Services;

public class LibreTranslateService
{
    private static readonly HttpClient _http = new();
    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly ILogger<LibreTranslateService> _logger;

    // Maps language names (as stored in DB) to ISO 639-1 codes (lowercase)
    // Add entries here if you add languages whose names are not yet covered
    private static readonly Dictionary<string, string> LangNameToCode = new(StringComparer.OrdinalIgnoreCase)
    {
        { "français", "fr" }, { "french", "fr" }, { "fr", "fr" },
        { "anglais", "en" }, { "english", "en" }, { "en", "en" },
        { "espagnol", "es" }, { "spanish", "es" }, { "es", "es" },
        { "allemand", "de" }, { "german", "de" }, { "de", "de" },
        { "néerlandais", "nl" }, { "dutch", "nl" }, { "nl", "nl" },
        { "italien", "it" }, { "italian", "it" }, { "it", "it" },
        { "portugais", "pt" }, { "portuguese", "pt" }, { "pt", "pt" },
        { "polonais", "pl" }, { "polish", "pl" }, { "pl", "pl" },
        { "japonais", "ja" }, { "japanese", "ja" }, { "ja", "ja" },
        { "chinois", "zh" }, { "chinese", "zh" }, { "zh", "zh" },
        { "russe", "ru" }, { "russian", "ru" }, { "ru", "ru" },
    };

    public LibreTranslateService(IConfiguration config, ILogger<LibreTranslateService> logger)
    {
        _baseUrl = (config["LibreTranslate:Url"] ?? "").TrimEnd('/');
        _apiKey = config["LibreTranslate:ApiKey"];
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_baseUrl);

    public string? GetIsoCode(string languageName) =>
        LangNameToCode.TryGetValue(languageName.Trim(), out var code) ? code : null;

    public async Task<List<string>?> TranslateAsync(List<string> texts, string sourceLangCode, string targetLangCode)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("LibreTranslate URL not configured — set LibreTranslate:Url in appsettings.json");
            return null;
        }

        try
        {
            var tasks = texts.Select(text => TranslateOneAsync(text, sourceLangCode, targetLangCode));
            var results = await Task.WhenAll(tasks);
            return results.Any(r => r == null) ? null : [.. results];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LibreTranslate batch failed ({Count} texts, {Source}→{Target})",
                texts.Count, sourceLangCode, targetLangCode);
            return null;
        }
    }

    private async Task<string?> TranslateOneAsync(string text, string sourceLang, string targetLang)
    {
        var body = new Dictionary<string, string>
        {
            ["q"] = text,
            ["source"] = sourceLang,
            ["target"] = targetLang,
            ["format"] = "text"
        };
        if (!string.IsNullOrEmpty(_apiKey))
            body["api_key"] = _apiKey;

        var response = await _http.PostAsJsonAsync(_baseUrl, body);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.TranslatedText;
    }

    private record LibreTranslateResponse(string TranslatedText);
}
