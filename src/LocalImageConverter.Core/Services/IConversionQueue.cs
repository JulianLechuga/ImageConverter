using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public record QueueProgressReport(
    int TotalItems,
    int CompletedItems,
    int ErrorItems,
    int CancelledItems,
    int CurrentItemIndex,
    string CurrentFileName,
    double ProgressPercentage,
    long TotalOriginalBytes,
    long TotalConvertedBytes,
    long TotalSavedBytes,
    double SavingsPercentage
);

public interface IConversionQueue
{
    event EventHandler<QueueProgressReport>? ProgressChanged;
    event EventHandler<ConversionResult>? ItemCompleted;
    event EventHandler<ConversionResult>? ItemFailed;

    bool IsRunning { get; }
    Task<IReadOnlyList<ConversionResult>> ExecuteQueueAsync(
        IReadOnlyList<ImageFileInfo> items,
        ConversionOptions options,
        CancellationToken cancellationToken = default);

    void Cancel();
}
