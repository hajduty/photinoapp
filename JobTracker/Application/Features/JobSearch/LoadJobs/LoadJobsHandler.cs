using JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;
using JobTracker.Application.Features.Postings;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobSearch.LoadJobs;

public record LoadJobsRequest(string Keyword);
public record LoadJobsResponse(int JobsLoaded);

public class LoadJobsHandler : RpcHandler<LoadJobsRequest, LoadJobsResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly JobTechScraper _jobTechScraper;

    public override string Command => "jobSearch.loadJobs";

    public LoadJobsHandler(IDbContextFactory<AppDbContext> dbFactory, JobTechScraper jobTechScraper)
    {
        _dbFactory = dbFactory;
        _jobTechScraper = jobTechScraper;
    }

    protected override async Task<LoadJobsResponse> HandleAsync(LoadJobsRequest request)
    {
        // Fetch jobs from JobTech API
        var jobTechJobs = await _jobTechScraper.FetchJobsAsync(request.Keyword);

        //var linkedInJobs = await FetchFromLinkedInAsync(keyword);
        //var indeedJobs = await FetchFromIndeedAsync(keyword);

        await using var db = await _dbFactory.CreateDbContextAsync();

        db.Postings.AddRange(jobTechJobs);
        //db.Postings.AddRange(linkedInJobs);
        //db.Postings.AddRange(indeedJobs);

        await db.SaveChangesAsync();
        return new LoadJobsResponse(jobTechJobs.Count);
    }
}
