using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Application.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Embeddings.GetDescription;

public record GetJobDescriptionRequest(int JobId);
public record GetJobDescriptionResponse(List<JobSentence> Sentences);

public class GetDescriptionHandler 
    : RpcHandler<GetJobDescriptionRequest, GetJobDescriptionResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbContext;
    private readonly SentenceClassifierService _classifierService;

    public GetDescriptionHandler(IDbContextFactory<AppDbContext> dbContext, SentenceClassifierService classifierService)
    {
        _dbContext = dbContext;
        _classifierService = classifierService;
    }

    public override string Command => "embeddings.getDescription";

    protected override async Task<GetJobDescriptionResponse> HandleAsync(GetJobDescriptionRequest request)
    {
        await using var db = await _dbContext.CreateDbContextAsync();
        
        //var descriptions = await db.JobEmbeddings.

        var embeddings = db.JobEmbeddings.Where(j => j.JobId == request.JobId).ToList();

        // reclassify
        foreach (var embedding in embeddings)
        {
            var data = _classifierService.ClassifyWithScore(embedding.Data);

            embedding.SentenceType = data.Category;
            embedding.Score = data.Score;
        }

        await db.SaveChangesAsync();

        var sentences = embeddings.Select(e => new JobSentence
        {
            Id = e.Id,
            JobId = e.JobId,
            Start = e.Start,
            Length = e.Length,
            Sentence = e.Sentence,
            SentenceType = e.SentenceType,
            Score = e.Score
        }).ToList();

        return new GetJobDescriptionResponse(sentences);
    }
}
