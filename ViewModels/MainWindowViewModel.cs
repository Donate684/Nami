using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using Nami.Models;

namespace Nami.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Shikimori Import
    [ObservableProperty] private string _shikimoriImportUrl = string.Empty;
    [ObservableProperty] private string _importButtonText = "Загрузить с Shikimori";

    // Title Block
    [ObservableProperty] private string _titleJpKanji = string.Empty;
    [ObservableProperty] private string _titleJpRomaji = string.Empty;
    [ObservableProperty] private string _titleEn = string.Empty;
    [ObservableProperty] private string _titleRu = string.Empty;
    [ObservableProperty] private string _titleLocalized = string.Empty;

    // Poster
    [ObservableProperty] private string _posterUrl = string.Empty;

    // Specifications
    [ObservableProperty] private string _genre = string.Empty;
    [ObservableProperty] private string _releaseType = "ТВ";
    [ObservableProperty] private string _duration = string.Empty;
    [ObservableProperty] private string _releaseDate = string.Empty;

    // Staff
    [ObservableProperty] private string _authorName = string.Empty;
    [ObservableProperty] private string _authorUrl = string.Empty;
    [ObservableProperty] private string _directorName = string.Empty;
    [ObservableProperty] private string _directorUrl = string.Empty;
    [ObservableProperty] private string _studioName = string.Empty;
    [ObservableProperty] private string _studioUrl = string.Empty;

    // Anime Resources Links
    [ObservableProperty] private string _shikimoriUrl = string.Empty;
    [ObservableProperty] private string _worldArtUrl = string.Empty;
    [ObservableProperty] private string _annUrl = string.Empty;
    [ObservableProperty] private string _malUrl = string.Empty;
    [ObservableProperty] private string _syoboiUrl = string.Empty;
    [ObservableProperty] private string _anidbUrl = string.Empty;

    // Age Rating
    [ObservableProperty] private string _ageRating = "18+";
    [ObservableProperty] private string _ageDescription = "Для зрителей старше 18 лет, запрещено для детей";

    // Description
    [ObservableProperty] private string _description = string.Empty;

    // Technical specs
    [ObservableProperty] private string _videoQuality = "WEBRip";
    [ObservableProperty] private string _releaseGroup = string.Empty;
    [ObservableProperty] private string _videoInfo = string.Empty;
    [ObservableProperty] private string _audioInfo = string.Empty;
    [ObservableProperty] private string _audioLanguage = "Японский";
    [ObservableProperty] private string _subtitles = string.Empty;
    [ObservableProperty] private string _translation = string.Empty;
    [ObservableProperty] private string _internalSubtitles = string.Empty;
    [ObservableProperty] private string _internalTranslation = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InternalSubtitlesLabel))]
    [NotifyPropertyChangedFor(nameof(InternalTranslationLabel))]
    private bool _isAuthor = false;

    public string InternalSubtitlesLabel => IsAuthor ? "СУБТИТРЫ (В КОНТЕЙНЕРЕ)" : "СУБТИТРЫ";
    public string InternalTranslationLabel => IsAuthor ? "ПЕРЕВОД (В КОНТЕЙНЕРЕ)" : "ПЕРЕВОД";

    partial void OnIsAuthorChanged(bool value)
    {
        UpdateOutputs();
    }

    // MediaInfo Raw Input
    [ObservableProperty] private string _mediaInfo = string.Empty;

    // Extras
    [ObservableProperty] private bool _includeTrailer = false;
    partial void OnIncludeTrailerChanged(bool value)
    {
        UpdateOutputs();
    }

    [ObservableProperty] private string _trailerUrl = string.Empty;
    [ObservableProperty] private string _screenshots = string.Empty;
    [ObservableProperty] private string _episodes = string.Empty;

    // ImageBan Album Tracking
    [ObservableProperty] private string _currentAlbumId = string.Empty;

    // ImageBan API keys
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SecretKeyStatus))]
    private string _imageBanSecretKey = string.Empty;

    public string SecretKeyStatus => string.IsNullOrWhiteSpace(ImageBanSecretKey) ? "Не настроен (загрузка недоступна)" : "Настроен и готов к работе ✓";

    // Output properties
    [ObservableProperty] private string _bbCodeOutput = string.Empty;
    [ObservableProperty] private string _templateContent = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _presets = new();
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomPresetSelected))]
    [NotifyPropertyChangedFor(nameof(IsDefaultPresetSelected))]
    private string _selectedPreset = string.Empty;
    
    public bool IsCustomPresetSelected => SelectedPreset != "Default";
    public bool IsDefaultPresetSelected => SelectedPreset == "Default";
    
    private bool _isLoadingTemplate = false;
    private bool _suppressTemplateLoad = false;
    private bool _isImporting = false;
    private bool _isCreatingScreenshots = false;
    [ObservableProperty] private string _copyText = "Скопировать BBCode";
    [ObservableProperty] private string _copyTopicTitleText = "Скопировать заголовок";
    [ObservableProperty] private string _parseButtonText = "Разобрать лог MediaInfo";
    [ObservableProperty] private string _selectFileButtonText = "Выбрать видео-файл для анализа...";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVideoLoaded))]
    private string _selectedVideoFilePath = string.Empty;
    [ObservableProperty] private string _createScreenshotsButtonText = "Создать скриншоты...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyPropertyChangedFor(nameof(CanSelectFile))]
    private bool _isImportDone = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSelectFile))]
    [NotifyPropertyChangedFor(nameof(CanCreateScreenshots))]
    private bool _isFileSelectionDone = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateScreenshots))]
    private bool _isScreenshotsDone = false;

    [ObservableProperty] private bool _isWelcomeOverlayVisible = false;

    public bool IsVideoLoaded => !string.IsNullOrEmpty(SelectedVideoFilePath);
    public bool CanImport => !IsImportDone;
    public bool CanSelectFile => IsImportDone && !IsFileSelectionDone;
    public bool CanCreateScreenshots => IsFileSelectionDone && !IsScreenshotsDone;

    public ObservableCollection<string> ReleaseTypes { get; } = new()
    {
        "ТВ", "ONA", "OVA", "Фильм", "Спешл", "Промо"
    };

    public ObservableCollection<string> AgeRatings { get; } = new()
    {
        "18+", "16+", "12+", "6+", "0+"
    };

    public MainWindowViewModel()
    {
        LoadSettings();
        if (string.IsNullOrWhiteSpace(ImageBanSecretKey))
        {
            IsWelcomeOverlayVisible = true;
        }
        
        RefreshPresetsList();
        LoadTemplate();
        UpdateOutputs();
    }

    [RelayCommand]
    private void SaveApiKeys()
    {
        if (!string.IsNullOrWhiteSpace(ImageBanSecretKey))
        {
            IsWelcomeOverlayVisible = false;
            SaveSettings();
        }
    }

    [RelayCommand]
    private void OpenImageBanProfile()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://imageban.ru/u/profile",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private static readonly HashSet<string> _ignoredProperties = new()
    {
        nameof(BbCodeOutput), nameof(CopyText), nameof(ParseButtonText),
        nameof(CopyTopicTitleText), nameof(ImportButtonText),
        nameof(SelectFileButtonText), nameof(CreateScreenshotsButtonText),
        nameof(SecretKeyStatus), nameof(SelectedPreset),
        nameof(IsImportDone), nameof(IsFileSelectionDone), nameof(IsScreenshotsDone),
        nameof(IsWelcomeOverlayVisible)
    };

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName != null && !_ignoredProperties.Contains(e.PropertyName))
        {
            UpdateOutputs();
        }

        if (e.PropertyName != null)
        {
            if (e.PropertyName == nameof(Translation) || e.PropertyName == nameof(Subtitles) ||
                e.PropertyName == nameof(ImageBanSecretKey) ||
                e.PropertyName == nameof(IsAuthor) || e.PropertyName == nameof(IncludeTrailer))
            {
                SaveSettings();
            }
            else if (e.PropertyName == nameof(TemplateContent))
            {
                if (!_isLoadingTemplate)
                {
                    SaveTemplate();
                }
            }
            else if (e.PropertyName == nameof(SelectedPreset))
            {
                SaveSettings();
                if (!_suppressTemplateLoad)
                {
                    LoadTemplate();
                }
            }
        }
    }

    partial void OnAgeRatingChanged(string value)
    {
        AgeDescription = value switch
        {
            "18+" => "Для зрителей старше 18 лет, запрещено для детей",
            "16+" => "Для зрителей старше 16 лет",
            "12+" => "Для зрителей старше 12 лет",
            "6+" => "Для зрителей старше 6 лет",
            "0+" => "Для всех возрастов",
            _ => AgeDescription
        };
    }

    private void UpdateOutputs()
    {
        if (_isImporting) return;

        string jpPart = string.IsNullOrWhiteSpace(TitleJpKanji) ? TitleJpRomaji : (string.IsNullOrWhiteSpace(TitleJpRomaji) ? TitleJpKanji : $"{TitleJpKanji} | {TitleJpRomaji}");
        string enRuPart = string.IsNullOrWhiteSpace(TitleEn) ? TitleRu : (string.IsNullOrWhiteSpace(TitleRu) ? TitleEn : $"{TitleEn}  |  {TitleRu}");
        string topPart = string.Join("\n", new[] { jpPart, enRuPart }.Where(s => !string.IsNullOrWhiteSpace(s)));

        string formattedScreenshots = "";
        if (!string.IsNullOrWhiteSpace(Screenshots))
        {
            string trimmed = Screenshots.Trim();
            if (trimmed.Contains("[align=") || trimmed.Contains("Скриншоты"))
                formattedScreenshots = trimmed + "\n";
            else
                formattedScreenshots = $"[brc][align=center][b]Скриншоты:[/b]\n{FormatScreenshots(Screenshots)}[/align]\n";
        }

        bool hasLinks = !string.IsNullOrWhiteSpace(ShikimoriUrl) || !string.IsNullOrWhiteSpace(WorldArtUrl) || 
                        !string.IsNullOrWhiteSpace(AnnUrl) || !string.IsNullOrWhiteSpace(MalUrl) || 
                        !string.IsNullOrWhiteSpace(AnidbUrl) || !string.IsNullOrWhiteSpace(SyoboiUrl);

        var values = new Dictionary<string, string?>
        {
            { "TitleTopPart", topPart },
            { "TitleJpKanji", TitleJpKanji },
            { "TitleJpRomaji", TitleJpRomaji },
            { "TitleEn", TitleEn },
            { "TitleRu", TitleRu },
            { "TitleLocalized", TitleLocalized },
            { "PosterUrl", PosterUrl?.Trim() },
            { "ReleaseType", ReleaseType },
            { "Genre", Genre },
            { "ReleaseDate", ReleaseDate },
            { "Duration", Duration },
            { "AgeRating", AgeRating },
            { "Director", DirectorName },
            { "Studio", StudioName },
            { "Author", AuthorName },
            { "Description", Description },
            { "ShikimoriUrl", ShikimoriUrl?.Trim() },
            { "WorldArtUrl", WorldArtUrl?.Trim() },
            { "AnnUrl", AnnUrl?.Trim() },
            { "MalUrl", MalUrl?.Trim() },
            { "AnidbUrl", AnidbUrl?.Trim() },
            { "SyoboiUrl", SyoboiUrl?.Trim() },
            { "AudioLanguage", AudioLanguage },
            { "Subtitles", Subtitles },
            { "Translation", Translation },
            { "InternalSubtitles", InternalSubtitles },
            { "InternalTranslation", InternalTranslation },
            { "VideoQuality", VideoQuality },
            { "ReleaseGroup", ReleaseGroup },
            { "VideoInfo", VideoInfo },
            { "AudioInfo", AudioInfo },
            { "Screenshots", Screenshots },
            { "FormattedScreenshots", formattedScreenshots },
            { "Episodes", Episodes },
            { "MediaInfo", MediaInfo?.TrimEnd() },
            { "IsAuthor", IsAuthor ? "true" : null },
            { "HasLinks", hasLinks ? "true" : null },
            { "TrailerUrl", IncludeTrailer && !string.IsNullOrWhiteSpace(TrailerUrl) ? TrailerUrl.Trim() : null }
        };

        string template = string.IsNullOrWhiteSpace(TemplateContent) ? DefaultTemplate : TemplateContent;

        // Process [IF Field]...[/IF] tags (supports nesting and newline swallowing)
        bool changed = true;
        int maxIterations = 50;
        int iterations = 0;
        while (changed && iterations < maxIterations)
        {
            iterations++;
            string newTemplate = IfTagRegex.Replace(template, match =>
            {
                bool invert = match.Groups[1].Value == "!";
                string field = match.Groups[2].Value;
                string content = match.Groups[3].Value;
                string newline = match.Groups[4].Value;

                bool hasValue = values.TryGetValue(field, out string? val) && !string.IsNullOrWhiteSpace(val);
                bool conditionMet = invert ? !hasValue : hasValue;
                return conditionMet ? content + newline : "";
            });
            
            if (newTemplate == template) break;
            template = newTemplate;
        }

        // Replace placeholders
        foreach (var kvp in values)
        {
            template = template.Replace($"{{{kvp.Key}}}", kvp.Value ?? "");
        }

        BbCodeOutput = template;
    }

    private string FormatScreenshots(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var formatted = new List<string>();
        var bbCodeMatches = Regex.Matches(input, @"\[url=(?<url>[^\]]+)\]\s*\[img\](?<img>[^\[]+)\[/img\]\s*\[/url\]", RegexOptions.IgnoreCase);

        foreach (Match match in bbCodeMatches)
        {
            string url = match.Groups["url"].Value.Trim();
            string img = match.Groups["img"].Value.Trim();

            if (url.Contains("imageban.ru/show/") && img.Contains("imageban.ru/thumbs/"))
            {
                var thumbMatch = Regex.Match(img, @"i(?<server>\d+)\.imageban\.ru/thumbs/(?<year>\d+)\.(?<month>\d+)\.(?<day>\d+)/(?<hash>[a-f0-9]+)\.png", RegexOptions.IgnoreCase);
                if (thumbMatch.Success)
                {
                    url = $"https://i{thumbMatch.Groups["server"].Value}.imageban.ru/out/{thumbMatch.Groups["year"].Value}/{thumbMatch.Groups["month"].Value}/{thumbMatch.Groups["day"].Value}/{thumbMatch.Groups["hash"].Value}.png";
                }
            }
            formatted.Add($"[url={url}][img]{img}[/img][/url]");
        }

        string plainInput = Regex.Replace(input, @"\[url=[^\]]+\]\s*\[img\][^\[]+\[/img\]\s*\[/url\]", "", RegexOptions.IgnoreCase).Trim();
        if (!string.IsNullOrEmpty(plainInput))
        {
            var lines = plainInput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    formatted.Add($"[url={parts[0]}][img]{parts[1]}[/img][/url]");
                }
                else
                {
                    if (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        string nextLine = lines[i + 1].Trim();
                        formatted.Add($"[url={line}][img]{nextLine}[/img][/url]");
                        i++;
                    }
                    else
                    {
                        formatted.Add($"[url={line}][img]{line}[/img][/url]");
                    }
                }
            }
        }

        if (formatted.Count == 0) return input.Trim();

        var rows = new List<string>();
        for (int i = 0; i < formatted.Count; i += 4)
        {
            var rowItems = formatted.GetRange(i, Math.Min(4, formatted.Count - i));
            if (rowItems.Count == 4)
            {
                rows.Add($"{rowItems[0]} {rowItems[1]}   {rowItems[2]} {rowItems[3]}");
            }
            else
            {
                rows.Add(string.Join(" ", rowItems));
            }
        }
        
        return string.Join("\n\n", rows);
    }

    [RelayCommand]
    private async Task CopyBbCodeAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(BbCodeOutput);
                CopyText = "Скопировано! ✓";
                await Task.Delay(2000);
                CopyText = "Скопировать BBCode";
            }
        }
    }

    [RelayCommand]
    private async Task CopyTopicTitleAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                var titles = new List<string>();
                string jpPart = !string.IsNullOrWhiteSpace(TitleJpRomaji) ? TitleJpRomaji : TitleJpKanji;
                if (!string.IsNullOrWhiteSpace(jpPart)) titles.Add(jpPart.Trim());
                if (!string.IsNullOrWhiteSpace(TitleEn)) titles.Add(TitleEn.Trim());
                
                string ruPart = !string.IsNullOrWhiteSpace(TitleLocalized) ? TitleLocalized : TitleRu;
                if (!string.IsNullOrWhiteSpace(ruPart)) titles.Add(ruPart.Trim());
                
                string joinedTitles = string.Join(" | ", titles);

                string year = "202?";
                if (!string.IsNullOrWhiteSpace(ReleaseDate))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(ReleaseDate, @"\d{4}");
                    if (match.Success) year = match.Value;
                }

                string type = !string.IsNullOrWhiteSpace(ReleaseType) ? ReleaseType.Trim() : "TV";
                
                string resolution = "1080p";
                if (!string.IsNullOrWhiteSpace(VideoInfo))
                {
                    if (VideoInfo.Contains("3840x2160") || VideoInfo.Contains("2160p")) resolution = "2160p";
                    else if (VideoInfo.Contains("1920x1080") || VideoInfo.Contains("1080p")) resolution = "1080p";
                    else if (VideoInfo.Contains("1280x720") || VideoInfo.Contains("720p")) resolution = "720p";
                    else if (VideoInfo.Contains("720x480") || VideoInfo.Contains("480p")) resolution = "480p";
                }

                string quality = !string.IsNullOrWhiteSpace(VideoQuality) ? VideoQuality.Trim() : "WEBRip";
                
                bool isHevc = !string.IsNullOrWhiteSpace(VideoInfo) && (VideoInfo.Contains("HEVC", StringComparison.OrdinalIgnoreCase) || VideoInfo.Contains("H.265", StringComparison.OrdinalIgnoreCase));
                bool is10Bit = !string.IsNullOrWhiteSpace(VideoInfo) && (VideoInfo.Contains("10 bit", StringComparison.OrdinalIgnoreCase) || VideoInfo.Contains("10-bit", StringComparison.OrdinalIgnoreCase));

                string suffix = "raw";
                if (isHevc)
                {
                    suffix = is10Bit ? "HEVC 10-bit raw" : "HEVC raw";
                }
                else if (is10Bit)
                {
                    suffix = "10-bit raw";
                }

                string topicTitle = $"{joinedTitles} [{year}, {type}, ?? из ?? эп.] {quality} {resolution} {suffix}";

                await clipboard.SetTextAsync(topicTitle);
                CopyTopicTitleText = "Скопировано! ✓";
                await Task.Delay(2000);
                CopyTopicTitleText = "Скопировать заголовок";
            }
        }
    }

    [RelayCommand]
    private async Task ParseMediaInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(MediaInfo)) return;

        var result = MediaInfoParser.Parse(MediaInfo);

        if (!string.IsNullOrEmpty(result.VideoInfo))
            VideoInfo = result.VideoInfo;
        if (!string.IsNullOrEmpty(result.AudioInfo))
            AudioInfo = result.AudioInfo;
        if (!string.IsNullOrEmpty(result.AudioLanguage))
            AudioLanguage = result.AudioLanguage;
        if (!string.IsNullOrEmpty(result.ReleaseGroup))
            ReleaseGroup = result.ReleaseGroup;
        if (!string.IsNullOrEmpty(result.VideoQuality))
            VideoQuality = result.VideoQuality;
        
        // Fill internal subtitle and translation properties
        if (!string.IsNullOrEmpty(result.InternalSubtitles))
            InternalSubtitles = result.InternalSubtitles;
        if (!string.IsNullOrEmpty(result.InternalTranslation))
            InternalTranslation = result.InternalTranslation;

        ParseButtonText = "Успешно разобрано! ✓";
        await Task.Delay(2000);
        ParseButtonText = "Разобрать лог MediaInfo";
    }

    [RelayCommand]
    private async Task SelectAndAnalyzeFileAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime && desktopLifetime.MainWindow != null)
        {
            var storageProvider = desktopLifetime.MainWindow.StorageProvider;
            if (storageProvider != null)
            {
                var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Выберите видеофайл для анализа",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Видеофайлы")
                        {
                            Patterns = new[] { "*.mkv", "*.mp4", "*.avi", "*.ts", "*.m2ts", "*.webm", "*.wmv", "*.flv" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    var filePath = files[0].Path.LocalPath;
                    SelectedVideoFilePath = filePath;
                    CurrentAlbumId = string.Empty;
                    SelectFileButtonText = "Анализ файла... ⏳";
                    try
                    {
                        string rawText = await Task.Run(() =>
                        {
                            using var mi = new MediaInfoDllWrapper();
                            if (mi.Open(filePath))
                            {
                                string info = mi.Inform();
                                mi.Close();
                                return info;
                            }
                            return string.Empty;
                        });

                        if (!string.IsNullOrWhiteSpace(rawText))
                        {
                            MediaInfo = rawText;
                            await ParseMediaInfoAsync();
                            SelectFileButtonText = "Файл успешно проанализирован! ✓";
                            IsFileSelectionDone = true;
                        }
                        else
                        {
                            SelectFileButtonText = "Ошибка чтения файла ❌";
                        }
                    }
                    catch (DllNotFoundException)
                    {
                        SelectFileButtonText = "MediaInfo не найден! ❌";
                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                        {
                            var dialog = new Nami.Views.DialogWindow();
                            var result = await dialog.ShowDialog<bool>(desktop.MainWindow);
                            if (result)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "https://mediaarea.net/ru/MediaInfo/Download",
                                        UseShellExecute = true
                                    });
                                }
                                catch {}
                            }
                        }
                    }
                    catch (Exception)
                    {
                        SelectFileButtonText = "Ошибка при анализе! ❌";
                    }

                    await Task.Delay(2000);
                    SelectFileButtonText = "Выбрать видео-файл для анализа...";
                }
            }
        }
    }

    private async Task<string?> GetFfmpegPathAsync(Action<string>? statusCallback = null)
    {
        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(localPath)) return localPath;

        statusCallback?.Invoke("Скачивание FFmpeg... ⏳");
        try
        {
            await Xabe.FFmpeg.Downloader.FFmpegDownloader.GetLatestVersion(Xabe.FFmpeg.Downloader.FFmpegVersion.Official, AppDomain.CurrentDomain.BaseDirectory);
            if (File.Exists(localPath)) return localPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FFmpeg download failed: {ex}");
        }

        return null;
    }

    private int ParseDuration(string info)
    {
        var matches = Regex.Matches(info, @"Duration\s*:\s*(.*)");
        foreach (Match match in matches)
        {
            var val = match.Groups[1].Value.Trim();
            int hours = 0;
            int minutes = 0;
            int seconds = 0;

            var hMatch = Regex.Match(val, @"\b(\d+)\s*h\b");
            if (hMatch.Success) hours = int.Parse(hMatch.Groups[1].Value);

            var mMatch = Regex.Match(val, @"\b(\d+)\s*min\b");
            if (mMatch.Success) minutes = int.Parse(mMatch.Groups[1].Value);

            var sMatch = Regex.Match(val, @"\b(\d+)\s*s\b");
            if (sMatch.Success) seconds = int.Parse(sMatch.Groups[1].Value);

            int totalSeconds = hours * 3600 + minutes * 60 + seconds;
            if (totalSeconds > 0)
                return totalSeconds;
        }
        return 0;
    }

    private int GetDurationInSeconds()
    {
        int duration = 0;
        if (!string.IsNullOrWhiteSpace(MediaInfo))
        {
            duration = ParseDuration(MediaInfo);
        }

        if (duration == 0 && !string.IsNullOrWhiteSpace(SelectedVideoFilePath) && File.Exists(SelectedVideoFilePath))
        {
            try
            {
                using var mi = new MediaInfoDllWrapper();
                if (mi.Open(SelectedVideoFilePath))
                {
                    duration = ParseDuration(mi.Inform());
                }
            }
            catch { }
        }

        return duration;
    }

    [RelayCommand]
    private async Task CreateScreenshotsAsync()
    {
        if (_isCreatingScreenshots) return;
        _isCreatingScreenshots = true;

        try
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoFilePath) || !File.Exists(SelectedVideoFilePath))
            {
                var oldText = CreateScreenshotsButtonText;
                CreateScreenshotsButtonText = "Выберите видеофайл! ❌";
                await Task.Delay(2000);
                CreateScreenshotsButtonText = oldText;
                return;
            }

            string? ffmpegPath = await GetFfmpegPathAsync(status => CreateScreenshotsButtonText = status);
            if (ffmpegPath == null)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var dialog = new Nami.Views.DialogWindow(
                        "FFmpeg не найден", 
                        "Для нарезки скриншотов требуется установленный FFmpeg.\n\nХотите перейти на официальный сайт для скачивания?"
                    );
                    var result = await dialog.ShowDialog<bool>(desktop.MainWindow);
                    if (result)
                    {
                        try { Process.Start(new ProcessStartInfo { FileName = "https://ffmpeg.org/download.html", UseShellExecute = true }); } catch { }
                    }
                }
                return;
            }

            CreateScreenshotsButtonText = "Создание кадров... ⏳";
            int duration = GetDurationInSeconds();
            double start = duration * 0.1;
            double end = duration * 0.9;
            double interval = (end - start) / 9.0;
            var imagesData = new List<byte[]>();

            await Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    double timeSec = start + i * interval;
                    string timeStr = timeSec.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = $"-y -ss {timeStr} -i \"{SelectedVideoFilePath}\" -vframes 1 -f image2pipe -c:v png -",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        using var ms = new MemoryStream();
                        var copyTask = process.StandardOutput.BaseStream.CopyToAsync(ms);
                        if (!process.WaitForExit(15000)) { try { process.Kill(); } catch { } }
                        try { copyTask.Wait(5000); } catch { }
                        if (ms.Length > 0) { imagesData.Add(ms.ToArray()); }
                    }
                }
            });

            List<byte[]>? selectedData = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopSelection && desktopSelection.MainWindow != null)
            {
                var selectionWindow = new Nami.Views.ScreenshotSelectionWindow(imagesData);
                selectedData = await selectionWindow.ShowDialog<List<byte[]>?>(desktopSelection.MainWindow);
            }

            if (selectedData == null || selectedData.Count != 4)
            {
                CreateScreenshotsButtonText = "Отменено ⚠️";
                await Task.Delay(2000);
                CreateScreenshotsButtonText = "Создать скриншоты...";
                return;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string outFolder = Path.Combine(baseDir, "Temp");
            Directory.CreateDirectory(outFolder);

            for (int i = 0; i < 4; i++)
            {
                File.WriteAllBytes(Path.Combine(outFolder, $"selected_0{i + 1}.png"), selectedData[i]);
            }

            // Convert and save cover
            string? coverImgId = null;
            string? coverDirectUrl = null;
            string coverStats = "";
            bool hasCover = !string.IsNullOrWhiteSpace(PosterUrl);

            if (hasCover)
            {
                CreateScreenshotsButtonText = "Подготовка обложки... ⏳";
                var coverResult = await ProcessCoverInternalAsync(ffmpegPath, outFolder);
                coverImgId = coverResult.Id;
                coverDirectUrl = coverResult.Url;
                coverStats = coverResult.Stats;
            }

            bool apiUploaded = false;
            if (!string.IsNullOrWhiteSpace(ImageBanSecretKey))
            {
                var uploadedLinks = new List<string>();
                var uploadedIds = new List<string>();
                bool anyFailed = false;

                for (int i = 0; i < 4; i++)
                {
                    CreateScreenshotsButtonText = $"Загрузка скриншота {i + 1}/4... ⏳";
                    var (directUrl, imgId) = await UploadImageToImageBanAsync(selectedData[i], $"selected_0{i + 1}.png", true);
                    if (!string.IsNullOrEmpty(directUrl))
                    {
                        string thumbUrl = directUrl.Replace("/out/", "/thumbs/");
                        uploadedLinks.Add($"[url={directUrl}][img]{thumbUrl}[/img][/url]");
                        if (!string.IsNullOrEmpty(imgId)) uploadedIds.Add(imgId);
                    }
                    else
                    {
                        anyFailed = true;
                    }
                }

                if (hasCover)
                {
                    if (!string.IsNullOrEmpty(coverImgId))
                    {
                        uploadedIds.Add(coverImgId);
                        PosterUrl = coverDirectUrl;
                    }
                }

                if (uploadedLinks.Count > 0)
                {
                    if (uploadedIds.Count > 0 && !string.IsNullOrWhiteSpace(ImageBanSecretKey))
                    {
                        if (string.IsNullOrEmpty(CurrentAlbumId))
                        {
                            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopAlbum && desktopAlbum.MainWindow != null)
                            {
                                var albumDialog = new Nami.Views.AlbumNameDialogWindow();
                                var albumName = await albumDialog.ShowDialog<string?>(desktopAlbum.MainWindow);
                                if (!string.IsNullOrWhiteSpace(albumName))
                                {
                                    CreateScreenshotsButtonText = "Создание альбома... ⏳";
                                    var id = await CreateImageBanAlbumAsync(albumName, uploadedIds);
                                    if (id != null) CurrentAlbumId = id;
                                }
                            }
                        }
                        else
                        {
                            CreateScreenshotsButtonText = "Добавление в альбом... ⏳";
                            await AddImagesToImageBanAlbumAsync(CurrentAlbumId, uploadedIds);
                        }
                    }

                    Screenshots = string.Join("\n", uploadedLinks);
                    apiUploaded = true;
                    
                    if (anyFailed)
                    {
                        CreateScreenshotsButtonText = "Часть не загружена ⚠️";
                    }
                    else
                    {
                        CreateScreenshotsButtonText = "Успешно! ✓";
                        IsScreenshotsDone = true;
                    }
                }
            }

            if (!apiUploaded)
            {
                try { Process.Start("explorer.exe", $"\"{outFolder}\""); } catch { }

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime && desktopLifetime.MainWindow != null)
                {
                    if (hasCover && string.IsNullOrEmpty(coverDirectUrl))
                    {
                        var coverWindow = new Nami.Views.CoverUploadWindow(coverStats, null);
                        var pastedCoverUrl = await coverWindow.ShowDialog<string?>(desktopLifetime.MainWindow);
                        if (!string.IsNullOrWhiteSpace(pastedCoverUrl)) PosterUrl = pastedCoverUrl;
                    }

                    var uploadWindow = new Nami.Views.ScreenshotUploadWindow();
                    var pastedLinks = await uploadWindow.ShowDialog<string?>(desktopLifetime.MainWindow);
                    
                    if (!string.IsNullOrWhiteSpace(pastedLinks))
                    {
                        Screenshots = pastedLinks;
                        CreateScreenshotsButtonText = "Успешно! ✓";
                        IsScreenshotsDone = true;
                    }
                    else
                    {
                        CreateScreenshotsButtonText = "Создано, но не загружено ⚠️";
                        await Task.Delay(2000);
                        CreateScreenshotsButtonText = "Создать скриншоты...";
                    }
                }
            }

            if (IsScreenshotsDone)
            {
                try
                {
                    if (Directory.Exists(outFolder))
                    {
                        Directory.Delete(outFolder, true);
                    }
                }
                catch { }
            }
        }
        catch (Exception)
        {
            CreateScreenshotsButtonText = "Ошибка при создании! ❌";
            await Task.Delay(2000);
            CreateScreenshotsButtonText = "Создать скриншоты...";
        }
        finally
        {
            _isCreatingScreenshots = false;
        }
    }

    private async Task<(string? Url, string? Id, string Stats)> ProcessCoverInternalAsync(string ffmpegPath, string outFolder)
    {
        try
        {
            string outputPath = Path.Combine(outFolder, "cover.webp");
            byte[] inputBytes;
            using (var request = new HttpRequestMessage(HttpMethod.Get, PosterUrl))
            {
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
                using var response = await SharedHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                inputBytes = await response.Content.ReadAsByteArrayAsync();
            }

            byte[] outputBytes = Array.Empty<byte>();
            string ffmpegStderr = string.Empty;
            int quality = 95;

            string tempInputPath = Path.Combine(Path.GetTempPath(), $"nami_cover_in_{Guid.NewGuid():N}");
            string tempOutputPath = Path.Combine(Path.GetTempPath(), $"nami_cover_out_{Guid.NewGuid():N}.webp");
            try
            {
                await File.WriteAllBytesAsync(tempInputPath, inputBytes);

                while (quality >= 55)
                {
                    await Task.Run(() =>
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = ffmpegPath,
                            Arguments = $"-y -i \"{tempInputPath}\" -vf \"scale=w='min(600,iw)':h='min(600,ih)':force_original_aspect_ratio=decrease\" -c:v libwebp -quality {quality} \"{tempOutputPath}\"",
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(startInfo);
                        if (process != null)
                        {
                            ffmpegStderr = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                        }
                    });

                    if (File.Exists(tempOutputPath))
                    {
                        outputBytes = await File.ReadAllBytesAsync(tempOutputPath);
                        try { File.Delete(tempOutputPath); } catch { }
                    }
                    else
                    {
                        outputBytes = Array.Empty<byte>();
                    }

                    if (outputBytes.Length > 0 && outputBytes.Length <= 150 * 1024) break;
                    if (outputBytes.Length == 0) break;
                    quality -= 10;
                }
            }
            finally
            {
                try { File.Delete(tempInputPath); } catch { }
                try { File.Delete(tempOutputPath); } catch { }
            }

            if (outputBytes.Length > 0)
            {
                var matches = Regex.Matches(ffmpegStderr, @"Stream.*Video:.*?\s(\d+)x(\d+)");
                int inputWidth = 0; int inputHeight = 0;
                if (matches.Count > 0)
                {
                    inputWidth = int.Parse(matches[0].Groups[1].Value);
                    inputHeight = int.Parse(matches[0].Groups[2].Value);
                }
                else
                {
                    var matchesAlt = Regex.Matches(ffmpegStderr, @"Video:.*?, (\d+)x(\d+)");
                    if (matchesAlt.Count > 0)
                    {
                        inputWidth = int.Parse(matchesAlt[0].Groups[1].Value);
                        inputHeight = int.Parse(matchesAlt[0].Groups[2].Value);
                    }
                }

                if (inputWidth > 0 && inputHeight > 0 && (inputWidth < 300 || inputHeight < 300))
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                    {
                        var dialog = new Nami.Views.DialogWindow(
                            "Обложка слишком мала",
                            $"Разрешение исходной обложки слишком мало ({inputWidth}x{inputHeight} px).\n" +
                            "Минимальные требования по правилам — от 300x300 px.\n\n" +
                            "Пожалуйста, найдите и загрузите обложку более высокого качества вручную."
                        );
                        var noBtn = dialog.FindControl<Button>("NoButton");
                        if (noBtn != null) noBtn.IsVisible = false;
                        var yesBtn = dialog.FindControl<Button>("YesButton");
                        if (yesBtn != null) yesBtn.Content = "ОК";
                        await dialog.ShowDialog<bool>(desktop.MainWindow);
                    }
                    return (null, null, "");
                }

                try { File.WriteAllBytes(outputPath, outputBytes); } catch { }

                double sizeKb = outputBytes.Length / 1024.0;
                string resolution = "300+ px";
                if (matches.Count > 1) resolution = $"{matches[1].Groups[1].Value}x{matches[1].Groups[2].Value}";
                else if (matches.Count == 1) resolution = $"{inputWidth}x{inputHeight}";

                string statsText = $"Разрешение: {resolution} px | Размер: {sizeKb:F1} KB";

                if (!string.IsNullOrWhiteSpace(ImageBanSecretKey))
                {
                    var (url, imgId) = await UploadImageToImageBanAsync(outputBytes, "cover.webp");
                    return (url, imgId, statsText);
                }
                return (null, null, statsText);
            }
        }
        catch { }
        return (null, null, "");
    }

    public async Task<(string? Link, string? Id)> UploadImageToImageBanAsync(byte[] fileBytes, string fileName, bool generatePreview = false)
    {
        if (string.IsNullOrWhiteSpace(ImageBanSecretKey))
        {
            return (null, null);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.imageban.ru/v1");
            request.Headers.Add("Authorization", $"Bearer {ImageBanSecretKey}");

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            form.Add(fileContent, "image", fileName);
            if (generatePreview)
            {
                form.Add(new StringContent("1"), "preview_info");
            }
            request.Content = form;

            using var response = await SharedHttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    if (root.TryGetProperty("data", out var dataProp))
                    {
                        if (dataProp.ValueKind == JsonValueKind.Array && dataProp.GetArrayLength() > 0)
                        {
                            var first = dataProp[0];
                            string? link = null;
                            string? id = null;
                            if (first.TryGetProperty("link", out var linkProp)) link = linkProp.GetString();
                            if (first.TryGetProperty("id", out var idProp)) id = idProp.ToString();
                            if (link != null) return (link, id);
                        }
                        else if (dataProp.ValueKind == JsonValueKind.Object)
                        {
                            string? link = null;
                            string? id = null;
                            if (dataProp.TryGetProperty("link", out var linkProp)) link = linkProp.GetString();
                            if (dataProp.TryGetProperty("id", out var idProp)) id = idProp.ToString();
                            if (link != null) return (link, id);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore upload failures
        }
        return (null, null);
    }

    public async Task<string?> CreateImageBanAlbumAsync(string albumName, IEnumerable<string> imageIds)
    {
        if (string.IsNullOrWhiteSpace(ImageBanSecretKey)) return null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.imageban.ru/v1/album");
            request.Headers.Add("Authorization", $"Bearer {ImageBanSecretKey}");
            
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(albumName), "album_name");
            
            var idsStr = string.Join(",", imageIds);
            if (!string.IsNullOrWhiteSpace(idsStr))
            {
                form.Add(new StringContent(idsStr), "images");
            }
            request.Content = form;
            
            using var response = await SharedHttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    if (root.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("id", out var idProp))
                    {
                        return idProp.ToString();
                    }
                }
            }
        }
        catch { }
        return null;
    }

    public async Task<bool> AddImagesToImageBanAlbumAsync(string albumId, IEnumerable<string> imageIds)
    {
        if (string.IsNullOrWhiteSpace(ImageBanSecretKey)) return false;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"https://api.imageban.ru/v1/album/{albumId}");
            request.Headers.Add("Authorization", $"Bearer {ImageBanSecretKey}");
            
            using var form = new MultipartFormDataContent();
            var idsStr = string.Join(",", imageIds);
            if (!string.IsNullOrWhiteSpace(idsStr))
            {
                form.Add(new StringContent(idsStr), "images");
            }
            request.Content = form;
            
            using var response = await SharedHttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    // Persistence settings helpers
    private static readonly string SettingsDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings");
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirPath, "settings.txt");
    private static readonly string PresetsDirPath = Path.Combine(SettingsDirPath, "presets");

    // Shared resources
    private static readonly HttpClient SharedHttpClient = CreateSharedHttpClient();

    private static HttpClient CreateSharedHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
        return client;
    }
    private static readonly Regex IfTagRegex = new(@"\[IF\s+(!?)([a-zA-Z0-9_]+)\]((?:(?!\[IF\s|\[/IF\]).)*?)\[/IF\](\r?\n?)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex QuoteTagRegex = new(@"\[quote(?:=[^\]]+)?\].*?\[/quote\]", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex GeneralTagRegex = new(@"\[/?[a-zA-Z]+(?:=[^\]]+)?\]", RegexOptions.Compiled);

    private static readonly string DefaultTemplate = 
        "[align=center][size=18][b][color=#404040][IF TitleJpKanji]{TitleJpKanji}[/IF][IF TitleJpRomaji][IF TitleJpKanji] | [/IF]{TitleJpRomaji}[/IF]\n" +
        "[IF TitleEn]{TitleEn}[/IF][IF TitleRu][IF TitleEn] | [/IF]{TitleRu}[/IF][/color][/size][/b]\n" +
        "[b][size=25][color=#B00000]{TitleLocalized}[/color][/size][/b]\n" +
        "[color=#B00000]&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;&#9473;[/color][/align]\n" +
        "[poster=right]{PosterUrl}[/poster]\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Тип:[/b] {ReleaseType}\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Жанр:[/b] {Genre}\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Выпуск:[/b] {ReleaseDate}\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Продолжительность:[/b] {Duration}\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Рейтинг:[/b] [color=#B00000][b]{AgeRating}[/b][/color]\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Режиссер:[/b] {Director}\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Производство:[/b] {Studio}\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Автор оригинала:[/b] {Author}\n\n" +
        "[b][color=#B00000]&#9612;[/color][/b][b]Описание:[/b]\n{Description}\n" +
        "[b][color=#B00000]&#9632;[/color][/b] [b]Ссылки:[/b] [IF ShikimoriUrl][url={ShikimoriUrl}]Shikimori[/url] [/IF][IF WorldArtUrl][url={WorldArtUrl}]World-Art[/url] [/IF][IF AnnUrl][url={AnnUrl}]ANN[/url] [/IF][IF MalUrl][url={MalUrl}]MyAnimeList[/url] [/IF][IF AnidbUrl][url={AnidbUrl}]AniDB[/url] [/IF][IF SyoboiUrl][url={SyoboiUrl}]Syoboi[/url] [/IF] \n" +
        "\n[b][color=#B00000]&#9612;[/color][/b][b]Локализация:[/b]\n" +
        "[IF AudioLanguage][b][color=#B00000]&#9632;[/color][/b] [b]Язык озвучки:[/b] {AudioLanguage}[/IF]\n" +
        "[IF Subtitles][b][color=#B00000]&#9632;[/color][/b] [b]Субтитры:[/b] {Subtitles}[/IF]\n" +
        "[IF Translation][b][color=#B00000]&#9632;[/color][/b] [b]Перевод:[/b] {Translation}[/IF]\n" +
        "[IF InternalSubtitles][b][color=#B00000]&#9632;[/color][/b] [b]Субтитры (в контейнере):[/b] {InternalSubtitles}[/IF]\n" +
        "[IF InternalTranslation][b][color=#B00000]&#9632;[/color][/b] [b]Перевод (в контейнере):[/b] {InternalTranslation}[/IF]\n\n" +
        "[b][color=#B00000]&#9612;[/color][/b][b]Техническая информация:[/b]\n" +
        "[color=#B00000]&#9632;[/color] [b]Качество видео:[/b] {VideoQuality}\n" +
        "[color=#B00000]&#9632;[/color] [b]Автор рипа:[/b] {ReleaseGroup}\n" +
        "[color=#B00000]&#9632;[/color] [b]Видео:[/b] {VideoInfo}\n" +
        "[color=#B00000]&#9632;[/color] [b]Аудио:[/b] {AudioInfo}\n\n" +
        "[IF TrailerUrl][brc][align=center][b]Трейлер:[/b]\n[youtube=high]{TrailerUrl}[/youtube][/align]\n[/IF]" +
        "{FormattedScreenshots}" +
        "[IF Episodes][hide=Эпизоды]\n{Episodes}\n[/hide]\n[/IF]" +
        "[spoiler=MediaInfo]\n[pre]{MediaInfo}\n[/pre]\n[/spoiler]";

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var lines = File.ReadAllLines(SettingsFilePath);
                if (lines.Length > 0)
                {
                    Translation = lines[0].Trim();
                }
                if (lines.Length > 1)
                {
                    Subtitles = lines[1].Trim();
                }
                if (lines.Length > 2)
                {
                    // ImageBanClientId (Legacy) - ignored
                }
                if (lines.Length > 3)
                {
                    ImageBanSecretKey = lines[3].Trim();
                }
                if (lines.Length > 4)
                {
                    if (bool.TryParse(lines[4].Trim(), out bool parsedAuthor))
                    {
                        IsAuthor = parsedAuthor;
                    }
                }
                if (lines.Length > 5)
                {
                    string preset = lines[5].Trim();
                    if (!string.IsNullOrEmpty(preset))
                    {
                        SelectedPreset = preset;
                    }
                }
                if (lines.Length > 6)
                {
                    if (bool.TryParse(lines[6].Trim(), out bool parsedTrailer))
                    {
                        IncludeTrailer = parsedTrailer;
                    }
                }
            }
        }
        catch
        {
            // Ignore load errors safely
        }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirPath);
            File.WriteAllLines(SettingsFilePath, new[] { Translation, Subtitles, string.Empty, ImageBanSecretKey, IsAuthor.ToString(), SelectedPreset, IncludeTrailer.ToString() });
        }
        catch
        {
            // Ignore save errors safely
        }
    }

    private void RefreshPresetsList()
    {
        var oldSelected = SelectedPreset;
        Presets.Clear();
        Presets.Add("Default");

        try
        {
            if (Directory.Exists(PresetsDirPath))
            {
                var files = Directory.GetFiles(PresetsDirPath, "*.txt");
                foreach (var f in files)
                {
                    Presets.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
        }
        catch { }

        _suppressTemplateLoad = true;
        if (!string.IsNullOrEmpty(oldSelected) && Presets.Contains(oldSelected))
        {
            SelectedPreset = oldSelected;
            OnPropertyChanged(nameof(SelectedPreset));
        }
        else
        {
            SelectedPreset = "Default";
            OnPropertyChanged(nameof(SelectedPreset));
        }
        _suppressTemplateLoad = false;
    }

    private void LoadTemplate()
    {
        _isLoadingTemplate = true;
        try
        {
            if (string.IsNullOrEmpty(SelectedPreset) || SelectedPreset == "Default")
            {
                TemplateContent = DefaultTemplate;
                return;
            }

            try
            {
                string path = Path.Combine(PresetsDirPath, SelectedPreset + ".txt");
                if (File.Exists(path))
                {
                    TemplateContent = File.ReadAllText(path, Encoding.UTF8);
                    return;
                }
            }
            catch { }
            TemplateContent = DefaultTemplate;
        }
        finally
        {
            _isLoadingTemplate = false;
        }
    }

    private void SaveTemplate()
    {
        if (string.IsNullOrEmpty(SelectedPreset) || SelectedPreset == "Default")
            return; // Cannot save default

        try
        {
            Directory.CreateDirectory(PresetsDirPath);
            string path = Path.Combine(PresetsDirPath, SelectedPreset + ".txt");
            File.WriteAllText(path, TemplateContent, Encoding.UTF8);
        }
        catch { }
    }

    [RelayCommand]
    private void OpenTemplateEditor()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var editor = new Views.TemplateEditorWindow
            {
                DataContext = this
            };
            if (desktop.MainWindow != null)
                editor.Show(desktop.MainWindow);
            else
                editor.Show();
        }
    }

    [RelayCommand]
    private async Task SaveAsPresetAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialog = new Views.PresetNameDialogWindow();
            var activeWindow = desktop.Windows.OfType<Views.TemplateEditorWindow>().FirstOrDefault() ?? desktop.MainWindow;
            var result = await dialog.ShowDialog<string>(activeWindow!);
            if (!string.IsNullOrWhiteSpace(result))
            {
                // Remove invalid characters
                string safeName = string.Join("_", result.Split(Path.GetInvalidFileNameChars()));
                if (safeName.ToLower() == "default") return;

                if (Presets.Contains(safeName) && desktop.MainWindow != null)
                {
                    var confirmDialog = new Views.DialogWindow(
                        "Подтверждение", 
                        $"Пресет '{safeName}' уже существует.\nПерезаписать его?"
                    );
                    var confirmResult = await confirmDialog.ShowDialog<bool>(desktop.MainWindow);
                    if (!confirmResult) return;
                }

                SelectedPreset = safeName;
                
                try
                {
                    Directory.CreateDirectory(PresetsDirPath);
                    string path = Path.Combine(PresetsDirPath, safeName + ".txt");
                    File.WriteAllText(path, TemplateContent, Encoding.UTF8);
                }
                catch { }

                RefreshPresetsList();
            }
        }
    }

    [RelayCommand]
    private void DeletePreset()
    {
        if (string.IsNullOrEmpty(SelectedPreset) || SelectedPreset == "Default")
            return;

        try
        {
            string path = Path.Combine(PresetsDirPath, SelectedPreset + ".txt");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            
            _suppressTemplateLoad = true;
            SelectedPreset = "Default";
            _suppressTemplateLoad = false;
            
            RefreshPresetsList();
            LoadTemplate();
        }
        catch { }
    }

    [RelayCommand]
    private async Task ImportFromShikimoriAsync()
    {
        var (id, domain) = ShikimoriClient.ExtractAnimeInfo(ShikimoriImportUrl);
        if (string.IsNullOrWhiteSpace(id))
        {
            var oldText = ImportButtonText;
            ImportButtonText = "Неверная ссылка! ❌";
            await Task.Delay(2000);
            ImportButtonText = oldText;
            return;
        }

        var originalText = ImportButtonText;
        try
        {
            _isImporting = true;
            ImportButtonText = "Загрузка... ⏳";
            var data = await ShikimoriClient.FetchAnimeDataAsync(id, domain);
            if (data.Anime == null)
            {
                throw new Exception("Anime data was null");
            }

            var anime = data.Anime;

            // Titles
            var tempTitleJpKanji = anime.Japanese.Count > 0 && !string.IsNullOrEmpty(anime.Japanese[0]) ? anime.Japanese[0]! : string.Empty;
            var tempTitleJpRomaji = anime.Name;
            var tempTitleEn = anime.English.Count > 0 && !string.IsNullOrEmpty(anime.English[0]) ? anime.English[0]! : string.Empty;
            var tempTitleRu = string.Empty;
            var tempTitleLocalized = anime.Russian;

            // Poster URL
            var tempPosterUrl = string.Empty;
            if (anime.Image != null && !string.IsNullOrEmpty(anime.Image.Original))
            {
                string standardPoster = anime.Image.Original.StartsWith("http")
                    ? anime.Image.Original
                    : $"https://{domain}" + anime.Image.Original;

                string? highResPoster = await TryGetHighResPosterAsync(id, domain);
                tempPosterUrl = highResPoster ?? standardPoster;
            }

            // Specs
            var tempGenre = string.Join(", ", anime.Genres.ConvertAll(g => string.IsNullOrWhiteSpace(g.Russian) ? g.Name : g.Russian));
            
            var tempReleaseType = anime.Kind.ToLower() switch
            {
                "tv" => "ТВ",
                "movie" => "Фильм",
                "ova" => "OVA",
                "ona" => "ONA",
                "special" => "Спешл",
                "promo" => "Промо",
                _ => "ТВ"
            };

            var tempDuration = string.Empty;
            if (anime.Episodes > 1)
            {
                tempDuration = $"{anime.Episodes} серий по {anime.Duration} мин.";
            }
            else
            {
                tempDuration = anime.Duration > 0 ? $"~ {anime.Duration} мин." : string.Empty;
            }

            // Release Date formatting
            var tempReleaseDate = FormatReleaseDate(anime.AiredOn, anime.ReleasedOn, anime.Kind);

            // Age Rating
            var tempAgeRating = anime.Rating.ToLower() switch
            {
                "g" => "0+",
                "pg" => "6+",
                "pg_13" => "12+",
                "r" => "16+",
                "r_plus" => "18+",
                "rx" => "18+",
                _ => "18+"
            };

            // Description cleaning
            var tempDescription = CleanDescription(anime.Description);

            // Studio
            var tempStudioName = string.Empty;
            var tempStudioUrl = string.Empty;
            if (anime.Studios.Count > 0)
            {
                tempStudioName = anime.Studios[0].Name;
                tempStudioUrl = $"https://{domain}/studios/{anime.Studios[0].Id}";
            }

            // Staff (Director, Author)
            var tempDirectorName = string.Empty;
            var tempDirectorUrl = string.Empty;
            var tempAuthorName = string.Empty;
            var tempAuthorUrl = string.Empty;

            foreach (var r in data.Roles)
            {
                if (r.Person == null) continue;

                bool isDirector = r.Roles.Contains("Director") || r.Roles.Contains("Chief Director");
                bool isAuthor = r.Roles.Contains("Original Creator") || r.Roles.Contains("Original Story") || 
                                r.Roles.Contains("Original Writer") || r.Roles.Contains("Manga") || 
                                r.Roles.Contains("Novel") || r.Roles.Contains("Author") || r.Roles.Contains("Creator");

                if (isDirector && string.IsNullOrEmpty(tempDirectorName))
                {
                    tempDirectorName = string.IsNullOrWhiteSpace(r.Person.Russian) ? r.Person.Name : r.Person.Russian;
                    tempDirectorUrl = r.Person.Url.StartsWith("http") ? r.Person.Url : $"https://{domain}" + r.Person.Url;
                }
                if (isAuthor && string.IsNullOrEmpty(tempAuthorName))
                {
                    tempAuthorName = string.IsNullOrWhiteSpace(r.Person.Russian) ? r.Person.Name : r.Person.Russian;
                    tempAuthorUrl = r.Person.Url.StartsWith("http") ? r.Person.Url : $"https://{domain}" + r.Person.Url;
                }
            }

            // Resource links
            var tempShikimoriUrl = ShikimoriImportUrl.Trim().StartsWith("http") 
                ? ShikimoriImportUrl.Trim() 
                : $"https://{domain}/animes/{id}";
            
            // Set defaults to empty, then populate if found
            var tempWorldArtUrl = string.Empty;
            var tempAnnUrl = string.Empty;
            var tempMalUrl = string.Empty;
            var tempSyoboiUrl = string.Empty;
            var tempAnidbUrl = string.Empty;

            foreach (var link in data.ExternalLinks)
            {
                switch (link.Kind.ToLower())
                {
                    case "world_art":
                        tempWorldArtUrl = link.Url;
                        break;
                    case "anime_news_network":
                        tempAnnUrl = link.Url;
                        break;
                    case "myanimelist":
                        tempMalUrl = link.Url;
                        break;
                    case "syoboi":
                        tempSyoboiUrl = link.Url;
                        break;
                    case "anime_db":
                        tempAnidbUrl = link.Url;
                        break;
                }
            }

            // Fetch genres from MAL if possible (Shikimori genres are often messy)
            if (!string.IsNullOrEmpty(tempMalUrl))
            {
                string? malId = MalClient.ExtractMalId(tempMalUrl);
                if (!string.IsNullOrEmpty(malId))
                {
                    var malGenres = await MalClient.FetchGenresAsync(malId);
                    if (!string.IsNullOrEmpty(malGenres))
                    {
                        tempGenre = malGenres;
                    }
                }
            }

            // YouTube PV Trailer
            var tempTrailerUrl = string.Empty;
            foreach (var v in anime.Videos)
            {
                if (v.Kind.ToLower() == "pv" && (v.Url.Contains("youtube.com") || v.Url.Contains("youtu.be")))
                {
                    tempTrailerUrl = v.Url;
                    break;
                }
            }
            if (string.IsNullOrEmpty(tempTrailerUrl))
            {
                // Fallback to first YouTube video if no PV is tagged explicitly
                foreach (var v in anime.Videos)
                {
                    if (v.Url.Contains("youtube.com") || v.Url.Contains("youtu.be"))
                    {
                        tempTrailerUrl = v.Url;
                        break;
                    }
                }
            }

            // Episodes from Jikan API
            var tempEpisodes = string.Empty;
            long? targetMalId = anime.MyAnimeListId;
            if (!targetMalId.HasValue && !string.IsNullOrEmpty(tempMalUrl))
            {
                string? malIdStr = MalClient.ExtractMalId(tempMalUrl);
                if (long.TryParse(malIdStr, out long parsedMalId))
                {
                    targetMalId = parsedMalId;
                }
            }

            if (targetMalId.HasValue)
            {
                try
                {
                    var jikanEpisodes = await JikanClient.FetchAllEpisodesAsync(targetMalId.Value);
                    if (jikanEpisodes.Count > 0)
                    {
                        var sb = new System.Text.StringBuilder();
                        foreach (var ep in jikanEpisodes)
                        {
                            string title = !string.IsNullOrWhiteSpace(ep.Title) ? ep.Title 
                                : !string.IsNullOrWhiteSpace(ep.TitleRomanji) ? ep.TitleRomanji 
                                : ep.TitleJapanese ?? "Episode " + ep.MalId;
                            
                            sb.AppendLine($"{ep.MalId:D2}. {title}");
                        }
                        tempEpisodes = sb.ToString().TrimEnd();
                    }
                }
                catch
                {
                    // Ignore errors to not break import
                }
            }

            // Apply all variables
            TitleJpKanji = tempTitleJpKanji;
            TitleJpRomaji = tempTitleJpRomaji;
            TitleEn = tempTitleEn;
            TitleRu = tempTitleRu;
            TitleLocalized = tempTitleLocalized;
            PosterUrl = tempPosterUrl;
            Genre = tempGenre;
            ReleaseType = tempReleaseType;
            Duration = tempDuration;
            ReleaseDate = tempReleaseDate;
            AgeRating = tempAgeRating;
            Description = tempDescription;
            StudioName = tempStudioName;
            StudioUrl = tempStudioUrl;
            DirectorName = tempDirectorName;
            DirectorUrl = tempDirectorUrl;
            AuthorName = tempAuthorName;
            AuthorUrl = tempAuthorUrl;
            ShikimoriUrl = tempShikimoriUrl;
            WorldArtUrl = tempWorldArtUrl;
            AnnUrl = tempAnnUrl;
            MalUrl = tempMalUrl;
            SyoboiUrl = tempSyoboiUrl;
            AnidbUrl = tempAnidbUrl;
            TrailerUrl = tempTrailerUrl;
            Screenshots = string.Empty;
            Episodes = tempEpisodes;

            ImportButtonText = "Успешно! ✓";
            IsImportDone = true;
        }
        catch (Exception)
        {
            ImportButtonText = "Ошибка загрузки! ❌";
            await Task.Delay(2000);
            ImportButtonText = originalText;
        }
        finally
        {
            _isImporting = false;
            UpdateOutputs();
        }
    }

    private string FormatReleaseDate(string? airedOn, string? releasedOn, string kind)
    {
        bool hasAired = DateTime.TryParse(airedOn, out var airedDate);
        bool hasReleased = DateTime.TryParse(releasedOn, out var releasedDate);
        
        if (hasAired && hasReleased && airedDate != releasedDate)
        {
            return $"с {airedDate:dd.MM.yyyy} по {releasedDate:dd.MM.yyyy}";
        }
        else if (hasAired)
        {
            string k = kind.ToLower();
            if (k == "movie" || k == "special" || k == "promo")
            {
                return $"{airedDate:dd.MM.yyyy}";
            }
            return $"с {airedDate:dd.MM.yyyy}";
        }
        return string.Empty;
    }

    private string CleanDescription(string? desc)
    {
        if (string.IsNullOrEmpty(desc)) return string.Empty;
        
        // 1. Remove paired Shikimori custom tags but keep their inner text (e.g. [character=123]Text[/character] -> Text)
        string[] tags = { "character", "person", "anime", "manga", "club", "comment", "topic", "user" };
        foreach (var tag in tags)
        {
            desc = Regex.Replace(desc, @"\[" + tag + @"=\d+\](.*?)\[/" + tag + @"\]", "$1", RegexOptions.IgnoreCase);
            desc = Regex.Replace(desc, @"\[" + tag + @"=\d+\]", "", RegexOptions.IgnoreCase);
            desc = Regex.Replace(desc, @"\[/" + tag + @"\]", "", RegexOptions.IgnoreCase);
        }

        // 2. Remove all other bracket tags (e.g., [i], [/i], [span], [color=...], etc.)
        desc = QuoteTagRegex.Replace(desc, "");
        desc = GeneralTagRegex.Replace(desc, "");
        
        return desc;
    }

    private static async Task<string?> TryGetHighResPosterAsync(string animeId, string domain)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}/animes/{animeId}");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
            
            // Note: SharedHttpClient has a 30s timeout, which is close enough to 15s for this fallback.
            using var response = await SharedHttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string html = await response.Content.ReadAsStringAsync();
            
            var match = Regex.Match(html, $@"https://{domain.Replace(".", @"\.")}/uploads/poster/animes/{animeId}/[a-f0-9]+\.(?:jpe?g|png|webp)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Value;
            }
            
            var relMatch = Regex.Match(html, $@"/uploads/poster/animes/{animeId}/[a-f0-9]+\.(?:jpe?g|png|webp)", RegexOptions.IgnoreCase);
            if (relMatch.Success)
            {
                return $"https://{domain}" + relMatch.Value;
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }
}
