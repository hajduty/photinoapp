using JobTracker.Application.Features.Embeddings;
using JobTracker.Embeddings;
using System.Text;

namespace JobTracker.Application.Infrastructure.Services;

public class SemanticChunker
{
    private readonly float _defaultThreshold = 0.28f;
    private readonly SentenceClassifierService _sentenceClassifier;

    public SemanticChunker(SentenceClassifierService sentenceClassifier)
    {
        _sentenceClassifier = sentenceClassifier;
    }

    public List<JobChunk> CreateChunks(
        List<JobSentence> sentences,
        int jobId,
        float? similarityThreshold = null)
    {
        if (!sentences.Any()) return new();

        var threshold = similarityThreshold ?? _defaultThreshold;
        var chunks = new List<JobChunk>();
        var current = new List<JobSentence>();

        int chunkStart = sentences[0].Start;

        for (int i = 0; i < sentences.Count; i++)
        {
            current.Add(sentences[i]);

            if (i == sentences.Count - 1)
            {
                chunks.Add(BuildChunk(current, jobId, chunkStart));
                break;
            }

            var sim = CalculateSimilarity(sentences[i].Data, sentences[i + 1].Data);

            if (sim < threshold)
            {
                chunks.Add(BuildChunk(current, jobId, chunkStart));
                current.Clear();
                chunkStart = sentences[i + 1].Start;
            }
        }

        return chunks;
    }

    private float CalculateSimilarity(byte[] aBytes, byte[] bBytes)
    {
        var a = Helper.FromBytes(aBytes);
        var b = Helper.FromBytes(bBytes);
        return Helper.Dot(a, b);
    }

    private JobChunk BuildChunk(List<JobSentence> group, int jobId, int startChar)
    {
        var textBuilder = new StringBuilder();
        for (int i = 0; i < group.Count; i++)
        {
            if (i > 0) textBuilder.Append(' ');
            textBuilder.Append(group[i].Sentence.Trim());
        }

        var text = textBuilder.ToString();

        var vectors = new ReadOnlyMemory<float>[group.Count];
        for (int i = 0; i < group.Count; i++)
        {
            vectors[i] = Helper.FromBytes(group[i].Data);
        }

        var pooled = Helper.MeanPool(vectors);
        var pooledBytes = Helper.ToBytes(pooled);

        int endChar = group[group.Count - 1].Start + group[group.Count - 1].Length;

        var sentenceIds = new List<int>(group.Count);
        for (int i = 0; i < group.Count; i++)
        {
            sentenceIds.Add(group[i].Id);
        }

        return new JobChunk
        {
            JobId = jobId,
            ChunkText = text,
            ChunkEmbedding = pooledBytes,
            StartChar = startChar,
            Length = endChar - startChar,
            SentenceIds = sentenceIds,
            ChunkType = _sentenceClassifier.Classify(pooled)
        };
    }
}