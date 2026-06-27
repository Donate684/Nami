using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nami.Models;

public class MalGenre
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}

public class MalData
{
    [JsonPropertyName("genres")] public List<MalGenre> Genres { get; set; } = new();
    [JsonPropertyName("themes")] public List<MalGenre> Themes { get; set; } = new();
    [JsonPropertyName("demographics")] public List<MalGenre> Demographics { get; set; } = new();
}

public class JikanResponse
{
    [JsonPropertyName("data")] public MalData? Data { get; set; }
}

public static class MalClient
{
    private static readonly HttpClient HttpClientInstance = new();

    static MalClient()
    {
        HttpClientInstance.Timeout = TimeSpan.FromSeconds(30);
        HttpClientInstance.DefaultRequestHeaders.UserAgent.ParseAdd("AnimeBBCodeFormatter/1.0");
    }

    public static string? ExtractMalId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = Regex.Match(url, @"myanimelist\.net/anime/(\d+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static readonly Dictionary<string, string> GenreMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Action", "Экшен" },
        { "Adventure", "Приключения" },
        { "Comedy", "Комедия" },
        { "Drama", "Драма" },
        { "Sci-Fi", "Научная фантастика" },
        { "Fantasy", "Фэнтези" },
        { "Romance", "Романтика" },
        { "Slice of Life", "Повседневность" },
        { "Supernatural", "Сверхъестественное" },
        { "Mystery", "Мистика" },
        { "Horror", "Ужасы" },
        { "Psychological", "Психологическое" },
        { "Thriller", "Триллер" },
        { "Ecchi", "Эччи" },
        { "Harem", "Гарем" },
        { "Mecha", "Меха" },
        { "Military", "Военное" },
        { "Music", "Музыка" },
        { "Parody", "Пародия" },
        { "Police", "Полиция" },
        { "Samurai", "Самураи" },
        { "School", "Школа" },
        { "Space", "Космос" },
        { "Sports", "Спорт" },
        { "Super Power", "Суперспособности" },
        { "Vampire", "Вампиры" },
        { "Historical", "Историческое" },
        { "Gourmet", "Кулинария" },
        { "Boys Love", "Сёнен-ай" },
        { "Girls Love", "Сёдзё-ай" },
        { "Kids", "Детское" },
        { "Magic", "Магия" },
        { "Martial Arts", "Боевые искусства" },
        { "Seinen", "Сэйнэн" },
        { "Shoujo", "Сёдзё" },
        { "Shounen", "Сёнен" },
        { "Erotica", "Эротика" },
        { "Award Winning", "Удостоено наград" },
        { "Avant Garde", "Авангард" },
        { "Suspense", "Саспенс" },
        { "Hentai", "Хентай" },
        { "Adult Cast", "Взрослые персонажи" },
        { "Anthropomorphic", "Антропоморфизм" },
        { "CGDCT", "Милые девочки делают милые вещи" },
        { "Childcare", "Воспитание детей" },
        { "Combat Sports", "Единоборства" },
        { "Crossdressing", "Кроссдрессинг" },
        { "Delinquents", "Хулиганы" },
        { "Detective", "Детектив" },
        { "Educational", "Образовательное" },
        { "Gag Humor", "Гэг-юмор" },
        { "Gore", "Гуро" },
        { "High Stakes Game", "Игры с высокими ставками" },
        { "Idols (Female)", "Айдолы (девушки)" },
        { "Idols (Male)", "Айдолы (парни)" },
        { "Isekai", "Исекай" },
        { "Iyashikei", "Иясикэй" },
        { "Love Polygon", "Любовный многоугольник" },
        { "Love Status Quo", "Любовный статус-кво" },
        { "Magical Sex Shift", "Магическая смена пола" },
        { "Mahou Shoujo", "Махо-сёдзё" },
        { "Organized Crime", "Криминал" },
        { "Otaku Culture", "Отаку-культура" },
        { "Performing Arts", "Сценическое искусство" },
        { "Pets", "Питомцы" },
        { "Racing", "Гонки" },
        { "Reincarnation", "Перерождение" },
        { "Reverse Harem", "Обратный гарем" },
        { "Showbiz", "Шоу-бизнес" },
        { "Strategy Game", "Стратегические игры" },
        { "Survival", "Выживание" },
        { "Team Sports", "Командные виды спорта" },
        { "Time Travel", "Путешествия во времени" },
        { "Urban Fantasy", "Городское фэнтези" },
        { "Video Game", "Видеоигры" },
        { "Villainess", "Злодейка" },
        { "Visual Arts", "Изобразительное искусство" },
        { "Workplace", "Рабочие будни" },
        { "Josei", "Дзёсей" },
        { "Medical", "Медицина" },
        { "Mythology", "Мифология" }
    };

    public static async Task<string> FetchGenresAsync(string malId)
    {
        try
        {
            string url = $"https://api.jikan.moe/v4/anime/{malId}";
            var response = await HttpClientInstance.GetAsync(url);
            if (!response.IsSuccessStatusCode) return string.Empty;

            string json = await response.Content.ReadAsStringAsync();
            var malResponse = JsonSerializer.Deserialize<JikanResponse>(json);
            if (malResponse?.Data == null) return string.Empty;

            var result = new List<string>();
            
            // Only use main Genres, excluding Themes and Demographics as requested
            var allGenres = malResponse.Data.Genres;

            foreach (var g in allGenres)
            {
                if (GenreMap.TryGetValue(g.Name, out var ruName))
                {
                    result.Add(ruName);
                }
                else
                {
                    result.Add(g.Name);
                }
            }

            return string.Join(", ", result);
        }
        catch
        {
            return string.Empty;
        }
    }
}
