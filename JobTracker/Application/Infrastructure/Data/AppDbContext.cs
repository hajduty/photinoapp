using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Features.SemanticSearch;
using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Features.Tags;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Posting> Postings { get; set; } = null!;
    public DbSet<Features.JobTracker.JobTracker> JobTrackers { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Settings> Settings { get; set; } = null!;
    public DbSet<JobEmbedding> JobEmbeddings { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure many-to-many relationship between JobAlert and Tag
        modelBuilder.Entity<Features.JobTracker.JobTracker>()
            .HasMany(j => j.Tags)
            .WithMany(t => t.JobTrackers)
            .UsingEntity(j => j.ToTable("JobTrackerTags"));

        modelBuilder.Entity<JobEmbedding>()
                .HasOne<Posting>()
                .WithOne()
                .HasForeignKey<JobEmbedding>(je => je.JobId)
                .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobEmbedding>()
            .HasIndex(je => je.JobId)
            .IsUnique();
    }
}