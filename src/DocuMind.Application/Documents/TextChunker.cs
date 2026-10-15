using System.Text.RegularExpressions;

namespace DocuMind.Application.Documents;

/// <summary>
/// Greedily packs whole sentences into chunks up to a target size, carrying trailing sentences
/// from one chunk into the start of the next for retrieval context continuity. A chunk's page
/// range is the min/max page of the sentences it contains, so citations stay correct even when a
/// chunk spans a page boundary. A single sentence longer than the chunk size (no punctuation to
/// split on) is hard-split by character length so no chunk grows unbounded.
/// </summary>
public partial class TextChunker : ITextChunker
{
    public IReadOnlyList<DraftChunk> Chunk(IReadOnlyList<PageText> pages, ChunkingOptions options)
    {
        var units = BuildUnits(pages);
        var chunks = new List<DraftChunk>();
        var current = new List<(string Text, int Page)>();
        var nextIndex = 0;

        void Flush()
        {
            if (current.Count == 0) return;
            chunks.Add(new DraftChunk(
                nextIndex++,
                string.Join(" ", current.Select(u => u.Text)),
                current.Min(u => u.Page),
                current.Max(u => u.Page)));
        }

        List<(string Text, int Page)> TakeOverlapTail(List<(string Text, int Page)> source)
        {
            var tail = new List<(string Text, int Page)>();
            if (options.ChunkOverlapChars <= 0) return tail;

            for (var i = source.Count - 1; i >= 0; i--)
            {
                var candidate = new List<(string Text, int Page)> { source[i] };
                candidate.AddRange(tail);
                if (tail.Count > 0 && LengthOf(candidate) > options.ChunkOverlapChars) break;
                tail = candidate;
            }

            return tail;
        }

        foreach (var unit in units)
        {
            if (unit.Text.Length > options.ChunkSizeChars)
            {
                Flush();
                current = [];
                foreach (var piece in HardSplit(unit.Text, options.ChunkSizeChars))
                {
                    chunks.Add(new DraftChunk(nextIndex++, piece, unit.Page, unit.Page));
                }

                continue;
            }

            var prospective = new List<(string Text, int Page)>(current) { unit };
            if (current.Count > 0 && LengthOf(prospective) > options.ChunkSizeChars)
            {
                Flush();
                current = TakeOverlapTail(current);
                prospective = new List<(string Text, int Page)>(current) { unit };
            }

            current = prospective;
        }

        Flush();
        return chunks;
    }

    private static int LengthOf(List<(string Text, int Page)> list) =>
        list.Count == 0 ? 0 : list.Sum(u => u.Text.Length) + (list.Count - 1);

    private static List<(string Text, int Page)> BuildUnits(IReadOnlyList<PageText> pages)
    {
        var units = new List<(string, int)>();
        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text)) continue;

            var normalized = WhitespaceRegex().Replace(page.Text, " ").Trim();
            foreach (var sentence in SentenceSplitRegex().Split(normalized))
            {
                var trimmed = sentence.Trim();
                if (trimmed.Length > 0) units.Add((trimmed, page.PageNumber));
            }
        }

        return units;
    }

    private static IEnumerable<string> HardSplit(string text, int maxLength)
    {
        for (var i = 0; i < text.Length; i += maxLength)
        {
            yield return text.Substring(i, Math.Min(maxLength, text.Length - i));
        }
    }

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceSplitRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
