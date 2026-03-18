using JobTracker.Application.Features.JobSearch;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Infrastructure.Services.Scraper.Util;

public static class JobSearchHelper
{
    public static Posting MapPosting(JsonElement hit)
    {
        return new Posting
        {
            Id = ParseId(hit.GetProperty("id").GetString()),
            Title = hit.GetProperty("headline").GetString() ?? "",
            Description = GetDescription(hit),
            Company = hit.GetProperty("employer").GetProperty("name").GetString() ?? "",
            Location = GetLocation(hit),
            PostedDate = DateTime.Parse(hit.GetProperty("publication_date").GetString()),
            Url = GetUrl(hit),
            OriginUrl = GetOriginUrl(hit),
            CompanyImage = hit.GetProperty("logo_url").GetString() ?? "",
            CreatedAt = DateTime.UtcNow,
            LastApplicationDate = DateTime.Parse(hit.GetProperty("application_deadline").GetString()),
            DescriptionFormatted = hit.GetProperty("description").GetProperty("text_formatted").GetString() ?? "",
            YearsOfExperience = ExtractExperienceYears(GetDescription(hit), hit.GetProperty("headline").GetString() ?? "")
        };
    }

    public static int ParseId(string id) =>
    int.TryParse(id, out int num) ? num : Math.Abs(id.GetHashCode());

    public static string GetDescription(JsonElement hit) =>
        hit.GetProperty("description").GetProperty("text").GetString() ??
        hit.GetProperty("description").GetProperty("text_formatted").GetString() ?? "";

    public static string GetLocation(JsonElement hit)
    {
        var addr = hit.GetProperty("workplace_address");
        var city = addr.GetProperty("city").GetString();
        var country = addr.GetProperty("country").GetString();

        if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country))
            return $"{city}, {country}";

        return city ?? country ?? "";
    }

    public static string GetUrl(JsonElement hit) =>
        hit.GetProperty("application_details").GetProperty("url").GetString() ?? "";

    public static string GetOriginUrl(JsonElement hit) =>
        hit.GetProperty("webpage_url").GetString() ?? "";

    private static readonly Dictionary<string, int> WordNumbers = new()
    {
        // Swedish
        { "ett", 1 }, { "två", 2 }, { "tre", 3 }, { "fyra", 4 }, { "fem", 5 },
        { "sex", 6 }, { "sju", 7 }, { "åtta", 8 }, { "nio", 9 }, { "tio", 10 },
        // English
        { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 }, { "five", 5 },
        { "six", 6 }, { "seven", 7 }, { "eight", 8 }, { "nine", 9 }, { "ten", 10 }
    };

    private static readonly Dictionary<string, int> TitleExperienceLevels = new()
    {
        { "principal", 8 },
        { "staff", 7 },
        { "lead", 6 },
        { "senior", 5 },
        { "mid", 3 },
        { "junior", 1 },
        { "intern", 0 },
        { "trainee", 0 },
    };

    public static int? ExtractExperienceYears(string text, string title)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var normalized = text.ToLowerInvariant()
                             .Replace("–", "-").Replace("−", "-").Replace("―", "-")
                             .Replace("års", " years").Replace("år", " years").Replace("årig", " years");

        foreach (var (word, num) in WordNumbers)
        {
            normalized = Regex.Replace(
                normalized,
                $@"\b{word}\b(?=\s*years?\b)",
                num.ToString(),
                RegexOptions.IgnoreCase
            );
        }

        normalized = Regex.Replace(normalized, @"\bminst\b", "", RegexOptions.IgnoreCase).Trim();

        var sentences = Regex.Split(normalized, @"(?<=[.!?])\s+|[\n\r;]+")
                             .Where(s => s.Trim().Length > 10)
                             .Select(s => s.Trim())
                             .ToList();

        int? highest = null;

        foreach (var sentence in sentences)
        {
            bool isCompanyIntro = sentence.StartsWith("vi ") || sentence.StartsWith("we ");
            bool hasCompanyReference = sentence.Contains(" vi ") || sentence.Contains(" we ") ||
                                       sentence.Contains(" vår ") || sentence.Contains(" our ");
            bool isServiceDescription = sentence.Contains("stöttar") || sentence.Contains("hjälper") ||
                                        sentence.Contains("erbjuder") || sentence.Contains("offers") ||
                                        sentence.Contains("levererar") || sentence.Contains("delivers");
            bool hasExperienceKeyword = sentence.Contains("erfarenhet") || sentence.Contains("experience");

            if ((isCompanyIntro || (hasCompanyReference && isServiceDescription)) && !hasExperienceKeyword)
                continue;

            var overPattern = @"(?:över|mer än|over|>)\s*(\d{1,2})\s*years?\b";
            foreach (Match m in Regex.Matches(sentence, overPattern, RegexOptions.IgnoreCase))
            {
                if (int.TryParse(m.Groups[1].Value, out int num) && num >= 1 && num <= 10)
                    highest = Math.Max(highest ?? 0, num);
            }

            var pattern = @"(\d{1,2})\s*[-+–−]?[\s.,;:/]*\s*(?:years?|yrs?)\b";
            foreach (Match m in Regex.Matches(sentence, pattern, RegexOptions.IgnoreCase))
            {
                if (int.TryParse(m.Groups[1].Value, out int num) && num >= 1 && num <= 10)
                    highest = Math.Max(highest ?? 0, num);
            }

            var rangePattern = @"(\d{1,2})\s*[-–−]\s*(\d{1,2})\s*(?:years?|yrs?)?\b";
            foreach (Match m in Regex.Matches(sentence, rangePattern, RegexOptions.IgnoreCase))
            {
                if (int.TryParse(m.Groups[1].Value, out int low) &&
                    int.TryParse(m.Groups[2].Value, out int high) &&
                    high > low && high <= 10)
                    highest = Math.Max(highest ?? 0, high);
            }
        }

        // Title fallback — only if description yielded nothing
        if (highest == null && !string.IsNullOrWhiteSpace(title))
        {
            var normalizedTitle = title.ToLowerInvariant();
            foreach (var (keyword, years) in TitleExperienceLevels)
            {
                if (Regex.IsMatch(normalizedTitle, $@"\b{keyword}\b"))
                {
                    highest = years;
                    break;
                }
            }
        }

        return highest;
    }
}
