namespace DocuMind.Application.Documents;

public interface ITextChunker
{
    IReadOnlyList<DraftChunk> Chunk(IReadOnlyList<PageText> pages, ChunkingOptions options);
}
