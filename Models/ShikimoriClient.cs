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
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
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
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        HttpClientInstance = new HttpClient(handler);
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
        // Try modern GraphQL API v2 first (single fast query with complete data including genres, staff, links)
        try
        {
            var gqlData = await FetchAnimeDataViaGraphQLAsync(animeId, domain);
            if (gqlData?.Anime != null)
            {
                return gqlData;
            }
        }
        catch
        {
            // Fall back to legacy REST API below
        }

        return await FetchAnimeDataViaRestAsync(animeId, domain);
    }

    private static async Task<ShikimoriAnimeData?> FetchAnimeDataViaGraphQLAsync(string animeId, string domain)
    {
        string query = @"query($id: String) {
          animes(ids: $id) {
            id
            malId
            name
            russian
            japanese
            english
            kind
            rating
            episodes
            duration
            airedOn { date }
            releasedOn { date }
            url
            description
            poster { originalUrl mainUrl }
            genres { id name russian kind }
            studios { id name }
            externalLinks { id kind url }
            personRoles {
              rolesEn
              rolesRu
              person { id name russian url }
            }
            videos { id url name kind }
            screenshots { id originalUrl x332Url }
          }
        }";

        var payload = new
        {
            query = query,
            variables = new { id = animeId }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        var response = await HttpClientInstance.PostAsync($"https://{domain}/api/graphql", content);
        if (!response.IsSuccessStatusCode) return null;

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var dataElem)) return null;
        if (!dataElem.TryGetProperty("animes", out var animesElem) || animesElem.GetArrayLength() == 0) return null;

        var a = animesElem[0];

        long parsedId = 0;
        if (a.TryGetProperty("id", out var idProp))
        {
            if (idProp.ValueKind == JsonValueKind.String)
                long.TryParse(idProp.GetString(), out parsedId);
            else if (idProp.ValueKind == JsonValueKind.Number)
                idProp.TryGetInt64(out parsedId);
        }

        long? parsedMalId = null;
        if (a.TryGetProperty("malId", out var malProp))
        {
            if (malProp.ValueKind == JsonValueKind.Number && malProp.TryGetInt64(out var mId))
                parsedMalId = mId;
            else if (malProp.ValueKind == JsonValueKind.String && long.TryParse(malProp.GetString(), out var mIdStr))
                parsedMalId = mIdStr;
        }

        var anime = new ShikimoriAnime
        {
            Id = parsedId,
            MyAnimeListId = parsedMalId,
            Name = a.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "",
            Russian = a.TryGetProperty("russian", out var ruProp) ? ruProp.GetString() ?? "" : "",
            Kind = a.TryGetProperty("kind", out var kindProp) ? kindProp.GetString() ?? "" : "",
            Rating = a.TryGetProperty("rating", out var ratProp) ? ratProp.GetString() ?? "" : "",
            Episodes = a.TryGetProperty("episodes", out var epProp) && epProp.TryGetInt32(out var ep) ? ep : 0,
            Duration = a.TryGetProperty("duration", out var durProp) && durProp.TryGetInt32(out var dur) ? dur : 0,
            Url = a.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? "" : "",
            Description = a.TryGetProperty("description", out var descProp) ? descProp.GetString() : null
        };

        if (a.TryGetProperty("airedOn", out var airedElem) && airedElem.TryGetProperty("date", out var adProp))
            anime.AiredOn = adProp.GetString();
        if (a.TryGetProperty("releasedOn", out var relElem) && relElem.TryGetProperty("date", out var rdProp))
            anime.ReleasedOn = rdProp.GetString();

        if (a.TryGetProperty("japanese", out var jpProp) && !string.IsNullOrEmpty(jpProp.GetString()))
            anime.Japanese = new List<string?> { jpProp.GetString() };
        if (a.TryGetProperty("english", out var enProp) && !string.IsNullOrEmpty(enProp.GetString()))
            anime.English = new List<string?> { enProp.GetString() };

        if (a.TryGetProperty("poster", out var posterElem))
        {
            anime.Image = new ShikimoriImage
            {
                Original = posterElem.TryGetProperty("originalUrl", out var orig) ? orig.GetString() ?? "" : "",
                Preview = posterElem.TryGetProperty("mainUrl", out var main) ? main.GetString() ?? "" : ""
            };
        }

        // Genres
        if (a.TryGetProperty("genres", out var genresElem))
        {
            var mainList = new List<ShikimoriGenre>();
            var allList = new List<ShikimoriGenre>();
            foreach (var g in genresElem.EnumerateArray())
            {
                var genre = new ShikimoriGenre
                {
                    Name = g.TryGetProperty("name", out var gn) ? gn.GetString() ?? "" : "",
                    Russian = g.TryGetProperty("russian", out var gr) ? gr.GetString() ?? "" : "",
                    Kind = g.TryGetProperty("kind", out var gk) ? gk.GetString() ?? "" : ""
                };
                allList.Add(genre);
                if (genre.Kind.Equals("genre", StringComparison.OrdinalIgnoreCase))
                    mainList.Add(genre);
            }
            anime.Genres = mainList.Count > 0 ? mainList : allList;
        }

        // Studios
        if (a.TryGetProperty("studios", out var studiosElem))
        {
            foreach (var s in studiosElem.EnumerateArray())
            {
                long sidVal = 0;
                if (s.TryGetProperty("id", out var sid))
                {
                    if (sid.ValueKind == JsonValueKind.String) long.TryParse(sid.GetString(), out sidVal);
                    else if (sid.ValueKind == JsonValueKind.Number) sid.TryGetInt64(out sidVal);
                }
                anime.Studios.Add(new ShikimoriStudio
                {
                    Id = sidVal,
                    Name = s.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : ""
                });
            }
        }

        // Videos
        if (a.TryGetProperty("videos", out var videosElem))
        {
            foreach (var v in videosElem.EnumerateArray())
            {
                anime.Videos.Add(new ShikimoriVideo
                {
                    Url = v.TryGetProperty("url", out var vu) ? vu.GetString() ?? "" : "",
                    Kind = v.TryGetProperty("kind", out var vk) ? vk.GetString() ?? "" : ""
                });
            }
        }

        // Screenshots
        if (a.TryGetProperty("screenshots", out var shotsElem))
        {
            foreach (var sc in shotsElem.EnumerateArray())
            {
                anime.Screenshots.Add(new ShikimoriScreenshot
                {
                    Original = sc.TryGetProperty("originalUrl", out var so) ? so.GetString() ?? "" : "",
                    Preview = sc.TryGetProperty("x332Url", out var sp) ? sp.GetString() ?? "" : ""
                });
            }
        }

        var result = new ShikimoriAnimeData { Anime = anime };

        // External Links
        if (a.TryGetProperty("externalLinks", out var linksElem))
        {
            foreach (var el in linksElem.EnumerateArray())
            {
                string linkKind = el.TryGetProperty("kind", out var lk) ? lk.GetString() ?? "" : "";
                string linkUrl = el.TryGetProperty("url", out var lu) ? lu.GetString() ?? "" : "";
                result.ExternalLinks.Add(new ShikimoriExternalLink
                {
                    Kind = linkKind,
                    Url = linkUrl
                });

                if (anime.MyAnimeListId == null && linkKind.Equals("myanimelist", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(linkUrl, @"myanimelist\.net/anime/(\d+)");
                    if (match.Success && long.TryParse(match.Groups[1].Value, out var malId))
                    {
                        anime.MyAnimeListId = malId;
                    }
                }
            }
        }

        // Roles
        if (a.TryGetProperty("personRoles", out var rolesElem))
        {
            foreach (var pr in rolesElem.EnumerateArray())
            {
                var roleObj = new ShikimoriRole();
                if (pr.TryGetProperty("rolesEn", out var reElem))
                {
                    foreach (var r in reElem.EnumerateArray())
                    {
                        string rStr = r.GetString() ?? "";
                        if (!string.IsNullOrEmpty(rStr)) roleObj.Roles.Add(rStr);
                    }
                }
                if (pr.TryGetProperty("person", out var pElem))
                {
                    long pidVal = 0;
                    if (pElem.TryGetProperty("id", out var pid))
                    {
                        if (pid.ValueKind == JsonValueKind.String) long.TryParse(pid.GetString(), out pidVal);
                        else if (pid.ValueKind == JsonValueKind.Number) pid.TryGetInt64(out pidVal);
                    }
                    roleObj.Person = new ShikimoriPerson
                    {
                        Id = pidVal,
                        Name = pElem.TryGetProperty("name", out var pn) ? pn.GetString() ?? "" : "",
                        Russian = pElem.TryGetProperty("russian", out var prus) ? prus.GetString() ?? "" : "",
                        Url = pElem.TryGetProperty("url", out var pu) ? pu.GetString() ?? "" : ""
                    };
                }
                result.Roles.Add(roleObj);
            }
        }

        return result;
    }

    private static async Task<ShikimoriAnimeData> FetchAnimeDataViaRestAsync(string animeId, string domain)
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
