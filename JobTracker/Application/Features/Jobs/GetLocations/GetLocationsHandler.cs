using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Jobs.GetLocations;

public record LocationsResponse(List<string> Cities, List<string> Counties);

public class GetLocationsHandler : RpcHandler<object?, LocationsResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "jobs.getLocations";

    public GetLocationsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<LocationsResponse> HandleAsync(object? request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var raw = await db.Postings
            .AsNoTracking()
            .Select(p => new { p.City, p.County })
            .ToListAsync();

        var cities = raw
            .Where(p => !string.IsNullOrEmpty(p.City))
            .Select(p => p.City!)
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.GroupBy(c => c).OrderByDescending(x => x.Count()).First().Key)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var counties = raw
            .Where(p => !string.IsNullOrEmpty(p.County))
            .Select(p => p.County!)
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.GroupBy(c => c).OrderByDescending(x => x.Count()).First().Key)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new LocationsResponse(cities, counties);
    }
}
