using DocuMind.Application.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.BackgroundJobs;

/// <summary>
/// Polls for documents in the Queued state and indexes them with bounded concurrency. A
/// database-backed queue (rather than a message broker) is sufficient at this project's scale —
/// see the project README for that trade-off. Not safe for multiple API instances running this
/// service concurrently (no cross-instance claim locking); fine for a single instance.
/// </summary>
public class DocumentIndexingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<IndexingOptions> options,
    ILogger<DocumentIndexingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Document indexing batch failed unexpectedly");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        List<Guid> documentIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var queued = await repository.GetQueuedForProcessingAsync(options.Value.BatchSize, ct);
            if (queued.Count == 0) return;

            documentIds = queued.Select(d => d.Id).ToList();
        }

        using var semaphore = new SemaphoreSlim(Math.Max(1, options.Value.MaxConcurrency));
        var tasks = documentIds.Select(id => ProcessOneAsync(id, semaphore, ct));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessOneAsync(Guid documentId, SemaphoreSlim semaphore, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var indexer = scope.ServiceProvider.GetRequiredService<IDocumentIndexer>();
            await indexer.IndexAsync(documentId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error indexing document {DocumentId}", documentId);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
