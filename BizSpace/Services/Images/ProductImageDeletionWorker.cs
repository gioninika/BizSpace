using BizSpace.Data;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Services.Images;

public sealed class ProductImageDeletionProcessor(
    StoreDbContext db,
    IProductImageStorage storage,
    ILogger<ProductImageDeletionProcessor> logger)
{
    public async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var jobs = await db.ProductImageDeletionJobs
            .OrderBy(x => x.CreatedAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            try
            {
                await storage.DeleteAsync(job.PublicId, cancellationToken);
                db.ProductImageDeletionJobs.Remove(job);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (ImageStorageNotConfiguredException)
            {
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                job.Attempts++;
                job.LastAttemptAtUtc = DateTime.UtcNow;
                job.LastError = exception.GetType().Name;
                await db.SaveChangesAsync(cancellationToken);
                logger.LogWarning(exception, "Image cleanup job {JobId} failed; it will be retried.", job.Id);
            }
        }
    }
}

public sealed class ProductImageDeletionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProductImageDeletionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ProductImageDeletionProcessor>();
                await processor.ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The product image cleanup pass failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
