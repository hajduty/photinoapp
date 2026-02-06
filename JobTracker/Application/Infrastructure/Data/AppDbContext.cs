using JobTracker.Application.Features.JobAlerts;
using JobTracker.Application.Features.Postings;
using JobTracker.Application.Features.Tags;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Posting> Postings { get; set; } = null!;
    public DbSet<JobAlert> JobAlerts { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
} 