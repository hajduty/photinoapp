using JobTracker.Application.Features.JobSearch.LoadJobs;
using JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;
using JobTracker.Application.Features.JobTracker.Process;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Services.Scraper;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Infrastructure.Services;

public class ScrapeService
{
    private readonly JobTechScraper _jobTechScraper;
    private readonly LinkedInScraper _linkedInScraper;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ScrapeService(JobTechScraper jobTechScraper, LinkedInScraper linkedInScraper, IDbContextFactory<AppDbContext> dbFactory)
    {
        _linkedInScraper = linkedInScraper;
        _jobTechScraper = jobTechScraper;
        _dbFactory = dbFactory;
    }

    public async Task<LoadJobsResponse> Fetch(LoadJobsRequest request)
    {
        var jobTechJobs = await _jobTechScraper.FetchJobsAsync(request.Keyword);
        var linkedInJobs = await _linkedInScraper.FetchJobsAsync(request.Keyword);

        await using var db = await _dbFactory.CreateDbContextAsync();

        db.Postings.AddRange(jobTechJobs);

        await db.SaveChangesAsync();
        return new LoadJobsResponse(jobTechJobs.Count);
    }
}