using DocuMind.Application.Documents;

namespace DocuMind.Tests.Documents;

public class TextChunkerTests
{
    private readonly TextChunker _chunker = new();

    [Fact]
    public void Chunk_WithShortSingleSentencePage_ProducesOneChunk()
    {
        var pages = new[] { new PageText(1, "This is a short sentence.") };
        var options = new ChunkingOptions { ChunkSizeChars = 1000, ChunkOverlapChars = 100 };

        var chunks = _chunker.Chunk(pages, options);

        var chunk = Assert.Single(chunks);
        Assert.Equal("This is a short sentence.", chunk.Text);
        Assert.Equal(1, chunk.PageStart);
        Assert.Equal(1, chunk.PageEnd);
        Assert.Equal(0, chunk.ChunkIndex);
    }

    [Fact]
    public void Chunk_NeverExceedsConfiguredSizeExceptForASingleOversizedSentence()
    {
        var text = string.Concat(Enumerable.Repeat("This is sentence number filler. ", 50));
        var pages = new[] { new PageText(1, text) };
        var options = new ChunkingOptions { ChunkSizeChars = 100, ChunkOverlapChars = 20 };

        var chunks = _chunker.Chunk(pages, options);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Text.Length <= options.ChunkSizeChars + 1));
    }

    [Fact]
    public void Chunk_DoesNotSplitMidSentence()
    {
        var pages = new[]
        {
            new PageText(1, "Alpha sentence one. Beta sentence two. Gamma sentence three. Delta sentence four."),
        };
        var options = new ChunkingOptions { ChunkSizeChars = 40, ChunkOverlapChars = 0 };

        var chunks = _chunker.Chunk(pages, options);

        foreach (var chunk in chunks)
        {
            var trimmed = chunk.Text.Trim();
            Assert.True(
                trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?'),
                $"Chunk did not end on a sentence boundary: \"{trimmed}\"");
        }
    }

    [Fact]
    public void Chunk_SpanningTwoPages_RecordsPageStartAndPageEndCorrectly()
    {
        var pages = new[]
        {
            new PageText(1, "First page content here, a reasonably long sentence to fill space."),
            new PageText(2, "Second page content continues the same chunk, also fairly long."),
        };
        // Large enough that both pages' single sentence fit into one chunk.
        var options = new ChunkingOptions { ChunkSizeChars = 500, ChunkOverlapChars = 0 };

        var chunks = _chunker.Chunk(pages, options);

        var chunk = Assert.Single(chunks);
        Assert.Equal(1, chunk.PageStart);
        Assert.Equal(2, chunk.PageEnd);
        Assert.Contains("First page", chunk.Text);
        Assert.Contains("Second page", chunk.Text);
    }

    [Fact]
    public void Chunk_WhenForcedToSplitAcrossPages_EachChunkReportsOnlyThePagesItActuallyContains()
    {
        var pages = new[]
        {
            new PageText(1, "Page one has this single long sentence that takes up most of the budget here."),
            new PageText(2, "Page two has a different sentence that will not fit in the first chunk at all."),
        };
        var options = new ChunkingOptions { ChunkSizeChars = 85, ChunkOverlapChars = 0 };

        var chunks = _chunker.Chunk(pages, options);

        Assert.True(chunks.Count >= 2);
        Assert.Equal(1, chunks[0].PageStart);
        Assert.Equal(1, chunks[0].PageEnd);
        Assert.Equal(2, chunks[^1].PageStart);
        Assert.Equal(2, chunks[^1].PageEnd);
    }

    [Fact]
    public void Chunk_CarriesOverlapTextIntoTheNextChunk()
    {
        var pages = new[]
        {
            new PageText(1, "Sentence one is here. Sentence two is here. Sentence three is here. Sentence four is here."),
        };
        var options = new ChunkingOptions { ChunkSizeChars = 45, ChunkOverlapChars = 25 };

        var chunks = _chunker.Chunk(pages, options);

        Assert.True(chunks.Count > 1);
        for (var i = 1; i < chunks.Count; i++)
        {
            var previousTail = chunks[i - 1].Text.Split('.', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
            Assert.Contains(previousTail, chunks[i].Text);
        }
    }

    [Fact]
    public void Chunk_WithZeroOverlap_DoesNotRepeatAnySentence()
    {
        var pages = new[]
        {
            new PageText(1, "Sentence one is here. Sentence two is here. Sentence three is here. Sentence four is here."),
        };
        var options = new ChunkingOptions { ChunkSizeChars = 25, ChunkOverlapChars = 0 };

        var chunks = _chunker.Chunk(pages, options);

        var allText = string.Join(" ", chunks.Select(c => c.Text));
        Assert.Equal(1, CountOccurrences(allText, "Sentence one"));
        Assert.Equal(1, CountOccurrences(allText, "Sentence four"));
    }

    [Fact]
    public void Chunk_SkipsBlankPages()
    {
        var pages = new[]
        {
            new PageText(1, "Real content on page one."),
            new PageText(2, "   "),
            new PageText(3, "Real content on page three."),
        };
        var options = new ChunkingOptions { ChunkSizeChars = 1000, ChunkOverlapChars = 0 };

        var chunks = _chunker.Chunk(pages, options);

        Assert.DoesNotContain(chunks, c => c.PageStart == 2 || c.PageEnd == 2);
    }

    [Fact]
    public void Chunk_WithNoPages_ReturnsNoChunks()
    {
        var chunks = _chunker.Chunk([], new ChunkingOptions());

        Assert.Empty(chunks);
    }

    [Fact]
    public void Chunk_SingleSentenceLongerThanChunkSize_IsHardSplitWithoutLosingText()
    {
        var longSentence = new string('x', 250) + ".";
        var pages = new[] { new PageText(1, longSentence) };
        var options = new ChunkingOptions { ChunkSizeChars = 100, ChunkOverlapChars = 10 };

        var chunks = _chunker.Chunk(pages, options);

        Assert.True(chunks.Count >= 3);
        Assert.All(chunks, c => Assert.Equal(1, c.PageStart));
        Assert.Equal(longSentence.Length, chunks.Sum(c => c.Text.Length));
    }

    [Fact]
    public void Chunk_ChunkIndexesAreSequentialStartingAtZero()
    {
        var pages = new[]
        {
            new PageText(1, "One sentence here. Two sentence here. Three sentence here. Four sentence here."),
        };
        var options = new ChunkingOptions { ChunkSizeChars = 30, ChunkOverlapChars = 0 };

        var chunks = _chunker.Chunk(pages, options);

        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(c => c.ChunkIndex));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
