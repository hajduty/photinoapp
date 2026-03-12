using JobTracker.Application.Events;
using JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Infrastructure.Services;
public record LoadJobsRequest(string Keyword);
public record LoadJobsResponse(int JobsLoaded);
public class ScrapeService
{
    private readonly JobTechScraper _jobTechScraper;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IUiEventEmitter _uiEventEmitter;

    public ScrapeService(JobTechScraper jobTechScraper, IDbContextFactory<AppDbContext> dbFactory, IUiEventEmitter uiEventEmitter)
    {
        _jobTechScraper = jobTechScraper;
        _dbFactory = dbFactory;
        _uiEventEmitter = uiEventEmitter;
    }

    public async Task<LoadJobsResponse> Fetch(LoadJobsRequest request)
    {
        var jobTechJobs = await _jobTechScraper.FetchJobsAsync(request.Keyword);

        await using var db = await _dbFactory.CreateDbContextAsync();

        db.Postings.AddRange(jobTechJobs);

        await db.SaveChangesAsync();

        // Send UI event, invalidate job cache
        _uiEventEmitter.Emit("jobs.invalidate");

        return new LoadJobsResponse(jobTechJobs.Count);
    }
}