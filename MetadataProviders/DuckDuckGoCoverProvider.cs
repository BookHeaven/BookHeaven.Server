using System.Text.RegularExpressions;
using System.Web;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.MetadataProviders.Abstractions;
using BookHeaven.Server.MetadataProviders.DTO;

namespace BookHeaven.Server.MetadataProviders;

public partial class DuckDuckGoCoverProvider(ILogger<DuckDuckGoCoverProvider> logger) : ICoverProvider
{
    private const string BASE_URL = "https://duckduckgo.com/";
    private const string USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
    private const string REFERRER = "https://duckduckgo.com/";
    private const string ACCEPT = "application/json, text/javascript";
    
    private readonly List<string> _preferredSources =
    [
        "goodreads.com",
        "amazon.com"
    ];
    
    private readonly List<string> _parameters =
    [
        "size:Large",
        "layout:Tall"
    ];
    
    private readonly List<(double width, double height)> _ratios =
    [
        (1, 1.4),
        (1, 1.5),
        (1, 1.6)
    ];
    
    private readonly List<string> _preferredFileTypes =
    [
        ".jpg",
        ".jpeg"
    ];
    
    private string Sources => HttpUtility.UrlEncode("(" + string.Join(" OR ", _preferredSources.Select(s => $"site:{s}")) + ")");
    private string Parameters => "&iar=images&iaf=" + HttpUtility.UrlEncode(string.Join(",", _parameters));
    
    [GeneratedRegex(@"vqd=([-\d]+)", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();
    
    private static readonly HttpClientHandler HttpClientHandler = new() { AllowAutoRedirect = true, UseCookies = true, CookieContainer = new() };
    private static readonly HttpClient HttpClient = new(HttpClientHandler);
    
    public async Task<Result<List<string>>> GetCoversAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.UrlEncode(request.Title + (string.IsNullOrWhiteSpace(request.Author) ? string.Empty : $" {request.Author}") + " book");
        
        try
        {
            var token = await GetVqdTokenAsync(query, cancellationToken);
            var preferredResults = await FetchImageUrlsAsync(query, token, cancellationToken);
            
            var newToken = await GetVqdTokenAsync(query, cancellationToken, true);
            var otherResults = await FetchImageUrlsAsync(query, newToken, cancellationToken, true);
            
            return preferredResults.Concat(otherResults).Distinct().ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "DuckDuckGo: Error fetching covers");
            return new Error("Network error while fetching covers");
        }
    }
    
    private async Task<string> GetVqdTokenAsync(string query, CancellationToken cancellationToken, bool ignorePreferredSources = false)
    {
        var searchUrl = $"{BASE_URL}/?t=h_&q={query}{(ignorePreferredSources ? "" : $"+{Sources}")}{Parameters}";
        var searchResponse = await MakeRequestAsync(searchUrl, cancellationToken);
        
        var tokenMatch = TokenRegex().Match(searchResponse);
        return !tokenMatch.Success ? throw new InvalidOperationException("Unable to find vqd token in DuckDuckGo response") : tokenMatch.Groups[1].Value;
    }
    
    private async Task<List<string>> FetchImageUrlsAsync(string query, string token, CancellationToken cancellationToken, bool ignorePreferredSources = false)
    {
        var apiUrl = $"{BASE_URL}i.js?o=json&q={query}{(ignorePreferredSources ? "" : $"+{Sources}")}{Parameters}&vqd={token}";
        var apiResponse = await MakeRequestAsync(apiUrl, cancellationToken, true);
        
        var imageUrls = new List<string>();
        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(apiResponse);
            if (jsonDoc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var result in results.EnumerateArray())
                {
                    
                    var imageUrl = result.TryGetProperty("image", out var imageElement) ? imageElement.GetString() : string.Empty;
                    if(string.IsNullOrWhiteSpace(imageUrl) || !_preferredFileTypes.Any(t => imageUrl.EndsWith(t))) continue;
                    
                    var width = result.TryGetProperty("width", out var widthElement) ? widthElement.GetInt32() : 0;
                    var height = result.TryGetProperty("height", out var heightElement) ? heightElement.GetInt32() : 0;
                    
                    // Ignore images if ratio is not close to preferred ratios or width is too small
                    if (width < 350 || !_ratios.Any(r => Math.Abs((double)width / height - r.width / r.height) < 0.05))
                        continue;
                    
                    imageUrls.Add(imageUrl);
                }
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogError(ex, "DuckDuckGo: Error parsing JSON response");
        }

        return imageUrls;
    }

    private static async Task<string> MakeRequestAsync(string url, CancellationToken cancellationToken, bool ignoreContentType = false)
    {
        HttpClient.DefaultRequestHeaders.UserAgent.Clear();
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);
        HttpClient.DefaultRequestHeaders.Referrer = new Uri(REFERRER);
        HttpClient.DefaultRequestHeaders.Accept.Clear();
        HttpClient.DefaultRequestHeaders.Accept.ParseAdd(ignoreContentType ? "*/*" : ACCEPT);
        
        var response = await HttpClient.GetAsync(url, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    
}