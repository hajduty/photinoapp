using JobTracker.Application.Features.JobSearch.LoadJobs.Utils;
using JobTracker.Application.Features.Postings;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobTracker.Application.Features.JobSearch.LoadJobs;

public record LoadJobsRequest(string Keyword);
public record LoadJobsResponse(int JobsLoaded);

// Fetch jobs from JobTech API, LinkedIn & Indeed and store them in the DB
public class LoadJobs
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly HttpClient _httpClient;

    public LoadJobs(IDbContextFactory<AppDbContext> dbFactory)
    {
        _httpClient = new HttpClient();
        _dbFactory = dbFactory;
    }

    public async Task<LoadJobsResponse> ExecuteAsync(LoadJobsRequest req)
    {
        // Fetch jobs from JobTech API
        var jobTechJobs = await FetchFromJobTechAsync(req.Keyword);

        //var linkedInJobs = await FetchFromLinkedInAsync(keyword);

        //var indeedJobs = await FetchFromIndeedAsync(keyword);

        await using var db = await _dbFactory.CreateDbContextAsync();

        db.Postings.AddRange(jobTechJobs);
        //db.Postings.AddRange(linkedInJobs);
        //db.Postings.AddRange(indeedJobs);

        await db.SaveChangesAsync();
        return new LoadJobsResponse(jobTechJobs.Count);
    }

    private async Task<List<Posting>> FetchFromJobTechAsync(string keyword)
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
            result.Add(MapPosting(hit));
        }

        int apiOffset = LIMIT;

        while (result.Count < total)
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
                result.Add(MapPosting(hit));
            }

            apiOffset += LIMIT;
            await Task.Delay(300); // be nice to the api
        }

        return result;
    }

    private Posting MapPosting(JsonElement hit)
    {
        return new Posting
        {
            Id = JobSearchHelper.ParseId(hit.GetProperty("id").GetString()),
            Title = hit.GetProperty("headline").GetString() ?? "",
            Description = JobSearchHelper.GetDescription(hit),
            Company = hit.GetProperty("employer").GetProperty("name").GetString() ?? "",
            Location = JobSearchHelper.GetLocation(hit),
            PostedDate = DateTime.Parse(hit.GetProperty("publication_date").GetString()),
            Url = JobSearchHelper.GetUrl(hit),
            OriginUrl = JobSearchHelper.GetOriginUrl(hit),
            CompanyImage = hit.GetProperty("logo_url").GetString() ?? "",
            CreatedAt = DateTime.UtcNow,
            LastApplicationDate = DateTime.Parse(hit.GetProperty("application_deadline").GetString())
        };
    }
}