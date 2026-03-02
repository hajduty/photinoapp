using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using Services;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Classifications.CreatePrototype;
[ExportTsInterface]
public record CreatePrototypeRequest(int ClassificationId, string Text);
[ExportTsInterface]
public record CreatePrototypeResponse(Prototype Prototype);

public class CreatePrototypeHandler
    : RpcHandler<CreatePrototypeRequest, CreatePrototypeResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly JinaEmbeddingService _embeddingService;

    public override string Command => "prototype.create";

    public CreatePrototypeHandler(
        IDbContextFactory<AppDbContext> dbContextFactory,
        JinaEmbeddingService embeddingService)
    {
        _dbContextFactory = dbContextFactory;
        _embeddingService = embeddingService;
    }

    protected override async Task<CreatePrototypeResponse> HandleAsync(CreatePrototypeRequest request)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var vector = _embeddingService.GenerateEmbeddingFloat(request.Text);
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);

        var prototype = new Prototype
        {
            ClassificationId = request.ClassificationId,
            Text = request.Text,
            Embedding = bytes
        };

        db.Prototypes.Add(prototype);
        await db.SaveChangesAsync();

        return new CreatePrototypeResponse(prototype);
    }
}