using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using FastBertTokenizer;
using System.Buffers;

namespace Services;

public class BertService : IDisposable
{
    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;
    private readonly int _maxLength = 384;

    public BertService()
    {
        string modelPath = "Models/sentence-bert-swedish-cased/model.onnx";
        string tokenizerJsonPath = "Models/sentence-bert-swedish-cased/tokenizer.json";

        _tokenizer = new BertTokenizer();
        using var tokenizerStream = File.OpenRead(tokenizerJsonPath);
        _tokenizer.LoadTokenizerJson(tokenizerStream);

        // Setup ONNX session with GPU if available
        var options = new SessionOptions();
        try
        {
            options.AppendExecutionProvider_DML(0);
            Console.WriteLine("Using GPU (DirectML)");
        }
        catch
        {
            Console.WriteLine("Using CPU");
        }

        _session = new InferenceSession(modelPath, options);
    }

    public byte[] GenerateEmbedding(string text)
    {
        var embedding = GenerateEmbeddingInternal(text);
        return ToBytes(embedding);
    }

    public List<byte[]> GenerateEmbeddingsBatch(List<string> texts)
    {
        var results = new List<byte[]>();
        var batchSize = 16;

        for (int i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();
            var embeddings = GenerateEmbeddingsInternal(batch);
            results.AddRange(embeddings.Select(ToBytes));
        }

        return results;
    }

    public float[] GenerateEmbeddingFloat(string text)
    {
        return GenerateEmbeddingInternal(text);
    }

    private float[] GenerateEmbeddingInternal(string text)
    {
        // Tokenize - Encode returns (Memory<long> inputIds, Memory<long> attentionMask, Memory<long> tokenTypeIds)
        var (inputIdsMem, attentionMaskMem, tokenTypeIdsMem) = _tokenizer.Encode(text);

        // Convert Memory<long> to List<long> for manipulation
        var inputIds = inputIdsMem.ToArray().ToList();
        var attentionMask = attentionMaskMem.ToArray().ToList();
        var tokenTypeIds = tokenTypeIdsMem.ToArray().ToList();

        // Pad/truncate
        inputIds = PadOrTruncate(inputIds, _maxLength);
        attentionMask = PadOrTruncate(attentionMask, _maxLength);
        tokenTypeIds = PadOrTruncate(tokenTypeIds, _maxLength);

        // Create tensors
        var inputTensor = new DenseTensor<long>(inputIds.ToArray(), new[] { 1, _maxLength });
        var maskTensor = new DenseTensor<long>(attentionMask.ToArray(), new[] { 1, _maxLength });
        var typeTensor = new DenseTensor<long>(tokenTypeIds.ToArray(), new[] { 1, _maxLength });

        // Run inference
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", typeTensor)
        };

        using var results = _session.Run(inputs);
        var embeddings = results.First().AsTensor<float>();

        // Mean pooling and normalize
        var pooled = MeanPooling(embeddings, attentionMask);
        return Normalize(pooled);
    }

    private List<float[]> GenerateEmbeddingsInternal(List<string> texts)
    {
        if (texts.Count == 0) return new List<float[]>();

        // Tokenize all texts and convert Memory<long> to List<long>
        var tokenized = texts.Select(t =>
        {
            var (inputIdsMem, attentionMaskMem, tokenTypeIdsMem) = _tokenizer.Encode(t);
            return (
                InputIds: inputIdsMem.ToArray().ToList(),
                AttentionMask: attentionMaskMem.ToArray().ToList(),
                TokenTypeIds: tokenTypeIdsMem.ToArray().ToList()
            );
        }).ToList();

        // Pad all to same length
        var paddedInputs = tokenized.Select(t => PadOrTruncate(t.InputIds, _maxLength)).ToList();
        var paddedMasks = tokenized.Select(t => PadOrTruncate(t.AttentionMask, _maxLength)).ToList();
        var paddedTypes = tokenized.Select(t => PadOrTruncate(t.TokenTypeIds, _maxLength)).ToList();

        // Create batched tensors [batch_size, max_length]
        var batchInput = new DenseTensor<long>(paddedInputs.SelectMany(x => x).ToArray(), new[] { texts.Count, _maxLength });
        var batchMask = new DenseTensor<long>(paddedMasks.SelectMany(x => x).ToArray(), new[] { texts.Count, _maxLength });
        var batchType = new DenseTensor<long>(paddedTypes.SelectMany(x => x).ToArray(), new[] { texts.Count, _maxLength });

        // Run inference on entire batch
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", batchInput),
            NamedOnnxValue.CreateFromTensor("attention_mask", batchMask),
            NamedOnnxValue.CreateFromTensor("token_type_ids", batchType)
        };

        using var results = _session.Run(inputs);
        var allEmbeddings = results.First().AsTensor<float>();

        // Extract and pool each item
        var result = new List<float[]>();
        for (int i = 0; i < texts.Count; i++)
        {
            var pooled = MeanPoolingBatch(allEmbeddings, i, paddedMasks[i], _maxLength);
            result.Add(Normalize(pooled));
        }

        return result;
    }

    private List<long> PadOrTruncate(List<long> tokens, int maxLength)
    {
        if (tokens.Count > maxLength)
            return tokens.Take(maxLength).ToList();

        var padded = new List<long>(tokens);
        while (padded.Count < maxLength)
            padded.Add(0);

        return padded;
    }

    public string ExtractSummary(string text, int maxSentences = 5)
    {
        // Split into sentences
        var sentences = text
            .Split(new[] { ". ", ".\n", "! ", "? ", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 20) // skip tiny fragments
            .ToList();

        if (sentences.Count <= maxSentences)
            return string.Join(". ", sentences);

        // Embed all sentences
        var embeddings = GenerateEmbeddingsInternal(sentences);

        // Embed the full text as the "topic" anchor
        var docEmbedding = GenerateEmbeddingInternal(text);

        // Score each sentence by cosine similarity to full document
        var scored = sentences
            .Select((sentence, i) => new
            {
                Sentence = sentence,
                Index = i,
                Score = CosineSimilarity(embeddings[i], docEmbedding)
            })
            .OrderByDescending(x => x.Score)
            .Take(maxSentences)
            .OrderBy(x => x.Index)
            .ToList();

        return string.Join(". ", scored.Select(x => x.Sentence)).Trim();
    }

    public static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0;
        for (int i = 0; i < a.Length; i++)
            dot += a[i] * b[i];
        return dot;
    }

    private float[] MeanPooling(Tensor<float> tokenEmbeddings, List<long> attentionMask)
    {
        int hiddenSize = tokenEmbeddings.Dimensions[2];
        var pooled = new float[hiddenSize];
        int validTokens = 0;

        for (int i = 0; i < attentionMask.Count; i++)
        {
            if (attentionMask[i] == 1)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    pooled[j] += tokenEmbeddings[0, i, j];
                }
                validTokens++;
            }
        }

        for (int j = 0; j < hiddenSize; j++)
            pooled[j] /= validTokens;

        return pooled;
    }

    private float[] MeanPoolingBatch(Tensor<float> allEmbeddings, int itemIndex, List<long> attentionMask, int seqLength)
    {
        int hiddenSize = allEmbeddings.Dimensions[2];
        var pooled = new float[hiddenSize];
        int validTokens = 0;

        for (int i = 0; i < seqLength; i++)
        {
            if (attentionMask[i] == 1)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    pooled[j] += allEmbeddings[itemIndex, i, j];
                }
                validTokens++;
            }
        }

        for (int j = 0; j < hiddenSize; j++)
            pooled[j] /= validTokens;

        return pooled;
    }

    private float[] Normalize(float[] vec)
    {
        var len = Math.Sqrt(vec.Select(x => x * x).Sum());
        if (len < 1e-12) return vec;

        var normalized = new float[vec.Length];
        for (int i = 0; i < vec.Length; i++)
            normalized[i] = (float)(vec[i] / len);

        return normalized;
    }

    private byte[] ToBytes(float[] vec)
    {
        var buffer = new byte[vec.Length * sizeof(float)];
        Buffer.BlockCopy(vec, 0, buffer, 0, buffer.Length);
        return buffer;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}