using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public interface IFileNameResolver
{
    string DetermineOutputDirectory(string sourceFilePath, ConversionOptions options);
    string ResolveDestinationFilePath(string sourceFilePath, string outputDirectory, string targetExtension, ConflictResolution conflictResolution);
}
