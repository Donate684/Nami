using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace Nami.Models;

public class MediaInfoResult
{
    public string VideoInfo { get; set; } = string.Empty;
    public string AudioInfo { get; set; } = string.Empty;
    public string AudioLanguage { get; set; } = string.Empty;
    public string InternalSubtitles { get; set; } = string.Empty;
    public string InternalTranslation { get; set; } = string.Empty;
    public string ReleaseGroup { get; set; } = string.Empty;
    public string VideoQuality { get; set; } = string.Empty;
}

public static class MediaInfoParser
{
    private class AudioTrack
    {
        public string Format { get; set; } = "";
        public string Channels { get; set; } = "";
        public string SampleRate { get; set; } = "";
        public string Bitrate { get; set; } = "";
        public string Title { get; set; } = "";
        public string LanguageRaw { get; set; } = "";
    }

    private class TextTrack
    {
        public string LanguageRaw { get; set; } = "";
        public string Title { get; set; } = "";
    }

    public static MediaInfoResult Parse(string rawText)
    {
        var result = new MediaInfoResult();
        if (string.IsNullOrWhiteSpace(rawText))
            return result;

        var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string currentSection = "";

        string videoFormat = "";
        string videoProfile = "";
        string videoWidth = "";
        string videoHeight = "";
        string videoAspect = "";
        string videoFps = "";
        string videoBitrate = "";
        string videoBitDepth = "";

        var audioTracks = new List<AudioTrack>();
        AudioTrack? currentAudio = null;

        var textTracks = new List<TextTrack>();
        TextTrack? currentText = null;
        
        var translators = new List<string>();

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            // Detect section (handles Text #1, Text #2, etc.)
            var sectionMatch = Regex.Match(trimmed, @"^(General|Video|Audio|Text|Menu)(?:\s+#\d+)?$", RegexOptions.IgnoreCase);
            if (sectionMatch.Success)
            {
                string sectionName = sectionMatch.Groups[1].Value.ToLower();
                if (sectionName == "general") 
                {
                    currentSection = "general";
                }
                else if (sectionName == "video") 
                {
                    if (string.IsNullOrEmpty(videoFormat) && string.IsNullOrEmpty(videoWidth))
                        currentSection = "video";
                    else
                        currentSection = "ignored";
                }
                else if (sectionName == "audio") 
                {
                    currentSection = "audio";
                    currentAudio = new AudioTrack();
                    audioTracks.Add(currentAudio);
                }
                else if (sectionName == "text") 
                {
                    currentSection = "text";
                    currentText = new TextTrack();
                    textTracks.Add(currentText);
                }
                else if (sectionName == "menu") 
                {
                    currentSection = "menu";
                }
                continue;
            }

            // Check if it's a key-value line
            int colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            string key = trimmed.Substring(0, colonIndex).Trim();
            string val = trimmed.Substring(colonIndex + 1).Trim();

            if (currentSection == "general")
            {
                if (key.Equals("Complete name", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string filename = Path.GetFileName(val);
                        // Try parsing release group, e.g. [SubsPlease] or [Erai-raws]
                        var match = Regex.Match(filename, @"^\[([^\]]+)\]");
                        if (match.Success)
                        {
                            result.ReleaseGroup = match.Groups[1].Value;
                        }
                        else
                        {
                            // Try parsing suffix like "-VARYG.mkv"
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
                            int lastHyphen = nameWithoutExt.LastIndexOf('-');
                            if (lastHyphen > 0 && lastHyphen < nameWithoutExt.Length - 1)
                            {
                                string group = nameWithoutExt.Substring(lastHyphen + 1).Trim();
                                if (!group.Equals("mkv", StringComparison.OrdinalIgnoreCase) && 
                                    !group.Equals("mp4", StringComparison.OrdinalIgnoreCase) &&
                                    !group.Equals("avi", StringComparison.OrdinalIgnoreCase))
                                {
                                    result.ReleaseGroup = group;
                                }
                            }
                        }

                        // Guess quality
                        if (filename.Contains("BDRip", StringComparison.OrdinalIgnoreCase) || filename.Contains("BD", StringComparison.OrdinalIgnoreCase) || filename.Contains("BluRay", StringComparison.OrdinalIgnoreCase))
                            result.VideoQuality = "BDRip";
                        else if (filename.Contains("WEB-DL", StringComparison.OrdinalIgnoreCase) || 
                                 filename.Contains("WEBRip", StringComparison.OrdinalIgnoreCase) ||
                                 filename.Contains("WEB", StringComparison.OrdinalIgnoreCase))
                            result.VideoQuality = "WEBRip";
                        else if (filename.Contains("HDTV", StringComparison.OrdinalIgnoreCase))
                            result.VideoQuality = "HDTV";
                        else
                            result.VideoQuality = "WEBRip"; // Default guess
                    }
                    catch
                    {
                        result.VideoQuality = "WEBRip";
                    }
                }
            }
            else if (currentSection == "video")
            {
                if (key.Equals("Format", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(videoFormat))
                    videoFormat = val;
                else if (key.Equals("Format profile", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(videoProfile))
                    videoProfile = val;
                else if (key.Equals("Width", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(videoWidth))
                    videoWidth = ExtractDigits(val);
                else if (key.Equals("Height", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(videoHeight))
                    videoHeight = ExtractDigits(val);
                else if (key.Equals("Display aspect ratio", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(videoAspect))
                    videoAspect = val;
                else if (key.Equals("Frame rate", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(videoFps))
                {
                    var match = Regex.Match(val, @"^[\d\.]+");
                    videoFps = match.Success ? match.Value : val;
                }
                else if (key.Equals("Bit rate", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(videoBitrate))
                    videoBitrate = "~" + val;
                else if (key.Equals("Bit depth", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(videoBitDepth))
                    videoBitDepth = val.Replace("bits", "bit").Trim();
            }
            else if (currentSection == "audio")
            {
                if (currentAudio == null) continue;
                
                if (key.Equals("Format", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(currentAudio.Format))
                    currentAudio.Format = val;
                else if ((key.Equals("Channel(s)", StringComparison.OrdinalIgnoreCase) || key.Equals("Channels", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrEmpty(currentAudio.Channels))
                {
                    var digits = ExtractDigits(val);
                    currentAudio.Channels = !string.IsNullOrEmpty(digits) ? $"{digits} ch" : val;
                }
                else if (key.Equals("Sampling rate", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(currentAudio.SampleRate))
                    currentAudio.SampleRate = val;
                else if (key.Equals("Bit rate", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(currentAudio.Bitrate))
                    currentAudio.Bitrate = "~" + val;
                else if (key.Equals("Title", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(currentAudio.Title))
                    currentAudio.Title = val;
                else if (key.Equals("Language", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(currentAudio.LanguageRaw))
                    currentAudio.LanguageRaw = val;
            }
            else if (currentSection == "text")
            {
                if (currentText == null) continue;
                
                if (key.Equals("Language", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(currentText.LanguageRaw))
                    currentText.LanguageRaw = val;
                else if (key.Equals("Title", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(currentText.Title))
                    currentText.Title = val;
            }
        }

        // Format Video
        var videoParts = new List<string>();
        if (!string.IsNullOrEmpty(videoFormat))
        {
            string displayFormat = videoFormat;
            if (videoFormat.Equals("AVC", StringComparison.OrdinalIgnoreCase))
                displayFormat = "H.264";
            else if (videoFormat.Equals("HEVC", StringComparison.OrdinalIgnoreCase))
                displayFormat = "H.265";

            string profilePart = !string.IsNullOrEmpty(videoProfile) ? $" ({videoProfile})" : "";
            videoParts.Add($"{displayFormat}{profilePart}");
        }
        if (!string.IsNullOrEmpty(videoWidth) && !string.IsNullOrEmpty(videoHeight))
        {
            string aspectPart = !string.IsNullOrEmpty(videoAspect) ? $" ({videoAspect})" : "";
            videoParts.Add($"{videoWidth}x{videoHeight}{aspectPart}");
        }
        if (!string.IsNullOrEmpty(videoFps))
            videoParts.Add($"~{videoFps} fps");
        if (!string.IsNullOrEmpty(videoBitrate))
            videoParts.Add(videoBitrate);
        if (!string.IsNullOrEmpty(videoBitDepth))
            videoParts.Add(videoBitDepth);

        result.VideoInfo = string.Join(", ", videoParts);

        // Format Audio tracks
        var finalAudioStrings = new List<string>();
        var languageStudios = new Dictionary<string, List<string>>();

        for (int i = 0; i < audioTracks.Count; i++)
        {
            var a = audioTracks[i];
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(a.Format)) parts.Add(a.Format);
            if (!string.IsNullOrEmpty(a.Bitrate)) parts.Add(a.Bitrate);
            if (!string.IsNullOrEmpty(a.SampleRate)) parts.Add(a.SampleRate);
            if (!string.IsNullOrEmpty(a.Channels)) parts.Add(a.Channels);
            
            string lang = TranslateAudioLanguage(a.LanguageRaw);
            if (string.IsNullOrEmpty(lang)) lang = "Японский";
            parts.Add(lang);
            
            if (!string.IsNullOrEmpty(a.Title) && 
                !a.Title.Equals("Оригинальная", StringComparison.OrdinalIgnoreCase) && 
                !a.Title.StartsWith("FLAC", StringComparison.OrdinalIgnoreCase) && 
                !a.Title.StartsWith("AC3", StringComparison.OrdinalIgnoreCase) &&
                !a.Title.Equals("[Erai-raws]_AAC_CR", StringComparison.OrdinalIgnoreCase))
            {
                string trackTitle = (lang == "Русский") ? $"Закадровый ({a.Title})" : $"({a.Title})";
                parts.Add(trackTitle);

                // Add to languageStudios
                if (!languageStudios.ContainsKey(lang)) languageStudios[lang] = new List<string>();
                var studios = a.Title.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var s in studios) {
                    string t = s.Trim();
                    if (!languageStudios[lang].Contains(t)) languageStudios[lang].Add(t);
                }
            }
            else
            {
                if (!languageStudios.ContainsKey(lang)) languageStudios[lang] = new List<string>();
            }

            string line = string.Join(", ", parts);
            if (i == 0) 
                finalAudioStrings.Add(line);
            else 
                finalAudioStrings.Add($"\n[color=#B00000]&#9632;[/color] [b]Аудио{i+1}:[/b] {line}");
        }
        result.AudioInfo = string.Join("", finalAudioStrings);

        // Audio voice language formatting
        var langStrings = new List<string>();
        if (languageStudios.ContainsKey("Русский"))
        {
            var studios = languageStudios["Русский"];
            langStrings.Add(studios.Count > 0 ? $"Русский ({string.Join(", ", studios)})" : "Русский");
            languageStudios.Remove("Русский");
        }
        foreach (var kv in languageStudios)
        {
            if (kv.Value.Count > 0)
                langStrings.Add($"{kv.Key} ({string.Join(", ", kv.Value)})");
            else
                langStrings.Add(kv.Key);
        }
        result.AudioLanguage = langStrings.Count > 0 ? string.Join(", ", langStrings) : "Японский";

        // Format Subtitles and translators
        var subLangTypes = new Dictionary<string, List<string>>();
        foreach (var t in textTracks)
        {
            string lang = TranslateLanguage(t.LanguageRaw);
            if (string.IsNullOrEmpty(lang)) lang = "русские"; // Default

            if (!subLangTypes.ContainsKey(lang)) subLangTypes[lang] = new List<string>();

            if (!string.IsNullOrEmpty(t.Title))
            {
                string subType = "";
                if (t.Title.Contains("Полные", StringComparison.OrdinalIgnoreCase)) subType = "Полные";
                else if (t.Title.Contains("Надписи", StringComparison.OrdinalIgnoreCase)) subType = "Надписи";
                else if (t.Title.Contains("Форс", StringComparison.OrdinalIgnoreCase)) subType = "Форсированные";

                if (!string.IsNullOrEmpty(subType) && !subLangTypes[lang].Contains(subType))
                    subLangTypes[lang].Add(subType);

                // Extract translator in parentheses
                var match = Regex.Match(t.Title, @"\(([^)]+)\)");
                if (match.Success)
                {
                    string studio = match.Groups[1].Value.Trim();
                    if (!translators.Contains(studio)) translators.Add(studio);
                }
                else 
                {
                    string translator = DetectTranslator(t.Title);
                    if (!string.IsNullOrEmpty(translator) && !translators.Contains(translator))
                    {
                        translators.Add(translator);
                    }
                }
            }
        }

        var formattedSubtitles = new List<string>();
        foreach (var kv in subLangTypes)
        {
            string l = kv.Key;
            if (kv.Value.Count > 0)
                formattedSubtitles.Add($"{l} ({string.Join(" + ", kv.Value)})");
            else
                formattedSubtitles.Add(l);
        }

        // Capitalize and move 'русские' to first position
        int rusIndex = formattedSubtitles.FindIndex(s => s.StartsWith("русские", StringComparison.OrdinalIgnoreCase));
        if (rusIndex >= 0)
        {
            var rus = formattedSubtitles[rusIndex];
            formattedSubtitles.RemoveAt(rusIndex);
            formattedSubtitles.Insert(0, char.ToUpper(rus[0]) + rus.Substring(1));
        }
        else if (formattedSubtitles.Count > 0)
        {
            var first = formattedSubtitles[0];
            formattedSubtitles[0] = char.ToUpper(first[0]) + first.Substring(1);
        }
        
        result.InternalSubtitles = string.Join(", ", formattedSubtitles);
        result.InternalTranslation = string.Join(", ", translators);

        return result;
    }

    private static string ExtractDigits(string input)
    {
        return Regex.Replace(input, @"[^\d]", "");
    }

    private static string DetectTranslator(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (Regex.IsMatch(text, @"\b(HIDIVE|HIDI)\b", RegexOptions.IgnoreCase)) return "HIDIVE";
        if (Regex.IsMatch(text, @"\b(Bilibili|BILI)\b", RegexOptions.IgnoreCase)) return "Bilibili";
        if (Regex.IsMatch(text, @"\b(Crunchyroll|CRUNCHY|CR|SubsPlease|Erai-raws)\b", RegexOptions.IgnoreCase)) return "Crunchyroll";
        if (Regex.IsMatch(text, @"\b(Netflix|NF)\b", RegexOptions.IgnoreCase)) return "Netflix";
        if (Regex.IsMatch(text, @"\b(Amazon|AMZN)\b", RegexOptions.IgnoreCase)) return "Amazon";
        if (Regex.IsMatch(text, @"\b(Disney|DSNP)\b", RegexOptions.IgnoreCase)) return "Disney+";

        return string.Empty;
    }

    private static string TranslateAudioLanguage(string lang)
    {
        if (string.IsNullOrEmpty(lang)) return string.Empty;
        lang = lang.Trim().ToLowerInvariant();
        if (lang.Contains("russian") || lang.Contains("rus") || lang == "ru") return "Русский";
        if (lang.Contains("english") || lang.Contains("eng") || lang == "en") return "Английский";
        if (lang.Contains("japanese") || lang.Contains("jpn") || lang == "ja" || lang == "jp") return "Японский";
        if (lang.Contains("chinese") || lang == "zh") return "Китайский";
        if (lang.Contains("korean") || lang == "ko") return "Корейский";
        if (lang.Contains("german") || lang == "de") return "Немецкий";
        if (lang.Contains("french") || lang == "fr") return "Французский";
        if (lang.Contains("spanish") || lang == "es") return "Испанский";
        if (lang.Contains("portuguese") || lang == "pt") return "Португальский";
        if (lang.Length > 0) return char.ToUpper(lang[0]) + lang.Substring(1);
        return lang;
    }

    private static string TranslateLanguage(string lang)
    {
        if (string.IsNullOrEmpty(lang)) return string.Empty;
        lang = lang.Trim().ToLowerInvariant();
        if (lang.Contains("brazil") || lang.Contains("(br)")) return "португальские (BR)";
        if (lang.Contains("latin america") || lang.Contains("(la)")) return "испанские (LA)";
        if (lang.Contains("portuguese") || lang == "pt") return "португальские";
        if (lang.Contains("spanish") || lang == "es") return "испанские";
        if (lang.Contains("chinese (traditional)")) return "китайские (традиционные)";
        if (lang.Contains("chinese (simplified)")) return "китайские (упрощенные)";
        if (lang.Contains("chinese") || lang == "zh") return "китайские";
        if (lang.Contains("korean") || lang == "ko") return "корейские";
        if (lang.Contains("malay") || lang == "ms") return "малайские";
        if (lang.Contains("arabic") || lang == "ar") return "арабские";
        if (lang.Contains("russian") || lang.Contains("rus") || lang == "ru") return "русские";
        if (lang.Contains("english") || lang.Contains("eng") || lang == "en") return "английские";
        if (lang.Contains("german") || lang.Contains("ger") || lang == "de") return "немецкие";
        if (lang.Contains("french") || lang.Contains("fre") || lang == "fr") return "французские";
        if (lang.Contains("italian") || lang == "it") return "итальянские";
        if (lang.Contains("indonesian") || lang.Contains("ind") || lang == "id") return "индонезийские";
        if (lang.Contains("thai") || lang == "th") return "тайские";
        if (lang.Contains("vietnamese") || lang == "vi") return "вьетнамские";
        if (lang.Contains("japanese") || lang.Contains("jpn") || lang == "ja" || lang == "jp") return "японские";
        return lang;
    }
}
