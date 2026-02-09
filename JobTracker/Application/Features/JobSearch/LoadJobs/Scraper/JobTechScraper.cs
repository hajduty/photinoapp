using JobTracker.Application.Features.JobSearch.LoadJobs.Utils;
using JobTracker.Application.Features.Postings;
using System.Text.Json;

namespace JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;

public class JobTechScraper
{
    private readonly HttpClient _httpClient;

    public JobTechScraper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Posting>> FetchJobsAsync(string keyword)
    {
        const int LIMIT = 100;

        var result = new List<Posting>();

        var baseUrl =
            $"https://jobsearch.api.jobtechdev.se/search?q={Uri.EscapeDataString(keyword)}&limit={LIMIT}&sort=pubdate-desc";

        var response = await _httpClient.GetStringAsync(baseUrl);
        using var doc = JsonDocument.Parse(response);

        var total = doc.RootElement.GetProperty("total").GetProperty("value").GetInt32();
        var hits = doc.RootElement.GetProperty("hits").EnumerateArray();

        foreach (var hit in hits)
        {
            result.Add(JobSearchHelper.MapPosting(hit));
        }

        int apiOffset = LIMIT;
        const int MAX_RESULTS = 1000;

        while (result.Count < total && result.Count < MAX_RESULTS)
        {
            var pagedUrl =
                $"{baseUrl}&offset={apiOffset}";

            var pagedResponse = await _httpClient.GetStringAsync(pagedUrl);
            using var pagedDoc = JsonDocument.Parse(pagedResponse);
            var pagedHits = pagedDoc.RootElement.GetProperty("hits").EnumerateArray();

            if (!pagedHits.Any())
                break;

            foreach (var hit in pagedHits)
            {
                result.Add(JobSearchHelper.MapPosting(hit));
            }

            apiOffset += LIMIT;
            await Task.Delay(300); // be nice to the api
        }

        return result;
    }
}
