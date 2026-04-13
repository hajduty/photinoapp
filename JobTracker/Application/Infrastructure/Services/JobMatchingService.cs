using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.JobSearch.GetJobs;
using JobTracker.Application.Features.System.Profiles;
using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Embeddings;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;
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

    public async Task<List<ScoredJob>> GetScoredJobsAsync(AppDbContext db, JobProfile profile)
    {
        var profileText = BuildUserProfile(profile);

        float[] userVector;
        if (profile.UserEmbedding == null)
        {
            userVector = _embeddingService.GenerateEmbeddingFloat(profileText);
            var profileToUpdate = new JobProfile { Id = profile.Id };
            db.JobProfiles.Attach(profileToUpdate);
            profileToUpdate.UserEmbedding = Helper.ToBytes(userVector);
            await db.SaveChangesAsync();
        }
        else
        {
            userVector = Helper.ToFloatArray(profile.UserEmbedding);
        }

        var maxAge = profile.MaxJobAgeDays ?? 30;
        var cutoff = DateTime.UtcNow.AddDays(-maxAge);

        var candidates = await db.Postings
            .AsNoTracking()
            .Where(p =>
                p.PostedDate >= cutoff &&
                !db.ProfileIgnoredJobs.Any(pij => pij.ProfileId == profile.Id && pij.PostingId == p.Id))
            .OrderByDescending(p => p.PostedDate)
            .Take(1000)
            .ToListAsync();

        var embeddings = await db.JobEmbeddings
            .AsNoTracking()
            .Where(e => candidates.Select(c => c.Id).Contains(e.JobId))
            .ToDictionaryAsync(e => e.JobId, e => e.EmbeddingData);

        var bookmarkVector = await BuildBookmarkVectorAsync(db);

        var allTags = await db.Tags.AsNoTracking().ToListAsync();
        var tagRegexes = allTags.ToDictionary(
            t => t.Id,
            t => new Regex(CreateTagPattern(t.Name), RegexOptions.IgnoreCase | RegexOptions.Compiled));

        var scored = new List<ScoredJob>();

        foreach (var job in candidates)
        {
            var jobTags = ExtractTags(job, allTags, tagRegexes);

            if (!ContainsSelectedTag(jobTags, profile)) continue;
            if (!PassesHardFilters(job, profile)) continue;
            if (!embeddings.TryGetValue(job.Id, out var embeddingBytes)) continue;

            var jobVector = Helper.ToFloatArray(embeddingBytes);

            float semantic = Helper.DotProductSimilarity(userVector, jobVector);
            float matchedKeywordBoost = MatchedKeywordBoost(job, profile);
            float selectedTagBoost = SelectedTagBoost(job, jobTags, profile);
            float freshnessBoost = FreshnessBoost(job);
            float bookmarkBoost = bookmarkVector != null
                ? Helper.DotProductSimilarity(bookmarkVector, jobVector) : 0f;
            float yoePenalty = YearsOfExperiencePenalty(job, profile);
            float rejectedKeywordPenalty = RejectedTechKeywordPenalty(job, profile, tagRegexes);

            float score =
                semantic * 0.15f +
                matchedKeywordBoost * 0.55f +
                selectedTagBoost * 0.55f +
                freshnessBoost * 0.10f +
                bookmarkBoost * 0.30f -
                yoePenalty -
                rejectedKeywordPenalty;

            scored.Add(new ScoredJob(job, jobTags, score));
        }

        return scored.OrderByDescending(x => x.Score).ToList();
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

    private static string BuildUserProfile(JobProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.UserCV))
            return $"CV: {profile.UserCV}";

        return string.Empty;
    }

    private static bool PassesHardFilters(Posting job, JobProfile profile)
    {
        if (profile.BlockedKeywords != null)
        {
            var title = job.Title ?? "";
            var desc = job.Description ?? "";

            foreach (var rule in profile.BlockedKeywords)
            {
                if (string.IsNullOrWhiteSpace(rule.Keyword)) continue;

                var pattern = $@"(?<![a-zA-Z0-9]){Regex.Escape(rule.Keyword)}(?![a-zA-Z0-9])";
                var rx = new Regex(pattern, RegexOptions.IgnoreCase);

                bool blocked = rule.Scope switch
                {
                    KeywordScope.TitleOnly       => rx.IsMatch(title),
                    KeywordScope.DescriptionOnly => rx.IsMatch(desc),
                    _                            => rx.IsMatch(title) || rx.IsMatch(desc)
                };

                if (blocked) return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.Location))
        {
            if (!job.Location.Contains(profile.Location, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (profile.BlockedLocations?.Count > 0)
        {
            foreach (var loc in profile.BlockedLocations)
            {
                if (!string.IsNullOrWhiteSpace(loc) && job.Location.Contains(loc, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        if (profile.RejectedSeniorityLevels?.Count > 0)
        {
            var jobSeniority = DetectSeniorityLevel(job);
            if (jobSeniority != null && profile.RejectedSeniorityLevels.Contains(jobSeniority, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static float RejectedTechKeywordPenalty(Posting job, JobProfile profile, Dictionary<int, Regex> tagRegexes)
    {
        if (profile.RejectedTechKeywords == null || profile.RejectedTechKeywords.Count == 0)
            return 0f;

        float penalty = 0f;
        var title = job.Title ?? "";
        var desc = job.Description ?? "";

        foreach (var rule in profile.RejectedTechKeywords)
        {
            if (!tagRegexes.TryGetValue(rule.TagId, out var rx)) continue;

            bool matches = rule.Scope switch
            {
                KeywordScope.TitleOnly       => rx.IsMatch(title),
                KeywordScope.DescriptionOnly => rx.IsMatch(desc),
                _                            => rx.IsMatch(title) || rx.IsMatch(desc)
            };

            if (matches) penalty += 0.4f;
        }

        return penalty;
    }

    private static string? DetectSeniorityLevel(Posting job)
    {
        var years = job.YearsOfExperience ?? 0;
        if (years <= 1) return "junior";
        if (years <= 4) return "mid";
        if (years <= 6) return "senior";
        return "lead";
    }

    private static float YearsOfExperiencePenalty(Posting job, JobProfile profile)
    {
        if (profile.YearsOfExperience is null || job.YearsOfExperience is null || job.YearsOfExperience == 0)
            return 0f;

        var gap = job.YearsOfExperience.Value - profile.YearsOfExperience.Value;

        if (gap <= 0) return 0f;
        if (gap == 1) return 0.15f;
        if (gap == 2) return 0.30f;
        if (gap == 3) return 0.45f;
        return 0.60f;
    }

    private static float MatchedKeywordBoost(Posting job, JobProfile profile)
    {
        if (profile.MatchedKeywords == null)
            return 0f;

        float boost = 0;
        var title = job.Title ?? "";
        var desc = job.Description ?? "";

        foreach (var rule in profile.MatchedKeywords)
        {
            if (string.IsNullOrWhiteSpace(rule.Keyword)) continue;

            bool inTitle = rule.Scope is KeywordScope.Both or KeywordScope.TitleOnly
                && title.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase);
            bool inDesc = rule.Scope is KeywordScope.Both or KeywordScope.DescriptionOnly
                && desc.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase);

            if (inTitle) boost += 0.2f;
            else if (inDesc) boost += 0.1f;
        }

        return MathF.Min(boost, 0.5f);
    }

    private static float SelectedTagBoost(Posting job, List<Tag> jobTags, JobProfile profile)
    {
        if (profile.SelectedTags == null || profile.SelectedTags.Count == 0)
            return 0f;

        float boost = 0;
        var selectedIds = profile.SelectedTags.Select(t => t.Id).ToHashSet();

        foreach (var tag in jobTags)
        {
            if (!selectedIds.Contains(tag.Id)) continue;

            if (job.Title.Contains(tag.Name, StringComparison.OrdinalIgnoreCase))
                boost += 0.2f;
            else if (job.Description.Contains(tag.Name, StringComparison.OrdinalIgnoreCase))
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

    private static bool ContainsSelectedTag(List<Tag> jobTags, JobProfile profile)
    {
        if (profile.SelectedTags == null || profile.SelectedTags.Count == 0)
            return true;

        var selectedIds = profile.SelectedTags
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
