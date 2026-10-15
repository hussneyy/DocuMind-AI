namespace DocuMind.Application.Documents;

/// <summary>Indexes one already-queued document: extract text, chunk it, persist, update status.</summary>
public interface IDocumentIndexer
{
    Task IndexAsync(Guid documentId, CancellationToken ct);
}
