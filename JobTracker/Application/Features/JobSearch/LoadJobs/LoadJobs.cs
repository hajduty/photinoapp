using JobTracker.Application.Features.Postings;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobTracker.Application.Features.JobSearch.LoadJobs;

// Fetch jobs from JobTech API, LinkedIn & Indeed and store them in the database.
public class LoadJobs
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly HttpClient _httpClient;

    public LoadJobs(IDbContextFactory<AppDbContext> dbFactory)
    {
        _httpClient = new HttpClient();
        _dbFactory = dbFactory;
    }

    public async Task ExecuteAsync(string keyword)
    {
            // Fetch jobs from JobTech API
            var jobTechJobs = await FetchFromJobTechAsync(keyword);
    
            //var linkedInJobs = await FetchFromLinkedInAsync(keyword);
    
            //var indeedJobs = await FetchFromIndeedAsync(keyword);
    
            await using var db = await _dbFactory.CreateDbContextAsync();
        
            db.Postings.AddRange(jobTechJobs);
            //db.Postings.AddRange(linkedInJobs);
            //db.Postings.AddRange(indeedJobs);
    
            await db.SaveChangesAsync();
    }

    private async Task<List<Posting>> FetchFromJobTechAsync(string keyword)
    {
        const int LIMIT = 100;

        var url = "https://jobsearch.api.jobtechdev.se/search?q=" + Uri.EscapeDataString(keyword) + "&limit=100&sort=pubdate-desc";
        var response = await _httpClient.GetStringAsync(url);

        using var doc = JsonDocument.Parse(response);
        var total = doc.RootElement.GetProperty("total").GetProperty("value").GetInt32();
        var hits = doc.RootElement.GetProperty("hits").EnumerateArray();

        var result = new List<Posting>();

        int apiOffset = 0;
        bool complete = false;

        while (!complete && result.Count < total)
        {
            apiOffset += LIMIT;
            var pagedUrl = $"https://jobsearch.api.jobtechdev.se/search?q={Uri.EscapeDataString(keyword)}&limit={LIMIT}&offset={apiOffset}&sort=pubdate-desc";
            var pagedResponse = await _httpClient.GetStringAsync(pagedUrl);
            using var pagedDoc = JsonDocument.Parse(pagedResponse);
            var pagedHits = pagedDoc.RootElement.GetProperty("hits").EnumerateArray();

            if (!pagedHits.Any())
                break;

            foreach (var hit in pagedHits)
            {
                var posting = new Posting
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
                result.Add(posting);
            }
            if (result.Count >= total)
                complete = true;

            await Task.Delay(100);
        }

        return result;
    }
}