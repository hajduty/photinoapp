using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.JobSearch.GetJobs;
using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Embeddings;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

public record ScoredJob(Posting Posting, List<Tag> Tags, float Score);

public class JobMatchingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly JinaEmbeddingService _embeddingService;

    public JobMatchingService(
        IDbContextFactory<AppDbContext> dbFactory,
        JinaEmbeddingService embeddingService)
    {
        _dbFactory = dbFactory;
        _embeddingService = embeddingService;
    }

    /// <summary>
    /// Scores and ranks jobs against the user profile. Returns all scored results —
    /// callers decide how many to take and what threshold matters to them.
    /// </summary>
    public async Task<List<ScoredJob>> GetScoredJobsAsync(AppDbContext db, Settings settings)
    {
        var profileText = BuildUserProfile(settings);

        float[] userVector;
        if (settings.UserEmbedding == null)
        {
            userVector = _embeddingService.GenerateEmbeddingFloat(profileText);
            var settingsToUpdate = new Settings { Id = settings.Id };
            db.Settings.Attach(settingsToUpdate);
            settingsToUpdate.UserEmbedding = Helper.ToBytes(userVector);
            await db.SaveChangesAsync();
        }
        else
        {
            userVector = Helper.ToFloatArray(settings.UserEmbedding);
        }

        var maxAge = settings.MaxJobAgeDays ?? 30;

        var candidates = await db.Postings
            .AsNoTracking()
            .Where(p => p.Ignored != true && p.PostedDate >= DateTime.UtcNow.AddDays(-maxAge) && p.SoftIgnore != true)
            .OrderByDescending(p => p.PostedDate)
            .Take(1000)
            .ToListAsync();

        var embeddings = await db.JobEmbeddings
            .AsNoTracking()
            .Where(e => candidates.Select(c => c.Id).Contains(e.JobId))
            .ToDictionaryAsync(e => e.JobId, e => e.EmbeddingData);

        var negativeVector = await BuildNegativeVectorAsync(db);
        var bookmarkVector = await BuildBookmarkVectorAsync(db);

        var allTags = await db.Tags.AsNoTracking().ToListAsync();
        var tagRegexes = allTags.ToDictionary(
            t => t.Id,
            t => new Regex(CreateTagPattern(t.Name), RegexOptions.IgnoreCase | RegexOptions.Compiled));

        var scored = new List<ScoredJob>();

        foreach (var job in candidates)
        {
            var jobTags = ExtractTags(job, allTags, tagRegexes);

            if (!ContainsSelectedTag(jobTags, settings)) continue;
            if (!PassesHardFilters(job, settings)) continue;
            if (!embeddings.TryGetValue(job.Id, out var embeddingBytes)) continue;

            var jobVector = Helper.ToFloatArray(embeddingBytes);

            float semantic = Helper.DotProductSimilarity(userVector, jobVector);
            float keywordBoost = KeywordBoost(job, settings);
            float freshnessBoost = FreshnessBoost(job);
            float negativePenalty = negativeVector != null
                ? Helper.DotProductSimilarity(negativeVector, jobVector) : 0f;
            float bookmarkBoost = bookmarkVector != null
                ? Helper.DotProductSimilarity(bookmarkVector, jobVector) : 0f;
            float yoePenalty = YearsOfExperiencePenalty(job, settings);

            float score =
                semantic * 0.55f +
                keywordBoost * 0.35f +
                freshnessBoost * 0.10f -
                negativePenalty * 0.50f +
                bookmarkBoost * 0.30f -
                yoePenalty;

            scored.Add(new ScoredJob(job, jobTags, score));
        }

        return scored.OrderByDescending(x => x.Score).ToList();
    }

    private static async Task<float[]?> BuildNegativeVectorAsync(AppDbContext db)
    {
        var ignoredIds = await db.Postings
            .AsNoTracking()
            .Where(p => p.Ignored == true)
            .Select(p => p.Id)
            .ToListAsync();

        if (ignoredIds.Count == 0)
            return null;

        var ignoredEmbeddings = await db.JobEmbeddings
            .AsNoTracking()
            .Where(e => ignoredIds.Contains(e.JobId))
            .Select(e => e.EmbeddingData)
            .ToListAsync();

        if (ignoredEmbeddings.Count == 0)
            return null;

        return BuildCentroid(ignoredEmbeddings);
    }

    private static async Task<float[]?> BuildBookmarkVectorAsync(AppDbContext db)
    {
        var bookmarkedIds = await db.Postings
            .AsNoTracking()
            .Where(p => p.Bookmarked == true)
            .Select(p => p.Id)
            .ToListAsync();

        if (bookmarkedIds.Count == 0)
            return null;

        var bookmarkedEmbeddings = await db.JobEmbeddings
            .AsNoTracking()
            .Where(e => bookmarkedIds.Contains(e.JobId))
            .Select(e => e.EmbeddingData)
            .ToListAsync();

        if (bookmarkedEmbeddings.Count == 0)
            return null;

        return BuildCentroid(bookmarkedEmbeddings);
    }

    private static float[] BuildCentroid(List<byte[]> embeddingsList)
    {
        var first = Helper.ToFloatArray(embeddingsList[0]);
        var centroid = new float[first.Length];

        foreach (var bytes in embeddingsList)
        {
            var vec = Helper.ToFloatArray(bytes);
            for (int i = 0; i < centroid.Length; i++)
                centroid[i] += vec[i];
        }

        for (int i = 0; i < centroid.Length; i++)
            centroid[i] /= embeddingsList.Count;

        return centroid;
    }

    private static string BuildUserProfile(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.UserCV))
            return $"CV: {settings.UserCV}";

        return string.Empty;
    }

    private static bool PassesHardFilters(Posting job, Settings settings)
    {
        if (settings.BlockedKeywords != null)
        {
            foreach (var k in settings.BlockedKeywords)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;

                var pattern = $@"(?<![a-zA-Z0-9]){Regex.Escape(k)}(?![a-zA-Z0-9])";
                var rx = new Regex(pattern, RegexOptions.IgnoreCase);

                if (rx.IsMatch(job.Title ?? "") || rx.IsMatch(job.Description ?? ""))
                    return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.Location))
        {
            if (!job.Location.Contains(settings.Location, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static float YearsOfExperiencePenalty(Posting job, Settings settings)
    {
        if (settings.YearsOfExperience is null || job.YearsOfExperience is null || job.YearsOfExperience == 0)
            return 0f;

        var gap = job.YearsOfExperience.Value - settings.YearsOfExperience.Value;

        if (gap <= 0) return 0f;
        if (gap == 1) return 0.15f;
        if (gap == 2) return 0.30f;
        if (gap == 3) return 0.45f;
        return 0.60f;
    }

    private static float KeywordBoost(Posting job, Settings settings)
    {
        if (settings.MatchedKeywords == null)
            return 0f;

        float boost = 0;

        foreach (var keyword in settings.MatchedKeywords)
        {
            if (job.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                boost += 0.2f;
            else if (job.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                boost += 0.1f;
        }

        return MathF.Min(boost, 0.5f);
    }

    private static float FreshnessBoost(Posting job)
    {
        var ageDays = (DateTime.UtcNow - job.PostedDate).TotalDays;

        if (ageDays <= 1) return 1f;
        if (ageDays <= 7) return 0.8f;
        if (ageDays <= 14) return 0.5f;
        if (ageDays <= 30) return 0.2f;

        return 0f;
    }

    private static List<ExtendedPosting> CreateExtended(
        List<Posting> jobs,
        List<Tag> allTags,
        Dictionary<int, Regex> regexes)
    {
        return jobs.Select(job =>
        {
            var tags = ExtractTags(job, allTags, regexes);

            return new ExtendedPosting
            {
                Posting = job,
                Tags = tags
            };
        }).ToList();
    }

    private static List<Tag> ExtractTags(
        Posting job,
        List<Tag> allTags,
        Dictionary<int, Regex> regexes)
    {
        var title = job.Title ?? "";
        var desc = job.Description ?? "";

        return allTags
            .Where(t =>
            {
                var rx = regexes[t.Id];
                return rx.IsMatch(title) || rx.IsMatch(desc);
            })
            .ToList();
    }

    private static bool ContainsSelectedTag(List<Tag> jobTags, Settings settings)
    {
        if (settings.SelectedTags == null || settings.SelectedTags.Count == 0)
            return true;

        var selectedIds = settings.SelectedTags
            .Select(t => t.Id)
            .ToHashSet();

        return jobTags.Any(t => selectedIds.Contains(t.Id));
    }

    private static string CreateTagPattern(string tagName)
    {
        var escaped = Regex.Escape(tagName);
        return $@"(?:^|[\s,;.!?()\[\]{{}}""'`])({escaped})(?:$|[\s,;.!?()\[\]{{}}""'`])";
    }
}