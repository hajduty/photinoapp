/*
using JobTracker.Application.Features.JobSearch.GetJobs;
using JobTracker.Application.Features.JobSearch.GetJobTitles;
using JobTracker.Application.Features.JobSearch.LoadJobs;
using JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JobTracker.Tests;

// fast tests, not testing the entire flow, but just that we can fetch job titles, load jobs and fetch jobs from db
public class JobSearchTests
{
    [Fact]
    public async Task GetJobTitlesAsync()
    {
        var sut = new GetJobTitles();
        var result = await sut.ExecuteAsync(new GetJobTitlesRequest("software"));
        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoadJobsAsync()
    {
        var dbFactory = DbFactory.CreateDbFactory();

        var sut = new LoadJobs(dbFactory);

        // Act
        await sut.ExecuteAsync(new LoadJobsRequest("utvecklare"));

        await using var db = await dbFactory.CreateDbContextAsync();
        var jobs = await db.Postings.ToListAsync();

        Debug.WriteLine("Total jobs in db: " + jobs.Count);

        Assert.NotEmpty(jobs);
    }

    [Fact]
    public async Task GetJobsAsync()
    {
        var dbFactory = DbFactory.CreateDbFactory();
        var sut = new GetJobsHandler(dbFactory);

        HttpClient httpClient = new HttpClient();

        var jobTech = new JobTechScraper(httpClient, dbFactory);
        var sut2 = new LoadJobsHandler(dbFactory, jobTech);

        // Act
        await sut2.HandleAsync(new LoadJobsRequest("utvecklare"));

        await Task.Delay(500);

        // Act
        var result = await sut.ExecuteAsync(new GetJobsRequest("utvecklare", 1, 10, new List<int>()));

        Console.WriteLine($"Total results: {result.TotalResults}, Total pages: {result.TotalPages}");
        Debug.WriteLine($"Total results: {result.TotalResults}, Total pages: {result.TotalPages}");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Postings);
    }
}

*/