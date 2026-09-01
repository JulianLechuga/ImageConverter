using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public interface IImageFormatCatalog
{
    IReadOnlyList<ImageFormatDefinition> GetAllFormats();
    ImageFormatDefinition? GetFormatById(string id);
    ImageFormatDefinition? GetFormatByExtension(string extension);
    bool IsExtensionSupported(string extension);
    IReadOnlyList<string> GetAllSupportedExtensions();
}
