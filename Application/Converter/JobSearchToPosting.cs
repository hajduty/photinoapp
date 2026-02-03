using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.Postings;
using System.Text.Json;

public static class JobSearchToPosting
{
    public static GetJobsResponse Convert(string json)
    {
        var result = new List<Posting>();

        using var doc = JsonDocument.Parse(json);
        var hits = doc.RootElement.GetProperty("hits").EnumerateArray();

        var total = doc.RootElement.GetProperty("total").GetProperty("value").GetInt32();

        foreach (var hit in hits)
        {
            var posting = new Posting
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
                LastApplicationDate = DateTime.Parse(hit.GetProperty("application_deadline").GetString())
            };

            result.Add(posting);
        }

        return new GetJobsResponse(total, result);
    }

    private static int ParseId(string id) =>
        int.TryParse(id, out int num) ? num : Math.Abs(id.GetHashCode());

    private static string GetDescription(JsonElement hit) =>
        hit.GetProperty("description").GetProperty("text").GetString() ??
        hit.GetProperty("description").GetProperty("text_formatted").GetString() ?? "";

    private static string GetLocation(JsonElement hit)
    {
        var addr = hit.GetProperty("workplace_address");
        var city = addr.GetProperty("city").GetString();
        var country = addr.GetProperty("country").GetString();

        if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country))
            return $"{city}, {country}";

        return city ?? country ?? "";
    }

    private static string GetUrl(JsonElement hit) =>
        hit.GetProperty("application_details").GetProperty("url").GetString() ?? "";

    private static string GetOriginUrl(JsonElement hit) =>
        hit.GetProperty("webpage_url").GetString() ?? "";
}