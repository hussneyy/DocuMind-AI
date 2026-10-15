namespace DocuMind.Application.Documents;

public record DraftChunk(int ChunkIndex, string Text, int PageStart, int PageEnd);
