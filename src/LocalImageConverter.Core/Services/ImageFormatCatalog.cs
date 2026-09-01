using ImageMagick;
using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public class ImageFormatCatalog : IImageFormatCatalog
{
    private static readonly List<ImageFormatDefinition> Formats = new()
    {
        new ImageFormatDefinition
        {
            Id = "webp",
            DisplayName = "WebP (.webp)",
            DefaultExtension = ".webp",
            Extensions = new[] { ".webp" },
            MagickFormat = MagickFormat.WebP,
            SupportsAlpha = true,
            SupportsLossyQuality = true,
            SupportsLossless = true,
            DefaultQuality = 82,
            IsAnimated = true,
            Description = "Formato moderno, alta compresión y excelente fidelidad."
        },
        new ImageFormatDefinition
        {
            Id = "jpg",
            DisplayName = "JPEG (.jpg)",
            DefaultExtension = ".jpg",
            Extensions = new[] { ".jpg", ".jpeg", ".jpe", ".jfif" },
            MagickFormat = MagickFormat.Jpeg,
            SupportsAlpha = false,
            SupportsLossyQuality = true,
            SupportsLossless = false,
            DefaultQuality = 90,
            IsAnimated = false,
            Description = "Formato universal para fotografías. Sin soporte de transparencia."
        },
        new ImageFormatDefinition
        {
            Id = "png",
            DisplayName = "PNG (.png)",
            DefaultExtension = ".png",
            Extensions = new[] { ".png" },
            MagickFormat = MagickFormat.Png,
            SupportsAlpha = true,
            SupportsLossyQuality = false,
            SupportsLossless = true,
            DefaultQuality = 100,
            IsAnimated = false,
            Description = "Formato sin pérdida con soporte total de transparencia."
        },
        new ImageFormatDefinition
        {
            Id = "avif",
            DisplayName = "AVIF (.avif)",
            DefaultExtension = ".avif",
            Extensions = new[] { ".avif" },
            MagickFormat = MagickFormat.Avif,
            SupportsAlpha = true,
            SupportsLossyQuality = true,
            SupportsLossless = true,
            DefaultQuality = 75,
            IsAnimated = false,
            Description = "Formato de compresión de última generación basado en AV1."
        },
        new ImageFormatDefinition
        {
            Id = "bmp",
            DisplayName = "BMP (.bmp)",
            DefaultExtension = ".bmp",
            Extensions = new[] { ".bmp", ".dib" },
            MagickFormat = MagickFormat.Bmp,
            SupportsAlpha = false,
            SupportsLossyQuality = false,
            SupportsLossless = true,
            DefaultQuality = 100,
            IsAnimated = false,
            Description = "Mapa de bits clásico sin compresión de Windows."
        },
        new ImageFormatDefinition
        {
            Id = "tiff",
            DisplayName = "TIFF (.tiff)",
            DefaultExtension = ".tiff",
            Extensions = new[] { ".tiff", ".tif" },
            MagickFormat = MagickFormat.Tiff,
            SupportsAlpha = true,
            SupportsLossyQuality = false,
            SupportsLossless = true,
            DefaultQuality = 100,
            IsAnimated = false,
            Description = "Alta precisión editorial y multipágina."
        },
        new ImageFormatDefinition
        {
            Id = "gif",
            DisplayName = "GIF (.gif)",
            DefaultExtension = ".gif",
            Extensions = new[] { ".gif" },
            MagickFormat = MagickFormat.Gif,
            SupportsAlpha = true,
            SupportsLossyQuality = false,
            SupportsLossless = true,
            DefaultQuality = 100,
            IsAnimated = true,
            Description = "Gráficos con paleta de hasta 256 colores y animaciones."
        },
        new ImageFormatDefinition
        {
            Id = "ico",
            DisplayName = "Icono (.ico)",
            DefaultExtension = ".ico",
            Extensions = new[] { ".ico" },
            MagickFormat = MagickFormat.Ico,
            SupportsAlpha = true,
            SupportsLossyQuality = false,
            SupportsLossless = true,
            DefaultQuality = 100,
            IsAnimated = false,
            Description = "Icono de aplicación o favicon para Windows y navegadores."
        },
        new ImageFormatDefinition
        {
            Id = "heic",
            DisplayName = "HEIC (.heic)",
            DefaultExtension = ".heic",
            Extensions = new[] { ".heic", ".heif" },
            MagickFormat = MagickFormat.Heic,
            SupportsAlpha = true,
            SupportsLossyQuality = true,
            SupportsLossless = false,
            DefaultQuality = 85,
            IsAnimated = false,
            Description = "Formato de alta eficiencia utilizado por dispositivos móviles."
        }
    };

    private static readonly Lazy<HashSet<string>> SupportedExtensionsCache = new(() =>
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var format in Formats)
        {
            foreach (var ext in format.Extensions)
            {
                set.Add(ext);
            }
        }
        return set;
    });

    public IReadOnlyList<ImageFormatDefinition> GetAllFormats() => Formats;

    public ImageFormatDefinition? GetFormatById(string id)
    {
        return Formats.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public ImageFormatDefinition? GetFormatByExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return null;
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        return Formats.FirstOrDefault(f => f.Extensions.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)));
    }

    public bool IsExtensionSupported(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        return SupportedExtensionsCache.Value.Contains(ext);
    }

    public IReadOnlyList<string> GetAllSupportedExtensions()
    {
        return SupportedExtensionsCache.Value.OrderBy(x => x).ToList();
    }
}
