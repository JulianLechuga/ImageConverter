using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public class ConversionQueue : IConversionQueue
{
    private readonly IImageConverter _converter;
    private readonly ILoggerService? _logger;
    private CancellationTokenSource? _currentCts;

    public event EventHandler<QueueProgressReport>? ProgressChanged;
    public event EventHandler<ConversionResult>? ItemCompleted;
    public event EventHandler<ConversionResult>? ItemFailed;

    public bool IsRunning { get; private set; }

    public ConversionQueue(IImageConverter converter, ILoggerService? logger = null)
    {
        _converter = converter;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ConversionResult>> ExecuteQueueAsync(
        IReadOnlyList<ImageFileInfo> items,
        ConversionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) return Array.Empty<ConversionResult>();

        IsRunning = true;
        _currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _currentCts.Token;

        var concurrency = Math.Clamp(options.MaxConcurrency, 1, 16);
        using var semaphore = new SemaphoreSlim(concurrency, concurrency);

        var results = new List<ConversionResult>();
        var resultsLock = new object();

        var totalItems = items.Count;
        var completedCount = 0;
        var errorCount = 0;
        var cancelledCount = 0;

        long totalOriginalBytes = 0;
        long totalConvertedBytes = 0;

        // Reset waiting items
        foreach (var item in items)
        {
            item.Status = ItemStatus.Waiting;
            item.ErrorMessage = null;
            item.ConvertedFilePath = null;
            item.ConvertedFileSizeBytes = null;
        }

        var tasks = items.Select(async (item, index) =>
        {
            if (token.IsCancellationRequested)
            {
                item.Status = ItemStatus.Cancelled;
                Interlocked.Increment(ref cancelledCount);
                return;
            }

            try
            {
                await semaphore.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                item.Status = ItemStatus.Cancelled;
                Interlocked.Increment(ref cancelledCount);
                return;
            }

            try
            {
                if (token.IsCancellationRequested)
                {
                    item.Status = ItemStatus.Cancelled;
                    Interlocked.Increment(ref cancelledCount);
                    return;
                }

                item.Status = ItemStatus.Processing;
                ReportProgress(totalItems, completedCount, errorCount, cancelledCount, index + 1, item.FileName, totalOriginalBytes, totalConvertedBytes);

                var result = await _converter.ConvertAsync(item, options, token).ConfigureAwait(false);

                lock (resultsLock)
                {
                    results.Add(result);
                }

                if (result.Success)
                {
                    item.Status = ItemStatus.Completed;
                    item.ConvertedFilePath = result.DestinationFilePath;
                    item.ConvertedFileSizeBytes = result.ConvertedBytes;
                    item.DurationMs = result.DurationMs;

                    Interlocked.Increment(ref completedCount);
                    Interlocked.Add(ref totalOriginalBytes, result.OriginalBytes);
                    Interlocked.Add(ref totalConvertedBytes, result.ConvertedBytes);

                    ItemCompleted?.Invoke(this, result);
                }
                else
                {
                    if (token.IsCancellationRequested)
                    {
                        item.Status = ItemStatus.Cancelled;
                        Interlocked.Increment(ref cancelledCount);
                    }
                    else
                    {
                        item.Status = ItemStatus.Error;
                        item.ErrorMessage = result.ErrorMessage;
                        Interlocked.Increment(ref errorCount);
                        ItemFailed?.Invoke(this, result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                item.Status = ItemStatus.Cancelled;
                Interlocked.Increment(ref cancelledCount);
            }
            catch (Exception ex)
            {
                item.Status = ItemStatus.Error;
                item.ErrorMessage = ex.Message;
                Interlocked.Increment(ref errorCount);
                _logger?.LogError($"Unhandled error in conversion queue for {item.FileName}", ex);
            }
            finally
            {
                try
                {
                    semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if disposed during teardown
                }
                ReportProgress(totalItems, completedCount, errorCount, cancelledCount, index + 1, item.FileName, totalOriginalBytes, totalConvertedBytes);
            }
        });

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            IsRunning = false;
            _currentCts?.Dispose();
            _currentCts = null;
        }

        return results;
    }

    public void Cancel()
    {
        _currentCts?.Cancel();
    }

    private void ReportProgress(
        int total,
        int completed,
        int errors,
        int cancelled,
        int currentIndex,
        string currentFile,
        long origBytes,
        long convBytes)
    {
        var processed = completed + errors + cancelled;
        var percentage = total > 0 ? (double)processed / total * 100.0 : 0;
        var savedBytes = Math.Max(0, origBytes - convBytes);
        var savingsPct = origBytes > 0 ? Math.Round((double)savedBytes / origBytes * 100.0, 1) : 0;

        var report = new QueueProgressReport(
            TotalItems: total,
            CompletedItems: completed,
            ErrorItems: errors,
            CancelledItems: cancelled,
            CurrentItemIndex: currentIndex,
            CurrentFileName: currentFile,
            ProgressPercentage: percentage,
            TotalOriginalBytes: origBytes,
            TotalConvertedBytes: convBytes,
            TotalSavedBytes: savedBytes,
            SavingsPercentage: savingsPct
        );

        ProgressChanged?.Invoke(this, report);
    }
}
