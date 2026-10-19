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
    public void ModelId_ReflectsTheConfiguredModel()
    {
        Assert.Equal("mock-grounded-v1", _provider.ModelId);
    }
}
