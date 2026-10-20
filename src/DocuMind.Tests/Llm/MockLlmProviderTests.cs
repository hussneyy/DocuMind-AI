using DocuMind.Application.Llm;
using DocuMind.Infrastructure.Llm;
using Microsoft.Extensions.Options;

namespace DocuMind.Tests.Llm;

public class MockLlmProviderTests
{
    private readonly MockLlmProvider _provider = new(Options.Create(new LlmOptions { Model = "mock-grounded-v1" }));

    [Fact]
    public async Task GenerateAnswerAsync_WithNoContext_RefusesRatherThanGuessing()
    {
        var request = new LlmRequest("What is the deadline?", [], []);

        var answer = await _provider.GenerateAnswerAsync(request, CancellationToken.None);

        Assert.Contains("don't have enough information", answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithRelevantContext_CitesTheBlockItDrewFrom()
    {
        var context = new List<LlmContextBlock>
        {
            new(1, Guid.NewGuid(), Guid.NewGuid(), "handbook.pdf", 2, 2, "The exam registration deadline is March fifteenth. Other unrelated sentence here."),
        };
        var request = new LlmRequest("When is the exam registration deadline?", context, []);

        var answer = await _provider.GenerateAnswerAsync(request, CancellationToken.None);

        Assert.Contains("[1]", answer);
        Assert.Contains("March fifteenth", answer);
    }

    [Fact]
    public async Task GenerateAnswerAsync_PicksTheMoreRelevantSentenceWithinABlock_NotJustTheFirstOne()
    {
        var context = new List<LlmContextBlock>
        {
            new(1, Guid.NewGuid(), Guid.NewGuid(), "handbook.pdf", 1, 1,
                "The cafeteria opens at seven. The library closes at midnight on weekdays."),
        };
        var request = new LlmRequest("What time does the library close?", context, []);

        var answer = await _provider.GenerateAnswerAsync(request, CancellationToken.None);

        Assert.Contains("library closes at midnight", answer);
        Assert.DoesNotContain("cafeteria", answer);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithMultipleContextBlocks_CitesEachOneItUses()
    {
        var context = new List<LlmContextBlock>
        {
            new(1, Guid.NewGuid(), Guid.NewGuid(), "a.pdf", 1, 1, "Registration opens on March first."),
            new(2, Guid.NewGuid(), Guid.NewGuid(), "b.pdf", 3, 3, "Registration closes on March fifteenth."),
        };
        var request = new LlmRequest("When does registration open and close?", context, []);

        var answer = await _provider.GenerateAnswerAsync(request, CancellationToken.None);

        Assert.Contains("[1]", answer);
        Assert.Contains("[2]", answer);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithManyContextBlocks_CitesEveryOneOfThem()
    {
        // Summary generation can pass dozens of chunks (one per document section); the mock must
        // not silently drop any of them the way an earlier, Q&A-only cap on cited blocks would have.
        var context = Enumerable.Range(1, 12)
            .Select(i => new LlmContextBlock(i, Guid.NewGuid(), Guid.NewGuid(), "doc.pdf", i, i, $"Section {i} covers topic {i}."))
            .ToList();
        var request = new LlmRequest("Summarize this document.", context, []);

        var answer = await _provider.GenerateAnswerAsync(request, CancellationToken.None);

        for (var i = 1; i <= 12; i++)
        {
            Assert.Contains($"[{i}]", answer);
        }
    }

    [Fact]
    public void ModelId_ReflectsTheConfiguredModel()
    {
        Assert.Equal("mock-grounded-v1", _provider.ModelId);
    }
}
