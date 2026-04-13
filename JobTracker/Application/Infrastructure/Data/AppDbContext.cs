using JobTracker.Application.Features.Classifications;
using JobTracker.Application.Features.Embeddings;
using JobTracker.Application.Features.JobApplication;
using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Features.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace JobTracker.Application.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Posting> Postings { get; set; } = null!;
    public DbSet<Features.JobTracker.JobTracker> JobTrackers { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Settings> Settings { get; set; } = null!;
    public DbSet<JobApplication> JobApplications { get; set; } = null!;
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; } = null!;
    public DbSet<Prototype> Prototypes { get; set; } = null!;
    public DbSet<Classification> Classifications { get; set; } = null!;
    public DbSet<JobEmbedding> JobEmbeddings { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        var intListComparer = new ValueComparer<List<int>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v)),
            c => c.ToList());

        modelBuilder.Entity<Features.JobTracker.JobTracker>()
            .HasMany(j => j.Tags)
            .WithMany(t => t.JobTrackers)
            .UsingEntity(j => j.ToTable("JobTrackerTags"));
        // --------------
        modelBuilder.Entity<JobApplication>()
            .HasIndex(je => je.JobId)
            .IsUnique();

        modelBuilder.Entity<JobApplication>()
            .HasOne<Posting>()
            .WithOne()
            .HasForeignKey<JobApplication>(je => je.JobId)
            .OnDelete(DeleteBehavior.Cascade);
        // --------------
        modelBuilder.Entity<ApplicationStatusHistory>()
            .HasOne(h => h.JobApplication)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(h => h.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        // --------------
        modelBuilder.Entity<Classification>()
            .HasMany(c => c.Prototypes)
            .WithOne(p => p.Classification)
            .HasForeignKey(p => p.ClassificationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Classification>()
            .HasIndex(c => c.Name)
            .IsUnique();
        // --------------
        modelBuilder.Entity<Settings>()
            .Property(u => u.BlockedKeywords)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default)!)
            .Metadata.SetValueComparer(stringListComparer);
        modelBuilder.Entity<Settings>().Property(u => u.BlockedKeywords).HasColumnType("TEXT");

        modelBuilder.Entity<Settings>()
            .Property(u => u.MatchedKeywords)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default)!)
            .Metadata.SetValueComparer(stringListComparer);
        modelBuilder.Entity<Settings>().Property(u => u.MatchedKeywords).HasColumnType("TEXT");

        modelBuilder.Entity<Settings>()
            .Property(u => u.BlockedLocations)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default)!)
            .Metadata.SetValueComparer(stringListComparer);
        modelBuilder.Entity<Settings>().Property(u => u.BlockedLocations).HasColumnType("TEXT");

        modelBuilder.Entity<Settings>()
            .Property(u => u.RejectedSeniorityLevels)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default)!)
            .Metadata.SetValueComparer(stringListComparer);
        modelBuilder.Entity<Settings>().Property(u => u.RejectedSeniorityLevels).HasColumnType("TEXT");

        modelBuilder.Entity<Settings>()
            .Property(u => u.RejectedTechKeywords)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => SafeDeserializeIntList(v))
            .Metadata.SetValueComparer(intListComparer);
        modelBuilder.Entity<Settings>().Property(u => u.RejectedTechKeywords).HasColumnType("TEXT");

        modelBuilder.Entity<Settings>()
            .HasMany(u => u.SelectedTags)
            .WithMany()
            .UsingEntity(j => j.ToTable("UserPreferenceTags"));
    }

    private static List<int> SafeDeserializeIntList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<int>();
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json, JsonSerializerOptions.Default) ?? new List<int>();
        }
        catch
        {
            // If it was previously a List<string>, return an empty list to avoid crashes
            return new List<int>();
        }
    }
}