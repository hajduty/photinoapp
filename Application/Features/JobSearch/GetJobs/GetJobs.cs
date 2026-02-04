using JobTracker.Application.Features.Postings;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.JobSearch.GetJobs;

public record GetJobsRequest(string Keyword, int Limit = 10, int Offset = 0);
public record GetJobsResponse(int TotalCount,  List<Posting> Jobs);

public class GetJobs
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly HttpClient _httpClient;
    private readonly string apiUrl = "https://jobsearch.api.jobtechdev.se/";

    public GetJobs(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        _httpClient = new HttpClient();
    }

    public async Task<GetJobsResponse> ExecuteAsync(GetJobsRequest request)
    {
        // Optional: persist something locally if needed
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        // Example: maybe you log that this search was executed
        // dbContext.SearchLogs.Add(new SearchLog { Keyword = request.Keyword, Timestamp = DateTime.UtcNow });
        // await dbContext.SaveChangesAsync();

        // Call external API
        var query = $"search?q={request.Keyword}&offset={request.Offset}&limit={request.Limit}&sort=pubdate-desc";
        var response = await _httpClient.GetStringAsync(apiUrl + query);

        var postings = JobSearchToPosting.Convert(response);

        return postings;
    }
}
