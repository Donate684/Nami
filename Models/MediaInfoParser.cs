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

    public static MediaInfoResult Parse(string rawText, string? optionalFilePath = null)
    {
        var result = new MediaInfoResult();
        if (string.IsNullOrWhiteSpace(rawText))
            return result;

        string filename = "";
        if (!string.IsNullOrWhiteSpace(optionalFilePath))
        {
            try { filename = Path.GetFileName(optionalFilePath); } catch {}
        }
        string movieTitle = "";

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
                if (key.Equals("Complete name", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("File name", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("FileName", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string parsedName = Path.GetFileName(val);
                        if (!string.IsNullOrEmpty(parsedName))
                            filename = parsedName;
                    }
                    catch {}
                }
                else if (key.Equals("Movie name", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("Title", StringComparison.OrdinalIgnoreCase))
                {
                    movieTitle = val;
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

        // Parse ReleaseGroup and VideoQuality from filename
        if (!string.IsNullOrEmpty(filename))
        {
            try
            {
                if (string.IsNullOrEmpty(result.ReleaseGroup))
                {
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
                }

                if (string.IsNullOrEmpty(result.VideoQuality))
                {
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
            }
            catch {}
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
                    if (!IsNonTranslatorSubtitleType(studio))
                    {
                        string canonical = DetectTranslator(studio);
                        string finalStudio = !string.IsNullOrEmpty(canonical) ? canonical : studio;
                        if (!translators.Contains(finalStudio)) translators.Add(finalStudio);
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

        // Fallback translator detection from filename and title
        if (translators.Count == 0 && !string.IsNullOrEmpty(filename))
        {
            string fileTranslator = DetectTranslator(filename);
            if (!string.IsNullOrEmpty(fileTranslator) && !translators.Contains(fileTranslator))
            {
                translators.Add(fileTranslator);
            }
        }

        if (translators.Count == 0 && !string.IsNullOrEmpty(movieTitle))
        {
            string titleTranslator = DetectTranslator(movieTitle);
            if (!string.IsNullOrEmpty(titleTranslator) && !translators.Contains(titleTranslator))
            {
                translators.Add(titleTranslator);
            }
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
        if (Regex.IsMatch(text, @"\b(Amazon|AMZN|PrimeVideo)\b|Prime\s*Video", RegexOptions.IgnoreCase)) return "Amazon";
        if (Regex.IsMatch(text, @"(?:\b(DisneyPlus|Disney|DSNP)\b|Disney\+)", RegexOptions.IgnoreCase)) return "Disney+";
        if (Regex.IsMatch(text, @"(?:\b(AppleTV|ATVP)\b|Apple\s*TV\+?)", RegexOptions.IgnoreCase)) return "Apple TV";
        if (Regex.IsMatch(text, @"\b(Wakanim|WKN)\b", RegexOptions.IgnoreCase)) return "Wakanim";

        return string.Empty;
    }

    private static bool IsNonTranslatorSubtitleType(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        string t = text.Trim().ToLowerInvariant();
        return t == "полные" || t == "полный" || t == "надписи" || t == "форсированные" || t == "форсированный" || t == "форс" ||
               t == "full" || t == "forced" || t == "signs" || t == "signs & songs" || t == "songs & signs" ||
               t == "signs and songs" || t == "songs and signs" || t == "songs" || t == "titles" ||
               t == "sdh" || t == "cc" || t == "default" || t == "dialogue" || t == "dialog" ||
               t == "subs" || t == "subtitles" || t == "субтитры" ||
               t == "srt" || t == "ass" || t == "ssa" || t == "pgs" || t == "vobsub";
    }

    private static bool TryTranslateLanguage(string langRaw, out string subtitleLang, out string audioLang)
    {
        subtitleLang = string.Empty;
        audioLang = string.Empty;
        if (string.IsNullOrWhiteSpace(langRaw)) return false;

        string l = langRaw.Trim().ToLowerInvariant();

        // 1. Regional / Compound variants
        if (l.Contains("brazil") || l.Contains("(br)") || l == "pt-br")
        {
            subtitleLang = "португальские (BR)";
            audioLang = "Португальский (BR)";
            return true;
        }
        if (l.Contains("latin america") || l.Contains("(la)") || l == "es-419" || l == "es-la")
        {
            subtitleLang = "испанские (LA)";
            audioLang = "Испанский (LA)";
            return true;
        }
        if (l.Contains("chinese (traditional)") || l.Contains("традицион") || l == "zh-hant" || l == "zh-tw" || l == "zh-hk")
        {
            subtitleLang = "китайские (традиционные)";
            audioLang = "Китайский (традиционный)";
            return true;
        }
        if (l.Contains("chinese (simplified)") || l.Contains("упрощен") || l == "zh-hans" || l == "zh-cn" || l == "zh-sg")
        {
            subtitleLang = "китайские (упрощенные)";
            audioLang = "Китайский (упрощенный)";
            return true;
        }
        if (l.Contains("nynorsk") || l.Contains("нюнорск") || l == "nn" || l == "nno")
        {
            subtitleLang = "норвежские (нюнорск)";
            audioLang = "Норвежский (нюнорск)";
            return true;
        }

        // 2. Base languages with prefixes or collision risks
        if (l.Contains("belarus") || l.Contains("белорус") || l == "be" || l == "bel")
        {
            subtitleLang = "белорусские";
            audioLang = "Белорусский";
            return true;
        }
        if (l.Contains("russian") || l.Contains("русск") || l == "ru" || l == "rus")
        {
            subtitleLang = "русские";
            audioLang = "Русский";
            return true;
        }
        if (l.Contains("bengali") || l.Contains("bangla") || l.Contains("бенгальск") || l == "bn" || l == "ben")
        {
            subtitleLang = "бенгальские";
            audioLang = "Бенгальский";
            return true;
        }
        if (l.Contains("english") || l.Contains("английск") || l == "en" || l == "eng")
        {
            subtitleLang = "английские";
            audioLang = "Английский";
            return true;
        }
        if (l.Contains("hindi") || l.Contains("хинди") || l.Contains("индийск") || l == "hi" || l == "hin")
        {
            subtitleLang = "хинди";
            audioLang = "Хинди";
            return true;
        }
        if (l.Contains("indonesian") || l.Contains("индонезийск") || l == "id" || l == "ind")
        {
            subtitleLang = "индонезийские";
            audioLang = "Индонезийский";
            return true;
        }
        if (l.Contains("norwegian") || l.Contains("норвежск") || l.Contains("bokmal") || l.Contains("bokmål") || l.Contains("букмол") || l == "no" || l == "nob" || l == "nor" || l == "nb")
        {
            subtitleLang = "норвежские";
            audioLang = "Норвежский";
            return true;
        }
        if (l.Contains("portuguese") || l.Contains("португальск") || l == "pt" || l == "por" || l == "pt-pt")
        {
            subtitleLang = "португальские";
            audioLang = "Португальский";
            return true;
        }
        if (l.Contains("spanish") || l.Contains("испанск") || l == "es" || l == "spa" || l == "es-es")
        {
            subtitleLang = "испанские";
            audioLang = "Испанский";
            return true;
        }
        if (l.Contains("chinese") || l.Contains("китайск") || l == "zh" || l == "chi" || l == "zho")
        {
            subtitleLang = "китайские";
            audioLang = "Китайский";
            return true;
        }
        if (l.Contains("japanese") || l.Contains("японск") || l == "ja" || l == "jp" || l == "jpn")
        {
            subtitleLang = "японские";
            audioLang = "Японский";
            return true;
        }
        if (l.Contains("korean") || l.Contains("корейск") || l == "ko" || l == "kor")
        {
            subtitleLang = "корейские";
            audioLang = "Корейский";
            return true;
        }
        if (l.Contains("german") || l.Contains("немецк") || l == "de" || l == "ger" || l == "deu")
        {
            subtitleLang = "немецкие";
            audioLang = "Немецкий";
            return true;
        }
        if (l.Contains("french") || l.Contains("французск") || l == "fr" || l == "fre" || l == "fra")
        {
            subtitleLang = "французские";
            audioLang = "Французский";
            return true;
        }
        if (l.Contains("italian") || l.Contains("итальянск") || l == "it" || l == "ita")
        {
            subtitleLang = "итальянские";
            audioLang = "Итальянский";
            return true;
        }
        if (l.Contains("arabic") || l.Contains("арабск") || l == "ar" || l == "ara")
        {
            subtitleLang = "арабские";
            audioLang = "Арабский";
            return true;
        }
        if (l.Contains("czech") || l.Contains("чешск") || l == "cs" || l == "cze" || l == "ces")
        {
            subtitleLang = "чешские";
            audioLang = "Чешский";
            return true;
        }
        if (l.Contains("danish") || l.Contains("датск") || l == "da" || l == "dan")
        {
            subtitleLang = "датские";
            audioLang = "Датский";
            return true;
        }
        if (l.Contains("dutch") || l.Contains("flemish") || l.Contains("нидерландск") || l.Contains("голландск") || l == "nl" || l == "nld" || l == "dut" || l == "fle")
        {
            subtitleLang = "нидерландские";
            audioLang = "Нидерландский";
            return true;
        }
        if (l.Contains("finnish") || l.Contains("финск") || l == "fi" || l == "fin")
        {
            subtitleLang = "финские";
            audioLang = "Финский";
            return true;
        }
        if (l.Contains("greek") || l.Contains("греческ") || l == "el" || l == "ell" || l == "gre")
        {
            subtitleLang = "греческие";
            audioLang = "Греческий";
            return true;
        }
        if (l.Contains("hebrew") || l.Contains("иврит") || l.Contains("еврейск") || l == "he" || l == "heb" || l == "iw")
        {
            subtitleLang = "иврит";
            audioLang = "Иврит";
            return true;
        }
        if (l.Contains("hungarian") || l.Contains("венгерск") || l == "hu" || l == "hun")
        {
            subtitleLang = "венгерские";
            audioLang = "Венгерский";
            return true;
        }
        if (l.Contains("malay") || l.Contains("малайск") || l == "ms" || l == "may" || l == "msa")
        {
            subtitleLang = "малайские";
            audioLang = "Малайский";
            return true;
        }
        if (l.Contains("polish") || l.Contains("польск") || l == "pl" || l == "pol")
        {
            subtitleLang = "польские";
            audioLang = "Польский";
            return true;
        }
        if (l.Contains("romanian") || l.Contains("румынск") || l == "ro" || l == "ron" || l == "rum")
        {
            subtitleLang = "румынские";
            audioLang = "Румынский";
            return true;
        }
        if (l.Contains("swedish") || l.Contains("шведск") || l == "sv" || l == "swe")
        {
            subtitleLang = "шведские";
            audioLang = "Шведский";
            return true;
        }
        if (l.Contains("thai") || l.Contains("тайск") || l == "th" || l == "tha")
        {
            subtitleLang = "тайские";
            audioLang = "Тайский";
            return true;
        }
        if (l.Contains("turkish") || l.Contains("турецк") || l == "tr" || l == "tur")
        {
            subtitleLang = "турецкие";
            audioLang = "Турецкий";
            return true;
        }
        if (l.Contains("ukrainian") || l.Contains("украинск") || l == "uk" || l == "ukr")
        {
            subtitleLang = "украинские";
            audioLang = "Украинский";
            return true;
        }
        if (l.Contains("vietnamese") || l.Contains("вьетнамск") || l == "vi" || l == "vie")
        {
            subtitleLang = "вьетнамские";
            audioLang = "Вьетнамский";
            return true;
        }
        if (l.Contains("bulgarian") || l.Contains("болгарск") || l == "bg" || l == "bul")
        {
            subtitleLang = "болгарские";
            audioLang = "Болгарский";
            return true;
        }
        if (l.Contains("croatian") || l.Contains("хорватск") || l == "hr" || l == "hrv" || l == "scr")
        {
            subtitleLang = "хорватские";
            audioLang = "Хорватский";
            return true;
        }
        if (l.Contains("serbian") || l.Contains("сербск") || l == "sr" || l == "srp" || l == "scc")
        {
            subtitleLang = "сербские";
            audioLang = "Сербский";
            return true;
        }
        if (l.Contains("slovak") || l.Contains("словацк") || l == "sk" || l == "slk" || l == "slo")
        {
            subtitleLang = "словацкие";
            audioLang = "Словацкий";
            return true;
        }
        if (l.Contains("slovenian") || l.Contains("словенск") || l == "sl" || l == "slv")
        {
            subtitleLang = "словенские";
            audioLang = "Словенский";
            return true;
        }
        if (l.Contains("estonian") || l.Contains("эстонск") || l == "et" || l == "est")
        {
            subtitleLang = "эстонские";
            audioLang = "Эстонский";
            return true;
        }
        if (l.Contains("latvian") || l.Contains("латышск") || l == "lv" || l == "lav")
        {
            subtitleLang = "латышские";
            audioLang = "Латышский";
            return true;
        }
        if (l.Contains("lithuanian") || l.Contains("литовск") || l == "lt" || l == "lit")
        {
            subtitleLang = "литовские";
            audioLang = "Литовский";
            return true;
        }
        if (l.Contains("icelandic") || l.Contains("исландск") || l == "is" || l == "isl" || l == "ice")
        {
            subtitleLang = "исландские";
            audioLang = "Исландский";
            return true;
        }
        if (l.Contains("filipino") || l.Contains("tagalog") || l.Contains("филиппинск") || l.Contains("тагальск") || l == "tl" || l == "fil" || l == "tgl")
        {
            subtitleLang = "филиппинские";
            audioLang = "Филиппинский";
            return true;
        }
        if (l.Contains("persian") || l.Contains("farsi") || l.Contains("персидск") || l.Contains("фарси") || l == "fa" || l == "fas" || l == "per")
        {
            subtitleLang = "персидские";
            audioLang = "Персидский";
            return true;
        }
        if (l.Contains("georgian") || l.Contains("грузинск") || l == "ka" || l == "kat" || l == "geo")
        {
            subtitleLang = "грузинские";
            audioLang = "Грузинский";
            return true;
        }
        if (l.Contains("armenian") || l.Contains("армянск") || l == "hy" || l == "hye" || l == "arm")
        {
            subtitleLang = "армянские";
            audioLang = "Армянский";
            return true;
        }
        if (l.Contains("kazakh") || l.Contains("казахск") || l == "kk" || l == "kaz")
        {
            subtitleLang = "казахские";
            audioLang = "Казахский";
            return true;
        }
        if (l.Contains("catalan") || l.Contains("каталанск") || l == "ca" || l == "cat")
        {
            subtitleLang = "каталанские";
            audioLang = "Каталанский";
            return true;
        }
        if (l.Contains("basque") || l.Contains("баскск") || l == "eu" || l == "eus" || l == "baq")
        {
            subtitleLang = "баскские";
            audioLang = "Баскский";
            return true;
        }
        if (l.Contains("galician") || l.Contains("галисийск") || l == "gl" || l == "glg")
        {
            subtitleLang = "галисийские";
            audioLang = "Галисийский";
            return true;
        }
        if (l.Contains("albanian") || l.Contains("албанск") || l == "sq" || l == "sqi" || l == "alb")
        {
            subtitleLang = "албанские";
            audioLang = "Албанский";
            return true;
        }
        if (l.Contains("macedonian") || l.Contains("македонск") || l == "mk" || l == "mkd" || l == "mac")
        {
            subtitleLang = "македонские";
            audioLang = "Македонский";
            return true;
        }
        if (l.Contains("bosnian") || l.Contains("боснийск") || l == "bs" || l == "bos")
        {
            subtitleLang = "боснийские";
            audioLang = "Боснийский";
            return true;
        }
        if (l.Contains("tamil") || l.Contains("тамильск") || l == "ta" || l == "tam")
        {
            subtitleLang = "тамильские";
            audioLang = "Тамильский";
            return true;
        }
        if (l.Contains("telugu") || l.Contains("телугу") || l == "te" || l == "tel")
        {
            subtitleLang = "телугу";
            audioLang = "Телугу";
            return true;
        }
        if (l.Contains("urdu") || l.Contains("урду") || l == "ur" || l == "urd")
        {
            subtitleLang = "урду";
            audioLang = "Урду";
            return true;
        }
        if (l.Contains("marathi") || l.Contains("маратхи") || l == "mr" || l == "mar")
        {
            subtitleLang = "маратхи";
            audioLang = "Маратхи";
            return true;
        }
        if (l.Contains("azerbaijani") || l.Contains("азербайджанск") || l == "az" || l == "aze")
        {
            subtitleLang = "азербайджанские";
            audioLang = "Азербайджанский";
            return true;
        }
        if (l.Contains("uzbek") || l.Contains("узбекск") || l == "uz" || l == "uzb")
        {
            subtitleLang = "узбекские";
            audioLang = "Узбекский";
            return true;
        }
        if (l.Contains("mongolian") || l.Contains("монгольск") || l == "mn" || l == "mon")
        {
            subtitleLang = "монгольские";
            audioLang = "Монгольский";
            return true;
        }
        if (l.Contains("irish") || l.Contains("ирландск") || l == "ga" || l == "gle")
        {
            subtitleLang = "ирландские";
            audioLang = "Ирландский";
            return true;
        }
        if (l.Contains("welsh") || l.Contains("валлийск") || l == "cy" || l == "cym" || l == "wel")
        {
            subtitleLang = "валлийские";
            audioLang = "Валлийский";
            return true;
        }
        if (l.Contains("latin") || l.Contains("латынь") || l.Contains("латинск") || l == "la" || l == "lat")
        {
            subtitleLang = "латинские";
            audioLang = "Латынь";
            return true;
        }
        if (l.Contains("esperanto") || l.Contains("эсперанто") || l == "eo" || l == "epo")
        {
            subtitleLang = "эсперанто";
            audioLang = "Эсперанто";
            return true;
        }
        if (l.Contains("swahili") || l.Contains("суахили") || l == "sw" || l == "swa")
        {
            subtitleLang = "суахили";
            audioLang = "Суахили";
            return true;
        }

        return false;
    }

    private static string TranslateAudioLanguage(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return string.Empty;
        if (TryTranslateLanguage(lang, out _, out string audioLang))
            return audioLang;

        string trimmed = lang.Trim();
        if (trimmed.Length > 0) return char.ToUpper(trimmed[0]) + trimmed.Substring(1);
        return trimmed;
    }

    private static string TranslateLanguage(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return string.Empty;
        if (TryTranslateLanguage(lang, out string subLang, out _))
            return subLang;

        return lang.Trim().ToLowerInvariant();
    }
}
