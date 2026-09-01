using ImageMagick;
using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public class ImageScanner : IImageScanner
{
    private readonly IImageFormatCatalog _catalog;
    private readonly ILoggerService? _logger;

    // Safety limits against image bombs
    private const uint MaxSafeDimension = 45000;
    private const long MaxSafeFileSizeBytes = 500L * 1024 * 1024; // 500 MB

    public ImageScanner(IImageFormatCatalog catalog, ILoggerService? logger = null)
    {
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<List<ImageFileInfo>> ScanPathsAsync(
        IEnumerable<string> paths,
        bool recursiveSubfolders = true,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var discoveredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultList = new List<ImageFileInfo>();

        await Task.Run(() =>
        {
            foreach (var path in paths)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (string.IsNullOrWhiteSpace(path)) continue;

                try
                {
                    if (File.Exists(path))
                    {
                        var ext = Path.GetExtension(path);
                        if (_catalog.IsExtensionSupported(ext))
                        {
                            discoveredFiles.Add(Path.GetFullPath(path));
                        }
                    }
                    else if (Directory.Exists(path))
                    {
                        var searchOption = recursiveSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                        var supportedExts = _catalog.GetAllSupportedExtensions();

                        try
                        {
                            var filesInDir = Directory.EnumerateFiles(path, "*.*", searchOption);
                            foreach (var f in filesInDir)
                            {
                                if (cancellationToken.IsCancellationRequested) break;
                                var ext = Path.GetExtension(f);
                                if (_catalog.IsExtensionSupported(ext))
                                {
                                    discoveredFiles.Add(Path.GetFullPath(f));
                                }
                            }
                        }
                        catch (UnauthorizedAccessException uex)
                        {
                            _logger?.LogWarning($"Permission denied scanning directory: {path} - {uex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error checking path: {path}", ex);
                }
            }
        }, cancellationToken);

        var count = 0;
        foreach (var file in discoveredFiles)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var info = await InspectFileAsync(file, cancellationToken);
            if (info != null)
            {
                resultList.Add(info);
            }

            count++;
            progress?.Report(count);
        }

        return resultList;
    }

    public async Task<ImageFileInfo?> InspectFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0 || fileInfo.Length > MaxSafeFileSizeBytes)
                {
                    _logger?.LogWarning($"Skipping file {fileInfo.Name}: size ({fileInfo.Length} bytes) is outside safe boundaries.");
                    return null;
                }

                var item = new ImageFileInfo
                {
                    FilePath = fileInfo.FullName,
                    FileName = fileInfo.Name,
                    FileExtension = fileInfo.Extension.ToLowerInvariant(),
                    FileSizeBytes = fileInfo.Length
                };

                try
                {
                    // Inspect header via MagickImageInfo without decoding all pixels to RAM
                    var magickInfo = new MagickImageInfo(filePath);
                    
                    if (magickInfo.Width > MaxSafeDimension || magickInfo.Height > MaxSafeDimension)
                    {
                        _logger?.LogWarning($"Rejected potential image bomb {fileInfo.Name}: {magickInfo.Width}x{magickInfo.Height}");
                        return null;
                    }

                    item.Width = magickInfo.Width;
                    item.Height = magickInfo.Height;
                    item.DetectedFormat = magickInfo.Format.ToString().ToUpperInvariant();
                    item.Orientation = magickInfo.Orientation.ToString();
                    item.HasAlpha = (magickInfo.ColorSpace == ColorSpace.Transparent ||
                                     item.FileExtension is ".png" or ".webp" or ".gif" or ".ico" or ".tiff" or ".tif" or ".avif");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Could not read header for {fileInfo.Name}: {ex.Message}. Using fallback properties.");
                    item.DetectedFormat = item.FileExtension.TrimStart('.').ToUpperInvariant();
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed inspecting file: {filePath}", ex);
                return null;
            }
        }, cancellationToken);
    }

    public async Task<byte[]?> GenerateThumbnailAsync(string filePath, int maxDimension = 256, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var readSettings = new MagickReadSettings
                {
                    Width = (uint)maxDimension,
                    Height = (uint)maxDimension
                };

                using var image = new MagickImage(filePath, readSettings);
                image.AutoOrient();
                image.Thumbnail((uint)maxDimension, (uint)maxDimension);
                image.Format = MagickFormat.Png;

                return image.ToByteArray();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed generating thumbnail for {filePath}: {ex.Message}");
                return null;
            }
        }, cancellationToken);
    }
}
