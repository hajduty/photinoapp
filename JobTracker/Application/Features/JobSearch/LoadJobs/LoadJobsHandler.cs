using JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;
using JobTracker.Application.Features.Postings;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Application.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobSearch.LoadJobs;

public record LoadJobsRequest(string Keyword);
public record LoadJobsResponse(int JobsLoaded);

public class LoadJobsHandler : RpcHandler<LoadJobsRequest, LoadJobsResponse>
{
    private readonly ScrapeService _scrapeService;

    public override string Command => "jobSearch.loadJobs2";

    public LoadJobsHandler(ScrapeService scrapeService)
    {
        _scrapeService = scrapeService;
    }

    protected override async Task<LoadJobsResponse> HandleAsync(LoadJobsRequest request)
    {
        var jobTechJobs = await _scrapeService.Fetch(request); //- Run the timed service, dont take string keyword.

        return new LoadJobsResponse(jobTechJobs.JobsLoaded);
    }
}
