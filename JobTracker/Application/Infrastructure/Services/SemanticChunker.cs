using JobTracker.Embeddings;
using JobTracker.Embeddings.Services;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace JobTracker.Application.Infrastructure.Services;

public record EmbeddedUnit(
    int Id,
    string Text,
    ReadOnlyMemory<float> Embedding);

public record ChunkResult(
    string Text,
    byte[] EmbeddingBytes,
    IReadOnlyList<int> OriginalUnitIds = null);

public class CvChunk
{
    public string ChunkText { get; set; }
    public byte[] ChunkEmbedding { get; set; }
}

public class SemanticChunker
{
    private readonly SentenceClassifierService _classifier;
    private readonly JinaEmbeddingService _embeddingService;
    private readonly float _defaultThreshold = 0.75f;

    public SemanticChunker(SentenceClassifierService classifier, JinaEmbeddingService embeddingService)
    {
        _classifier = classifier;
        _embeddingService = embeddingService;
    }

    public List<ChunkResult> Chunk(
        IReadOnlyList<EmbeddedUnit> units,
        float? similarityThreshold = null)
    {
        if (units.Count == 0) return new List<ChunkResult>();

        var threshold = similarityThreshold ?? _defaultThreshold;
        var chunks = new List<ChunkResult>();
        var current = new List<EmbeddedUnit>();

        for (int i = 0; i < units.Count; i++)
        {
            current.Add(units[i]);

            if (i == units.Count - 1)
            {
                chunks.Add(BuildChunk(current));
                break;
            }

            var sim = CalculateSimilarity(units[i].Embedding, units[i + 1].Embedding);

            if (sim < threshold)
            {
                chunks.Add(BuildChunk(current));
                current.Clear();
            }
        }

        return chunks;
    }

    private float CalculateSimilarity(ReadOnlyMemory<float> aMem, ReadOnlyMemory<float> bMem)
    {
        var a = aMem.ToArray();
        var b = bMem.ToArray();
        return Helper.Dot(a, b);
    }

    private ChunkResult BuildChunk(List<EmbeddedUnit> group)
    {
        var textBuilder = new StringBuilder();
        for (int i = 0; i < group.Count; i++)
        {
            if (i > 0) textBuilder.Append(" ");
            textBuilder.Append(group[i].Text.Trim());
        }
        var text = textBuilder.ToString();

        var vectors = group.Select(u => u.Embedding).ToArray();

        var pooled = Helper.MeanPool(vectors.AsMemory());

        var pooledBytes = Helper.ToBytes(pooled);

        return new ChunkResult(
            text,
            pooledBytes,
            group.Select(u => u.Id).ToList()
        );
    }

    public List<CvChunk> ChunkCv(string cvText)
    {
        var sentences = JobSentenceExtractor.Extract(cvText);
        var texts = sentences.Select(s => s.Sentence).ToArray();
        var rawEmbeddings = _embeddingService.GenerateEmbeddingsBatch(texts);

        var units = new List<EmbeddedUnit>();
        for (int i = 0; i < sentences.Count; i++)
        {
            units.Add(new EmbeddedUnit(
                i,
                sentences[i].Sentence,
                rawEmbeddings[i].AsMemory()));
        }

        var generic = Chunk(units);

        return generic.Select(gc => new CvChunk
        {
            ChunkText = gc.Text,
            ChunkEmbedding = gc.EmbeddingBytes,
        }).ToList();
    }
}