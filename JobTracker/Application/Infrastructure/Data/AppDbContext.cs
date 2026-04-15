using JobTracker.Application.Features.Classifications;
using JobTracker.Application.Features.Embeddings;
using JobTracker.Application.Features.JobApplication;
using JobTracker.Application.Features.Jobs;
using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Features.System.Profiles;
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
    public DbSet<JobProfile> JobProfiles { get; set; } = null!;
    public DbSet<ProfileIgnoredJob> ProfileIgnoredJobs { get; set; } = null!;
    public DbSet<ProfileBookmarkedJob> ProfileBookmarkedJobs { get; set; } = null!;

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

        var keywordRuleListComparer = new ValueComparer<List<KeywordRule>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        var rejectedTagRuleListComparer = new ValueComparer<List<RejectedTagRule>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        // --- JobTracker ---
        modelBuilder.Entity<Features.JobTracker.JobTracker>()
            .HasMany(j => j.Tags)
            .WithMany(t => t.JobTrackers)
            .UsingEntity(j => j.ToTable("JobTrackerTags"));

        modelBuilder.Entity<Features.JobTracker.JobTracker>()
            .HasOne<JobProfile>()
            .WithMany()
            .HasForeignKey(j => j.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- JobApplication ---
        modelBuilder.Entity<JobApplication>()
            .HasIndex(je => new { je.ProfileId, je.JobId })
            .IsUnique();

        modelBuilder.Entity<JobApplication>()
            .HasOne(je => je.Posting)
            .WithMany()
            .HasForeignKey(je => je.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobApplication>()
            .HasOne<JobProfile>()
            .WithMany()
            .HasForeignKey(je => je.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationStatusHistory>()
            .HasOne(h => h.JobApplication)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(h => h.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Classification ---
        modelBuilder.Entity<Classification>()
            .HasMany(c => c.Prototypes)
            .WithOne(p => p.Classification)
            .HasForeignKey(p => p.ClassificationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Classification>()
            .HasIndex(c => c.Name)
            .IsUnique();

        // --- JobProfile JSON columns ---
        modelBuilder.Entity<JobProfile>()
            .Property(u => u.BlockedKeywords)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => SafeDeserializeKeywordRules(v))
            .Metadata.SetValueComparer(keywordRuleListComparer);
        modelBuilder.Entity<JobProfile>().Property(u => u.BlockedKeywords).HasColumnType("TEXT");

        modelBuilder.Entity<JobProfile>()
            .Property(u => u.MatchedKeywords)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => SafeDeserializeKeywordRules(v))
            .Metadata.SetValueComparer(keywordRuleListComparer);
        modelBuilder.Entity<JobProfile>().Property(u => u.MatchedKeywords).HasColumnType("TEXT");

        modelBuilder.Entity<JobProfile>()
            .Property(u => u.Locations)
            .HasColumnName("Location")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => DeserializeStringList(v))
            .Metadata.SetValueComparer(stringListComparer);
        modelBuilder.Entity<JobProfile>().Property(u => u.Locations).HasColumnType("TEXT");

        modelBuilder.Entity<JobProfile>()
            .Property(u => u.BlockedLocations)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default)!)
            .Metadata.SetValueComparer(stringListComparer);
        modelBuilder.Entity<JobProfile>().Property(u => u.BlockedLocations).HasColumnType("TEXT");

        modelBuilder.Entity<JobProfile>()
            .Property(u => u.RejectedSeniorityLevels)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default)!)
            .Metadata.SetValueComparer(stringListComparer);
        modelBuilder.Entity<JobProfile>().Property(u => u.RejectedSeniorityLevels).HasColumnType("TEXT");

        modelBuilder.Entity<JobProfile>()
            .Property(u => u.RejectedTechKeywords)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => SafeDeserializeRejectedTagRules(v))
            .Metadata.SetValueComparer(rejectedTagRuleListComparer);
        modelBuilder.Entity<JobProfile>().Property(u => u.RejectedTechKeywords).HasColumnType("TEXT");

        // JobProfile ↔ Tags (many-to-many)
        modelBuilder.Entity<JobProfile>()
            .HasMany(p => p.SelectedTags)
            .WithMany()
            .UsingEntity(j => j.ToTable("JobProfileTags"));

        // --- ProfileIgnoredJob ---
        modelBuilder.Entity<ProfileIgnoredJob>()
            .HasIndex(p => new { p.ProfileId, p.PostingId })
            .IsUnique();

        modelBuilder.Entity<ProfileIgnoredJob>()
            .HasOne(p => p.Profile)
            .WithMany()
            .HasForeignKey(p => p.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProfileIgnoredJob>()
            .HasOne<Posting>()
            .WithMany()
            .HasForeignKey(p => p.PostingId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- ProfileBookmarkedJob ---
        modelBuilder.Entity<ProfileBookmarkedJob>()
            .HasIndex(p => new { p.ProfileId, p.PostingId })
            .IsUnique();

        modelBuilder.Entity<ProfileBookmarkedJob>()
            .HasOne(p => p.Profile)
            .WithMany()
            .HasForeignKey(p => p.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProfileBookmarkedJob>()
            .HasOne<Posting>()
            .WithMany()
            .HasForeignKey(p => p.PostingId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static List<string> DeserializeStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOptions.Default) ?? [];
        }
        catch
        {
            // Migration fallback: old data was a plain string, not a JSON array
            return string.IsNullOrWhiteSpace(json) ? [] : [json];
        }
    }

    private static List<KeywordRule> SafeDeserializeKeywordRules(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<KeywordRule>>(json, JsonSerializerOptions.Default) ?? [];
        }
        catch
        {
            try
            {
                var old = JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOptions.Default);
                return old?.Select(k => new KeywordRule(k, KeywordScope.Both)).ToList() ?? [];
            }
            catch { return []; }
        }
    }

    private static List<RejectedTagRule> SafeDeserializeRejectedTagRules(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<RejectedTagRule>>(json, JsonSerializerOptions.Default) ?? [];
        }
        catch
        {
            try
            {
                var old = JsonSerializer.Deserialize<List<int>>(json, JsonSerializerOptions.Default);
                return old?.Select(id => new RejectedTagRule(id, KeywordScope.Both)).ToList() ?? [];
            }
            catch { return []; }
        }
    }
}
