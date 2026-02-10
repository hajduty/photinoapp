using JobTracker.Application.Features.JobSearch.LoadJobs;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Events;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Features.JobTracker.Process;

public record ProcessJobTrackerResponse(int JobsAdded);

public class ProcessJobTrackerHandler 
    : RpcHandler<object?, ProcessJobTrackerResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventEmitter _events;

    public override string Command => "jobTracker.process";

    public ProcessJobTrackerHandler(
        IDbContextFactory<AppDbContext> dbFactory, 
        IServiceProvider serviceProvider,
        IEventEmitter events)
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
        _events = events;
    }

    protected async override Task<ProcessJobTrackerResponse> HandleAsync(object? request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var alerts = await dbContext.JobTrackers.ToListAsync();

        var dispatcher = _serviceProvider.GetRequiredService<RpcDispatcher>();
        int totalJobsAdded = 0;

        foreach (var alert in alerts)
        {
            Console.WriteLine($"Processing alert for keyword: {alert.Keyword}");
            
            // Fetch jobs for this alert's keyword
            var loadJobsRequest = JsonSerializer.Serialize(new
            {
                command = "jobSearch.loadJobs",
                id = Guid.NewGuid().ToString(),
                payload = new { keyword = alert.Keyword }
            });

            int amountOfJobsAdded = 0;

            try
            {
                var result = await dispatcher.DispatchAsync(loadJobsRequest);

                // Parse the JSON response and extract the data property
                using var doc = JsonDocument.Parse(result);
                var dataElement = doc.RootElement.GetProperty("data");
                var response = JsonSerializer.Deserialize<LoadJobsResponse>(dataElement.GetRawText());

                if (response != null)
                {
                    amountOfJobsAdded = response.JobsLoaded;
                    totalJobsAdded += amountOfJobsAdded;
                }

                Debug.WriteLine($"Fetched jobs for '{alert.Keyword}': {result}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch jobs for '{alert.Keyword}': {ex.Message}");
            }

            // Jobs loaded to db, time to filter and notify user
            var escapedKeyword = alert.Keyword
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");

            var pattern = $"%{escapedKeyword}%";

            var query = dbContext.Postings
                .AsNoTracking()
                .Where(p =>
                    EF.Functions.Like(p.Title, pattern) ||
                    EF.Functions.Like(p.Company, pattern) ||
                    EF.Functions.Like(p.Description, pattern));

            var allTags = await dbContext.Tags.AsNoTracking().ToListAsync();

            var wantedTags = alert.Tags;

            var tagNames = wantedTags.Select(t => t.Name).ToList();

            var postings = await query.ToListAsync();

            foreach (var tagName in tagNames)
            {
                var patterns = CreateTagPattern(tagName);
                var regex = new Regex(patterns, RegexOptions.IgnoreCase);
                postings = postings.Where(p =>
                    (p.Title != null && regex.IsMatch(p.Title)) ||
                    (p.Description != null && regex.IsMatch(p.Description))
                ).ToList();
            }
            alert.LastCheckedAt = DateTime.Now;

            // Emit event to frontend with found jobs data
            if (postings.Any())
            {
                _events.Emit("jobTracker:jobsFound", new
                {
                    TrackerId = alert.Id,
                    Keyword = alert.Keyword,
                    Count = postings.Count,
                    Jobs = postings.Select(p => new { p.Id, p.Title, p.Company })
                });
            }

            // 
        }

        dbContext.SaveChanges();

        // Emit completion event
        _events.Emit("jobTracker:completed", new
        {
            TotalJobsAdded = totalJobsAdded,
            ProcessedAt = DateTime.UtcNow
        });

        return new ProcessJobTrackerResponse(totalJobsAdded);
    }

    private string CreateTagPattern(string tagName)
    {
        var escaped = Regex.Escape(tagName);
        return $@"(?:^|[\s,;.!?()\[\]{{}}""""])({escaped})(?:$|[\s,;.!?()\[\]{{}}""""])";
    }
}
