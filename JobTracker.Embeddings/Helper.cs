using System.Numerics;
using System.Runtime.CompilerServices;

namespace JobTracker.Embeddings;

public static class Helper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ToBytes(float[] vector)
    {
        byte[] buffer = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, buffer, 0, buffer.Length);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DotProductSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Length mismatch");

        float sum = 0f;
        int i = 0;
        int len = a.Length;

        if (Vector.IsHardwareAccelerated && len >= Vector<float>.Count)
        {
            for (; i <= len - Vector<float>.Count; i += Vector<float>.Count)
            {
                var va = new Vector<float>(a, i);
                var vb = new Vector<float>(b, i);
                sum += Vector.Dot(va, vb);
            }
        }

        for (; i < len; i++)
        {
            sum += a[i] * b[i];
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[] Normalize(float[] vec)
    {
        var len = Math.Sqrt(vec.Select(x => x * x).Sum());
        if (len < 1e-12) return vec;

        var normalized = new float[vec.Length];
        for (int i = 0; i < vec.Length; i++)
            normalized[i] = (float)(vec[i] / len);

        return normalized;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(float[] a, float[] b)
    {
        float sum = 0;
        for (int i = 0; i < a.Length; i++)
            sum += a[i] * b[i];

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[] FromBytes(byte[] blob)
    {
        var floats = new float[blob.Length / sizeof(float)];
        Buffer.BlockCopy(blob, 0, floats, 0, blob.Length);
        return floats;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[] MeanPool(ReadOnlyMemory<ReadOnlyMemory<float>> vecs)
    {
        int dim = vecs.Span[0].Length;
        var sum = new float[dim];

        foreach (var v in vecs.Span)
        {
            var vSpan = v.Span;
            for (int i = 0; i < dim; i++)
                sum[i] += vSpan[i];
        }

        float inv = 1f / vecs.Length;
        for (int i = 0; i < dim; i++)
            sum[i] *= inv;

        // Optional: re-normalize
        float norm = MathF.Sqrt(sum.Select(x => x * x).Sum());
        if (norm > 1e-6f)
            for (int i = 0; i < dim; i++)
                sum[i] /= norm;

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[] MeanPool(ReadOnlyMemory<float>[] vecsArray)
    {
        return MeanPool(vecsArray.AsMemory());
    }
}
