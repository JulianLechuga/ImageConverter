using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public interface IImageScanner
{
    Task<List<ImageFileInfo>> ScanPathsAsync(
        IEnumerable<string> paths,
        bool recursiveSubfolders = true,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ImageFileInfo?> InspectFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<byte[]?> GenerateThumbnailAsync(string filePath, int maxDimension = 256, CancellationToken cancellationToken = default);
}
