using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Jobs.GetFullDescription;

public record GetFullDescriptionRequest(int JobId);
public record GetFullDescriptionResponse(string Description, string DescriptionFormatted);

public class GetFullDescription : RpcHandler<GetFullDescriptionRequest, GetFullDescriptionResponse>
{
    public override string Command => "jobs.getFullDescription";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetFullDescription(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetFullDescriptionResponse> HandleAsync(GetFullDescriptionRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var posting = await db.Postings.FindAsync(request.JobId);

        if (posting == null)
            return new GetFullDescriptionResponse("", ""); // send ui toast later

        return new GetFullDescriptionResponse(posting.Description, posting.DescriptionFormatted);
    }
}
