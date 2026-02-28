using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Buffers;
using System.Runtime.CompilerServices;
using Tokenizers.DotNet;

namespace Services;

// DeepSeek cooked here

public class JinaEmbeddingService : IDisposable
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;
    private readonly int _maxLength = 512;
    private readonly int _hiddenSize;
    private readonly bool _useGpu;

    private readonly long[] _inputIdsBuffer;
    private readonly long[] _attentionMaskBuffer;
    private readonly int[] _singleDimensions;

    public JinaEmbeddingService(int maxLength = 512, bool forceCpu = false)
    {
        _maxLength = maxLength;

        string modelPath = "Models/jina-embeddings-v5-text-nano-retrieval/model.onnx";
        string tokenizerJsonPath = "Models/jina-embeddings-v5-text-nano-retrieval/tokenizer.json";

        _tokenizer = new Tokenizer(tokenizerJsonPath);

        var options = new SessionOptions();
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        options.EnableCpuMemArena = true;
        options.EnableMemoryPattern = true;
        options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;

        options.AddSessionConfigEntry("session.intra_op.allow_spinning", "1");
        options.AddSessionConfigEntry("session.inter_op.allow_spinning", "1");
        options.AddSessionConfigEntry("session.set_denormal_as_zero", "1");

        _useGpu = !forceCpu;
        if (_useGpu)
        {
            try
            {
                options.AppendExecutionProvider_DML(0);
                Console.WriteLine("Using GPU (DirectML)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPU initialization failed: {ex.Message}, falling back to CPU");
                options.AppendExecutionProvider_CPU(0);
                _useGpu = false;
            }
        }
        else
        {
            options.AppendExecutionProvider_CPU(0);
            Console.WriteLine("Using CPU");
        }

        _session = new InferenceSession(modelPath, options);

        _hiddenSize = _session.OutputMetadata.TryGetValue("last_hidden_state", out var metadata)
            ? (int)metadata.Dimensions[2]
            : 768; 

        _inputIdsBuffer = new long[_maxLength];
        _attentionMaskBuffer = new long[_maxLength];

        _singleDimensions = new[] { 1, _maxLength };

        Warmup();
    }

    private void Warmup()
    {
        try
        {
            GenerateEmbeddingInternal("warmup");
            Console.WriteLine("Model warmed up successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warmup failed: {ex.Message}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float[] GenerateEmbeddingInternal(string text)
    {
        uint[] encoded = _tokenizer.Encode(text);
        int tokenCount = encoded.Length;
        if (tokenCount == 0)
        {
            throw new InvalidOperationException("Input produced no tokens after tokenization.");
        }

        int effectiveLength = Math.Min(tokenCount, _maxLength);

        // Clear buffers
        Array.Clear(_inputIdsBuffer, 0, _maxLength);
        Array.Clear(_attentionMaskBuffer, 0, _maxLength);

        // Fill buffers
        for (int i = 0; i < effectiveLength; i++)
        {
            _inputIdsBuffer[i] = encoded[i];
            _attentionMaskBuffer[i] = 1;
        }

        // Create tensors with proper dimensions
        var inputTensor = new DenseTensor<long>(_inputIdsBuffer, _singleDimensions);
        var maskTensor = new DenseTensor<long>(_attentionMaskBuffer, _singleDimensions);

        var inputs = new[]
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor)
        };

        using var results = _session.Run(inputs);
        var tokenEmbeddings = results[0].AsTensor<float>();

        return LastTokenPooling(tokenEmbeddings, effectiveLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float[] LastTokenPooling(Tensor<float> embeddings, int effectiveLength)
    {
        float[] result = new float[_hiddenSize];

        int lastIdx = effectiveLength - 1;
        float sumSq = 0f;

        for (int j = 0; j < _hiddenSize; j++)
        {
            float val = embeddings[0, lastIdx, j];
            result[j] = val;
            sumSq += val * val;
        }

        float norm = (float)Math.Sqrt(sumSq);
        if (norm > 1e-12f)
        {
            float invNorm = 1f / norm;
            for (int j = 0; j < _hiddenSize; j++)
            {
                result[j] *= invNorm;
            }
        }

        return result;
    }

    public float[][] GenerateEmbeddingsBatch(string[] texts)
    {
        if (texts == null || texts.Length == 0)
            return Array.Empty<float[]>();

        int count = texts.Length;
        var allEmbeddings = new float[count][];

        int batchSize = _useGpu ? 16 : 4;

        for (int i = 0; i < count; i += batchSize)
        {
            int currentBatchSize = Math.Min(batchSize, count - i);
            ProcessBatch(texts, i, currentBatchSize, allEmbeddings);
        }

        return allEmbeddings;
    }

    private void ProcessBatch(string[] texts, int startIdx, int batchSize, float[][] allEmbeddings)
    {
        long[] flatInputs = new long[batchSize * _maxLength];
        long[] flatMasks = new long[batchSize * _maxLength];
        int[] lengths = new int[batchSize];

        try
        {
            for (int b = 0; b < batchSize; b++)
            {
                string text = "Passage: " + texts[startIdx + b];
                uint[] encoded = _tokenizer.Encode(text);
                int len = Math.Min(encoded.Length, _maxLength);
                lengths[b] = len;

                int offset = b * _maxLength;
                for (int j = 0; j < len; j++)
                {
                    flatInputs[offset + j] = encoded[j];
                    flatMasks[offset + j] = 1;
                }
            }

            var batchInputTensor = new DenseTensor<long>(flatInputs, new[] { batchSize, _maxLength });
            var batchMaskTensor = new DenseTensor<long>(flatMasks, new[] { batchSize, _maxLength });

            var inputs = new[]
            {
                NamedOnnxValue.CreateFromTensor("input_ids", batchInputTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", batchMaskTensor)
            };

            using var results = _session.Run(inputs);
            var allTokenEmbeddings = results[0].AsTensor<float>();

            // Process results
            for (int b = 0; b < batchSize; b++)
            {
                int effectiveLength = lengths[b];
                int lastIdx = effectiveLength - 1;

                float[] embedding = new float[_hiddenSize];
                float sumSq = 0f;

                for (int j = 0; j < _hiddenSize; j++)
                {
                    float val = allTokenEmbeddings[b, lastIdx, j];
                    embedding[j] = val;
                    sumSq += val * val;
                }

                float norm = (float)Math.Sqrt(sumSq);
                if (norm > 1e-12f)
                {
                    float invNorm = 1f / norm;
                    for (int j = 0; j < _hiddenSize; j++)
                    {
                        embedding[j] *= invNorm;
                    }
                }

                allEmbeddings[startIdx + b] = embedding;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing batch: {ex.Message}");
            throw;
        }
    }

    public byte[] GenerateEmbedding(string text)
    {
        float[] embedding = GenerateEmbeddingInternal(text);
        return ToBytes(embedding);
    }

    public float[] GenerateEmbeddingFloat(string text)
    {
        return GenerateEmbeddingInternal(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte[] ToBytes(float[] vector)
    {
        byte[] buffer = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, buffer, 0, buffer.Length);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vector length mismatch");

        float dot = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
        }
        return dot;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}