namespace LocalImageConverter.Core.Models;

public class ConversionOptions
{
    public ImageFormatDefinition TargetFormat { get; set; } = null!;
    public int Quality { get; set; } = 85;
    public ResizeMode ResizeMode { get; set; } = ResizeMode.KeepOriginal;
    public int MaxDimension { get; set; } = 1920;
    public int? CustomWidth { get; set; }
    public int? CustomHeight { get; set; }
    public bool KeepAspectRatio { get; set; } = true;
    public MetadataOption MetadataOption { get; set; } = MetadataOption.KeepAll;
    public AlphaBackgroundColor AlphaBackground { get; set; } = AlphaBackgroundColor.White;
    public string CustomAlphaBackgroundHex { get; set; } = "#FFFFFF";
    public OutputDirectoryMode OutputDirectoryMode { get; set; } = OutputDirectoryMode.ConvertedSubfolder;
    public string? CustomOutputDirectory { get; set; }
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.AutoRename;
    public int MaxConcurrency { get; set; } = Math.Max(1, Math.Min(4, Environment.ProcessorCount));
    public bool AutoOrient { get; set; } = true;
    public bool Lossless { get; set; } = false;
}
