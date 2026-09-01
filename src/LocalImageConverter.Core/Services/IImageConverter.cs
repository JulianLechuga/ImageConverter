using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public interface IImageConverter
{
    Task<ConversionResult> ConvertAsync(
        ImageFileInfo fileInfo,
        ConversionOptions options,
        CancellationToken cancellationToken = default);
}
