namespace DocuMind.IntegrationTests;

/// <summary>
/// Shares a single <see cref="DocuMindApiFactory"/> (one host, one background indexing worker,
/// one storage directory) across every test class. Without this, xUnit's default parallel
/// test-class execution would spin up a separate factory per class — all pointed at the same
/// "documind_test" database — and their background workers would race to claim the same queued
/// documents while each only has its own factory's storage directory, causing spurious
/// FileNotFoundException-driven failures.
/// </summary>
[CollectionDefinition(Name)]
public class DocuMindApiCollection : ICollectionFixture<DocuMindApiFactory>
{
    public const string Name = "DocuMindApi";
}
