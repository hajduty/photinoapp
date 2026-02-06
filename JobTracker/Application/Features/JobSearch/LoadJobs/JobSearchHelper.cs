using System.Text.Json;

namespace JobTracker.Application.Features.JobSearch.LoadJobs;

public static class JobSearchHelper
{
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
}
