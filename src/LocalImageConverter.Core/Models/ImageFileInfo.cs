namespace LocalImageConverter.Core.Models;

public class ImageFileInfo
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public required string FileExtension { get; set; }
    public long FileSizeBytes { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
    public string DetectedFormat { get; set; } = string.Empty;
    public bool HasAlpha { get; set; }
    public string Orientation { get; set; } = "Normal";
    public int FrameCount { get; set; } = 1;
    public bool IsAnimated => FrameCount > 1;

    public ItemStatus Status { get; set; } = ItemStatus.Waiting;
    public string? ErrorMessage { get; set; }
    public string? ConvertedFilePath { get; set; }
    public long? ConvertedFileSizeBytes { get; set; }
    public long DurationMs { get; set; }

    public double SavingsPercentage
    {
        get
        {
            if (!ConvertedFileSizeBytes.HasValue || FileSizeBytes <= 0) return 0;
            var diff = FileSizeBytes - ConvertedFileSizeBytes.Value;
            return Math.Round((double)diff / FileSizeBytes * 100.0, 1);
        }
    }

    public long SavedBytes => ConvertedFileSizeBytes.HasValue ? Math.Max(0, FileSizeBytes - ConvertedFileSizeBytes.Value) : 0;
}
