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
            YearsOfExperience = ExtractExperienceYears(GetDescription(hit))
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

    public static int? ExtractExperienceYears(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var normalized = text.ToLowerInvariant()
                             .Replace("–", "-").Replace("−", "-").Replace("―", "-")
                             .Replace("år", " years").Replace("års", " years").Replace("årig", " years");

        // Split into approximate sentences
        var sentences = Regex.Split(normalized, @"(?<=[.!?])\s+")
                             .Where(s => s.Trim().Length > 10)
                             .Select(s => s.Trim())
                             .ToList();

        int? highest = null;

        foreach (var sentence in sentences)
        {
            // Company voice filter - more nuanced
            bool isCompanyIntro = sentence.StartsWith("vi ") || sentence.StartsWith("we ");
            bool hasCompanyReference = sentence.Contains(" vi ") || sentence.Contains(" we ") ||
                                       sentence.Contains(" vår ") || sentence.Contains(" our ");

            bool isServiceDescription = sentence.Contains("stöttar") || sentence.Contains("hjälper") ||
                                       sentence.Contains("erbjuder") || sentence.Contains("offers") ||
                                       sentence.Contains("levererar") || sentence.Contains("delivers");

            bool hasExperienceKeyword = sentence.Contains("erfarenhet") || sentence.Contains("experience");

            // ONLY skip if it's clearly a company intro WITHOUT experience context
            if ((isCompanyIntro || (hasCompanyReference && isServiceDescription)) && !hasExperienceKeyword)
            {
                continue;
            }

            // Handle "över X års" pattern (numbers over 10 should be IGNORED)
            var overPattern = @"(?:över|mer än|över|over|>)\s*(\d{1,2})\s*years?\b";
            var overMatches = Regex.Matches(sentence, overPattern, RegexOptions.IgnoreCase);

            foreach (Match m in overMatches)
            {
                if (int.TryParse(m.Groups[1].Value, out int num) && num >= 1 && num <= 10) // Only count if <= 10
                {
                    highest = Math.Max(highest ?? 0, num);
                }
            }

            // Core pattern: number + years
            var pattern = @"(\d{1,2})\s*[-+–−]?[\s.,;:/]*\s*(?:years?|yrs?)\b";
            var matches = Regex.Matches(sentence, pattern, RegexOptions.IgnoreCase);

            foreach (Match m in matches)
            {
                if (int.TryParse(m.Groups[1].Value, out int num) && num >= 1 && num <= 10) // Only count if <= 10
                {
                    highest = Math.Max(highest ?? 0, num);
                }
            }

            // Range patterns
            var rangePattern = @"(\d{1,2})\s*[-–−]\s*(\d{1,2})\s*(?:years?|yrs?)?\b";
            var rangeMatches = Regex.Matches(sentence, rangePattern, RegexOptions.IgnoreCase);

            foreach (Match m in rangeMatches)
            {
                if (int.TryParse(m.Groups[1].Value, out int low) &&
                    int.TryParse(m.Groups[2].Value, out int high) &&
                    high > low && high <= 10) // Only count if <= 10
                {
                    highest = Math.Max(highest ?? 0, high);
                }
            }
        }

        return highest;
    }
}
