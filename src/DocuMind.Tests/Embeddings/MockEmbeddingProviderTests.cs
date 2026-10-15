using DocuMind.Application.Embeddings;
using DocuMind.Infrastructure.Embeddings;
using Microsoft.Extensions.Options;

namespace DocuMind.Tests.Embeddings;

public class MockEmbeddingProviderTests
{
    private readonly MockEmbeddingProvider _provider = new(
        Options.Create(new EmbeddingOptions { Dimensions = 64, Model = "mock-bow-v1" }));

    [Fact]
    public async Task EmbedAsync_IsDeterministic_SameTextProducesTheSameVector()
    {
        var first = await _provider.EmbedAsync("The exam deadline is March fifteenth.", CancellationToken.None);
        var second = await _provider.EmbedAsync("The exam deadline is March fifteenth.", CancellationToken.None);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task EmbedAsync_ReturnsAVectorOfTheConfiguredDimensions()
    {
        var vector = await _provider.EmbedAsync("Some text", CancellationToken.None);

        Assert.Equal(64, vector.Length);
    }

    [Fact]
    public async Task EmbedAsync_ProducesANormalizedVector()
    {
        var vector = await _provider.EmbedAsync("Several different words here", CancellationToken.None);

        var magnitude = Math.Sqrt(vector.Sum(v => (double)v * v));

        Assert.True(Math.Abs(magnitude - 1.0) < 0.0001 || magnitude == 0,
            $"Expected a unit vector, got magnitude {magnitude}");
    }

    [Fact]
    public async Task EmbedAsync_TextsSharingVocabulary_AreMoreSimilarThanUnrelatedTexts()
    {
        var examQuestion = await _provider.EmbedAsync("When is the exam registration deadline?", CancellationToken.None);
        var examPage = await _provider.EmbedAsync("The exam registration deadline is March fifteenth.", CancellationToken.None);
        var unrelatedPage = await _provider.EmbedAsync("The cafeteria menu changes every Tuesday and Friday.", CancellationToken.None);

        var similarityToExamPage = CosineSimilarity(examQuestion, examPage);
        var similarityToUnrelatedPage = CosineSimilarity(examQuestion, unrelatedPage);

        Assert.True(similarityToExamPage > similarityToUnrelatedPage,
            $"Expected shared-vocabulary similarity ({similarityToExamPage}) > unrelated similarity ({similarityToUnrelatedPage})");
    }

    [Fact]
    public async Task EmbedBatchAsync_ProducesTheSameVectorsAsIndividualCalls()
    {
        string[] texts = ["First text", "Second text", "Third text"];

        var batch = await _provider.EmbedBatchAsync(texts, CancellationToken.None);
        var individual = await Task.WhenAll(texts.Select(t => _provider.EmbedAsync(t, CancellationToken.None)));

        for (var i = 0; i < texts.Length; i++)
        {
            Assert.Equal(individual[i], batch[i]);
        }
    }

    [Fact]
    public void ModelId_ReflectsTheConfiguredModel()
    {
        Assert.Equal("mock-bow-v1", _provider.ModelId);
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
