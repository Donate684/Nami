using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nami.Models;

public class JikanEpisode
{
    [JsonPropertyName("mal_id")] public long MalId { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("title_japanese")] public string? TitleJapanese { get; set; }
    [JsonPropertyName("title_romanji")] public string? TitleRomanji { get; set; }
}

public class JikanPagination
{
    [JsonPropertyName("has_next_page")] public bool HasNextPage { get; set; }
}

public class JikanEpisodesResponse
{
    [JsonPropertyName("data")] public List<JikanEpisode> Data { get; set; } = new();
    [JsonPropertyName("pagination")] public JikanPagination? Pagination { get; set; }
}

public static class JikanClient
{
    private static readonly HttpClient HttpClientInstance;

    static JikanClient()
    {
        HttpClientInstance = new HttpClient();
        HttpClientInstance.Timeout = TimeSpan.FromSeconds(30);
        // Jikan requires a meaningful User-Agent
        HttpClientInstance.DefaultRequestHeaders.UserAgent.ParseAdd("AnimeBBCodeFormatter/1.0 (contact@example.com)");
    }

    public static async Task<List<JikanEpisode>> FetchAllEpisodesAsync(long malId)
    {
        var allEpisodes = new List<JikanEpisode>();
        int page = 1;
        bool hasNextPage = true;
        int maxPages = 20;

        while (hasNextPage && page <= maxPages)
        {
            string url = $"https://api.jikan.moe/v4/anime/{malId}/episodes?page={page}";
            var response = await HttpClientInstance.GetAsync(url);
            
            // If it returns 404, there are no episodes or invalid ID
            if (!response.IsSuccessStatusCode)
                break;

            string json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JikanEpisodesResponse>(json);
            
            if (result?.Data != null)
            {
                allEpisodes.AddRange(result.Data);
            }

            hasNextPage = result?.Pagination?.HasNextPage ?? false;
            page++;

            // Rate limit for Jikan is 3 requests per second
            if (hasNextPage)
            {
                await Task.Delay(400);
            }
        }

        return allEpisodes;
    }
}
