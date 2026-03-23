using System.Net.Http.Json;

namespace DataBaseApi.Services;

public class LibreTranslateService
{
    private static readonly HttpClient _http = new();
    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly ILogger<LibreTranslateService> _logger;


    public LibreTranslateService(IConfiguration config, ILogger<LibreTranslateService> logger)
    {
        _baseUrl = (config["LibreTranslate:Url"] ?? "").TrimEnd('/');
        _apiKey = config["LibreTranslate:ApiKey"];
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_baseUrl);

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
