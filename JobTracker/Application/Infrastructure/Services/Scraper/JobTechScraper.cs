using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Services.Scraper.Util;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;

public class JobTechScraper
{
    private readonly HttpClient _httpClient;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public JobTechScraper(HttpClient httpClient, IDbContextFactory<AppDbContext> dbFactory)
    {
        _httpClient = httpClient;
        _dbFactory = dbFactory;
    }

    public async Task<List<Posting>> FetchJobsAsync(string keyword)
    {
        const int LIMIT = 100;
        const int MAX_CONSECUTIVE_DUPLICATES = 5; // Stop after finding this many consecutive duplicates
        const int MAX_RESULTS = 1000;

        var result = new List<Posting>();

        await using var db = await _dbFactory.CreateDbContextAsync();

        var baseUrl =
            $"https://jobsearch.api.jobtechdev.se/search?q={Uri.EscapeDataString(keyword)}&limit={LIMIT}&sort=pubdate-desc";

        var response = await _httpClient.GetStringAsync(baseUrl);
        using var doc = JsonDocument.Parse(response);

        var total = doc.RootElement.GetProperty("total").GetProperty("value").GetInt32();
        var hits = doc.RootElement.GetProperty("hits").EnumerateArray();

        int consecutiveDuplicates = 0;

        foreach (var hit in hits)
        {
            var posting = JobSearchHelper.MapPosting(hit);

            var exists = await db.Postings.AnyAsync(p => p.OriginUrl == posting.OriginUrl);
            if (exists)
            {
                consecutiveDuplicates++;
                if (consecutiveDuplicates >= MAX_CONSECUTIVE_DUPLICATES)
                {
                    // Stop scraping - we've hit existing jobs
                    return result;
                }
                continue;
            }

            consecutiveDuplicates = 0;
            result.Add(posting);
        }

        int apiOffset = LIMIT;

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
                var posting = JobSearchHelper.MapPosting(hit);

                // Check if job already exists in database
                var exists = await db.Postings.AnyAsync(p => p.OriginUrl == posting.OriginUrl);
                if (exists)
                {
                    consecutiveDuplicates++;
                    if (consecutiveDuplicates >= MAX_CONSECUTIVE_DUPLICATES)
                    {
                        // Stop scraping - we've hit existing jobs
                        return result;
                    }
                    continue;
                }

                consecutiveDuplicates = 0; // Reset on new job
                result.Add(posting);
            }

            // If we found duplicates in this page, stop scraping
            if (consecutiveDuplicates >= MAX_CONSECUTIVE_DUPLICATES)
            {
                return result;
            }

            apiOffset += LIMIT;
            await Task.Delay(300); // be nice to the api
        }

        return result;
    }
}
