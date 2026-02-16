using JobTracker.Application.Features.JobSearch.GetJobs;
using JobTracker.Application.Features.Postings;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Application.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace JobTracker.Application.Features.SemanticSearch.Search;

public record SemanticSearchRequest(string Keyword, int Page, int PageSize);
public record SemanticSearchResponse(List<RankedPostingResult> Postings, int Page, int PageSize, int TotalResults, int TotalPages, bool HasPreviousPage, bool HasNextPage);
public class RankedPostingResult
{
    public int Id { get; init; }
    public Posting Posting { get; init; } = null!;
    public float Score { get; init; }
}

public class SearchHandler
    : RpcHandler<SemanticSearchRequest, SemanticSearchResponse>
{
    private readonly EmbeddingService _embeddingService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly OllamaService _ollamaService;

    private readonly int MAX_LIMIT = 10;

    public override string Command => "semanticSearch.query";

    public SearchHandler(EmbeddingService embeddingService, IDbContextFactory<AppDbContext> dbFactory, OllamaService ollamaService)
    {
        _embeddingService = embeddingService;
        _dbFactory = dbFactory;
        _ollamaService = ollamaService;
    }


    public async Task<List<RankedPostingResult>> GetRankedResults(string keyword)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        Debug.WriteLine($"Searching for {keyword}");

        // Generate query embedding
        var queryVector = await _ollamaService.GenerateEmbeddingAsync(keyword);
        var normalizedQuery = EmbeddingService.Normalize(queryVector);

        // Load embeddings
        var embeddings = await db.JobEmbeddings.ToListAsync();

        // Rank them in memory
        var ranked = embeddings
            .Select(e => new
            {
                e.JobId,
                Score = EmbeddingService.Dot(normalizedQuery, EmbeddingService.FromBytes(e.Data))
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        Debug.WriteLine($"Got {ranked.Count} results in MMEORY?");

        var rankedIds = ranked.Select(r => r.JobId).ToList();

        // Fetch postings
        var postings = await db.Postings
            .Where(p => rankedIds.Contains(p.Id))
            .ToListAsync();

        var postingDict = postings.ToDictionary(p => p.Id);

        // Build final ordered results
        var results = ranked
            .Where(r => postingDict.ContainsKey(r.JobId))
            .Select(r => new RankedPostingResult
            {
                Id = r.JobId,
                Posting = postingDict[r.JobId],
                Score = r.Score
            })
            .ToList();

        return results;
    }

    protected async override Task<SemanticSearchResponse> HandleAsync(SemanticSearchRequest request)
    {
        // Ensure page & pageSize are valid
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Min(request.PageSize, MAX_LIMIT);

        // Get ranked postings
        var rankedResults = await GetRankedResults(request.Keyword);

        // Compute total results & total pages
        var totalResults = rankedResults.Count;
        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        // Paginate results
        var pagedResults = rankedResults
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Build response
        var response = new SemanticSearchResponse(
            Postings: pagedResults,
            Page: page,
            PageSize: pageSize,
            TotalResults: totalResults,
            TotalPages: totalPages,
            HasPreviousPage: page > 1,
            HasNextPage: page < totalPages
        );

        return response;
    }
}
