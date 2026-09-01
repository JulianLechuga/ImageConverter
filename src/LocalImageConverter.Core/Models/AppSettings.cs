namespace LocalImageConverter.Core.Models;

public class AppSettings
{
    public string TargetFormatId { get; set; } = "webp";
    public int Quality { get; set; } = 85;
    public ResizeMode ResizeMode { get; set; } = ResizeMode.KeepOriginal;
    public int MaxDimension { get; set; } = 1920;
    public bool KeepAspectRatio { get; set; } = true;
    public MetadataOption MetadataOption { get; set; } = MetadataOption.KeepAll;
    public AlphaBackgroundColor AlphaBackground { get; set; } = AlphaBackgroundColor.White;
    public string CustomAlphaBackgroundHex { get; set; } = "#FFFFFF";
    public OutputDirectoryMode OutputDirectoryMode { get; set; } = OutputDirectoryMode.ConvertedSubfolder;
    public string? CustomOutputDirectory { get; set; }
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.AutoRename;
    public int MaxConcurrency { get; set; } = Math.Max(1, Math.Min(4, Environment.ProcessorCount));
    public bool AutoOrient { get; set; } = true;
    public bool ScanSubfolders { get; set; } = true;
    public string Theme { get; set; } = "Dark"; // "Dark" or "Light"
    public string Language { get; set; } = "es";
}
