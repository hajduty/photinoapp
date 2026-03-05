using JobTracker.Application.Features.Embeddings;

namespace JobTracker.Application.Infrastructure.Services;

public static class JobSentenceExtractor
{
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "e.g", "i.e", "etc", "vs", "mr", "mrs", "ms", "dr", "prof", "sr", "jr",
        "dept", "approx", "incl", "excl", "est", "fig", "no", "vol", "p"
    };

    public static List<JobSentenceDto> Extract(string text)
    {
        var result = new List<JobSentenceDto>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        int start = 0, id = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '.' && IsSentenceEndDot(text, i))
                continue;

            if (c is not ('.' or '!' or '?' or '\n'))
                continue;

            string rawSentence = text.Substring(start, i - start + 1);
            string trimmedSentence = rawSentence.Trim();

            if (!string.IsNullOrWhiteSpace(trimmedSentence))
            {
                int actualStart = start;
                while (actualStart < text.Length && char.IsWhiteSpace(text[actualStart]))
                    actualStart++;

                result.Add(new JobSentenceDto
                {
                    Id = id++,
                    Start = actualStart,
                    Length = trimmedSentence.Length,
                    Sentence = trimmedSentence,
                    SentenceType = null
                });
            }

            start = i + 1;
        }

        if (start < text.Length)
        {
            string rawTrailing = text[start..];
            string trimmedTrailing = rawTrailing.Trim();

            if (trimmedTrailing.Length > 20)
            {
                int actualStart = start;
                while (actualStart < text.Length && char.IsWhiteSpace(text[actualStart]))
                    actualStart++;

                result.Add(new JobSentenceDto
                {
                    Id = id++,
                    Start = actualStart,
                    Length = trimmedTrailing.Length,
                    Sentence = trimmedTrailing
                });
            }
        }

        return result;
    }

    private static bool IsSentenceEndDot(string text, int i)
    {
        if (i + 1 < text.Length && !char.IsWhiteSpace(text[i + 1]))
            return true;

        int wordStart = i - 1;

        while (wordStart >= 0 && char.IsLetter(text[wordStart]))
            wordStart--;

        int actualWordStart = wordStart + 1;

        if (actualWordStart < i)
        {
            string wordBefore = text[actualWordStart..i].ToLower();
            return Abbreviations.Contains(wordBefore);
        }

        return false;
    }
}
