using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Classifications.CreateClassification;

[ExportTsInterface]
public record CreateClassificationRequest(string Name, string Color);
[ExportTsInterface] 
public record CreateClassificationResponse(Classification Classification);

public class CreateClassificationHandler
    : RpcHandler<CreateClassificationRequest, CreateClassificationResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    public override string Command => "classifications.create";

    public CreateClassificationHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<CreateClassificationResponse> HandleAsync(CreateClassificationRequest request)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var classification = new Classification
        {
            Name = request.Name,
            Color = request.Color
        };

        db.Classifications.Add(classification);
        await db.SaveChangesAsync();

        return new CreateClassificationResponse(classification);
    }
}