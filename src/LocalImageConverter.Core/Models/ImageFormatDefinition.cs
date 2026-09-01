using ImageMagick;

namespace LocalImageConverter.Core.Models;

public record ImageFormatDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string DefaultExtension { get; init; }
    public required IReadOnlyList<string> Extensions { get; init; }
    public required MagickFormat MagickFormat { get; init; }
    public bool SupportsAlpha { get; init; }
    public bool SupportsLossyQuality { get; init; }
    public bool SupportsLossless { get; init; }
    public int DefaultQuality { get; init; } = 90;
    public bool IsAnimated { get; init; }
    public string Description { get; init; } = string.Empty;
}
