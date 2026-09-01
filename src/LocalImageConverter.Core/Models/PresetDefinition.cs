namespace LocalImageConverter.Core.Models;

public record PresetDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string TargetFormatId { get; init; }
    public int Quality { get; init; } = 85;
    public ResizeMode ResizeMode { get; init; } = ResizeMode.KeepOriginal;
    public int MaxDimension { get; init; } = 1920;
    public MetadataOption MetadataOption { get; init; } = MetadataOption.KeepAll;
    public bool Lossless { get; init; } = false;

    public static IReadOnlyList<PresetDefinition> Defaults => new List<PresetDefinition>
    {
        new()
        {
            Id = "web_balanced",
            Name = "Web Equilibrado",
            Description = "WebP calidad 82, máx 1920px, metadata limpia",
            TargetFormatId = "webp",
            Quality = 82,
            ResizeMode = ResizeMode.MaxDimension,
            MaxDimension = 1920,
            MetadataOption = MetadataOption.StripPrivateGpsExif
        },
        new()
        {
            Id = "jpeg_hq",
            Name = "JPEG Alta Calidad",
            Description = "JPEG calidad 92, resolución original",
            TargetFormatId = "jpg",
            Quality = 92,
            ResizeMode = ResizeMode.KeepOriginal,
            MetadataOption = MetadataOption.KeepAll
        },
        new()
        {
            Id = "png_lossless",
            Name = "PNG Sin Pérdida",
            Description = "PNG transparente/lossless, resolución original",
            TargetFormatId = "png",
            Quality = 100,
            ResizeMode = ResizeMode.KeepOriginal,
            MetadataOption = MetadataOption.KeepAll,
            Lossless = true
        },
        new()
        {
            Id = "reduce_size",
            Name = "Reducir Tamaño",
            Description = "WebP calidad 75, máx 1600px, sin metadatos",
            TargetFormatId = "webp",
            Quality = 75,
            ResizeMode = ResizeMode.MaxDimension,
            MaxDimension = 1600,
            MetadataOption = MetadataOption.StripAll
        },
        new()
        {
            Id = "keep_original",
            Name = "Mantener Original",
            Description = "Solo cambiar formato, sin redimensionar ni alterar",
            TargetFormatId = "webp",
            Quality = 90,
            ResizeMode = ResizeMode.KeepOriginal,
            MetadataOption = MetadataOption.KeepAll
        }
    };
}
