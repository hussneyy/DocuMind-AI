namespace DocuMind.Application.Documents;

public record DocumentChunkDto(Guid Id, int ChunkIndex, string TextContent, int PageStart, int PageEnd);
