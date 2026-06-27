using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nami.Models;

public class ShikimoriAnime
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("myanimelist_id")] public long? MyAnimeListId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("russian")] public string Russian { get; set; } = string.Empty;
    [JsonPropertyName("image")] public ShikimoriImage? Image { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
    [JsonPropertyName("episodes")] public int Episodes { get; set; }
    [JsonPropertyName("duration")] public int Duration { get; set; }
    [JsonPropertyName("aired_on")] public string? AiredOn { get; set; }
    [JsonPropertyName("released_on")] public string? ReleasedOn { get; set; }
    [JsonPropertyName("rating")] public string Rating { get; set; } = string.Empty;
    [JsonPropertyName("english")] public List<string?> English { get; set; } = new();
    [JsonPropertyName("japanese")] public List<string?> Japanese { get; set; } = new();
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("genres")] public List<ShikimoriGenre> Genres { get; set; } = new();
    [JsonPropertyName("studios")] public List<ShikimoriStudio> Studios { get; set; } = new();
    [JsonPropertyName("videos")] public List<ShikimoriVideo> Videos { get; set; } = new();
    [JsonPropertyName("screenshots")] public List<ShikimoriScreenshot> Screenshots { get; set; } = new();
}

public class ShikimoriImage
{
    [JsonPropertyName("original")] public string Original { get; set; } = string.Empty;
    [JsonPropertyName("preview")] public string Preview { get; set; } = string.Empty;
}

public class ShikimoriGenre
{
    [JsonPropertyName("russian")] public string Russian { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}

public class ShikimoriStudio
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}

public class ShikimoriVideo
{
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
}

public class ShikimoriScreenshot
{
    [JsonPropertyName("original")] public string Original { get; set; } = string.Empty;
    [JsonPropertyName("preview")] public string Preview { get; set; } = string.Empty;
}

public class ShikimoriExternalLink
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}

public class ShikimoriRole
{
    [JsonPropertyName("roles")] public List<string> Roles { get; set; } = new();
    [JsonPropertyName("person")] public ShikimoriPerson? Person { get; set; }
}

public class ShikimoriPerson
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("russian")] public string Russian { get; set; } = string.Empty;
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}

public class ShikimoriAnimeData
{
    public ShikimoriAnime? Anime { get; set; }
    public List<ShikimoriExternalLink> ExternalLinks { get; set; } = new();
    public List<ShikimoriRole> Roles { get; set; } = new();
}

public static class ShikimoriClient
{
    private static readonly HttpClient HttpClientInstance;

    static ShikimoriClient()
    {
        HttpClientInstance = new HttpClient();
        HttpClientInstance.Timeout = TimeSpan.FromSeconds(30);
        // Shikimori API blocks requests without a custom user agent with a 403 Forbidden
        HttpClientInstance.DefaultRequestHeaders.UserAgent.ParseAdd("AnimeBBCodeFormatter/1.0 (contact@example.com)");
    }

    public static (string? Id, string Domain) ExtractAnimeInfo(string input)
    {
        string defaultDomain = "shikimori.one";
        if (string.IsNullOrWhiteSpace(input)) return (null, defaultDomain);

        input = input.Trim();

        // If it's purely a numeric ID, use it directly
        if (Regex.IsMatch(input, @"^\d+$"))
        {
            return (input, defaultDomain);
        }

        // Try to match URL with domain
        var match = Regex.Match(input, @"https?://(shikimori\.[a-z]+)/animes/(\d+)");
        if (match.Success) return (match.Groups[2].Value, match.Groups[1].Value);

        // Try to match without protocol but with domain
        match = Regex.Match(input, @"(shikimori\.[a-z]+)/animes/(\d+)");
        if (match.Success) return (match.Groups[2].Value, match.Groups[1].Value);

        // Try to match just /animes/12345
        match = Regex.Match(input, @"(?:/animes/)(\d+)");
        if (match.Success) return (match.Groups[1].Value, defaultDomain);

        return (null, defaultDomain);
    }

    public static async Task<ShikimoriAnimeData> FetchAnimeDataAsync(string animeId, string domain)
    {
        var data = new ShikimoriAnimeData();

        // 1. Fetch main anime info
        string animeUrl = $"https://{domain}/api/animes/{animeId}";
        var response = await HttpClientInstance.GetAsync(animeUrl);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Shikimori API error: {response.StatusCode}");

        string animeJson = await response.Content.ReadAsStringAsync();
        data.Anime = JsonSerializer.Deserialize<ShikimoriAnime>(animeJson);

        // 2. Fetch external links
        try
        {
            string linksUrl = $"https://{domain}/api/animes/{animeId}/external_links";
            var linksResponse = await HttpClientInstance.GetAsync(linksUrl);
            if (linksResponse.IsSuccessStatusCode)
            {
                string linksJson = await linksResponse.Content.ReadAsStringAsync();
                data.ExternalLinks = JsonSerializer.Deserialize<List<ShikimoriExternalLink>>(linksJson) ?? new();
            }
        }
        catch
        {
            // Ignore links load failures, they aren't critical
        }

        // 3. Fetch staff roles
        try
        {
            string rolesUrl = $"https://{domain}/api/animes/{animeId}/roles";
            var rolesResponse = await HttpClientInstance.GetAsync(rolesUrl);
            if (rolesResponse.IsSuccessStatusCode)
            {
                string rolesJson = await rolesResponse.Content.ReadAsStringAsync();
                data.Roles = JsonSerializer.Deserialize<List<ShikimoriRole>>(rolesJson) ?? new();
            }
        }
        catch
        {
            // Ignore roles load failures, they aren't critical
        }

        return data;
    }
}
