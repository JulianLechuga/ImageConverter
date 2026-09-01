using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public class FileNameResolver : IFileNameResolver
{
    public string DetermineOutputDirectory(string sourceFilePath, ConversionOptions options)
    {
        var sourceDir = Path.GetDirectoryName(sourceFilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        return options.OutputDirectoryMode switch
        {
            OutputDirectoryMode.CustomFolder => !string.IsNullOrWhiteSpace(options.CustomOutputDirectory) && Directory.Exists(options.CustomOutputDirectory)
                ? options.CustomOutputDirectory
                : Path.Combine(sourceDir, "Converted"),

            OutputDirectoryMode.SameFolderAsOriginal => sourceDir,

            OutputDirectoryMode.ConvertedSubfolder or _ => Path.Combine(sourceDir, "Converted")
        };
    }

    public string ResolveDestinationFilePath(
        string sourceFilePath,
        string outputDirectory,
        string targetExtension,
        ConflictResolution conflictResolution)
    {
        if (!targetExtension.StartsWith('.'))
        {
            targetExtension = "." + targetExtension;
        }

        var sourceDir = Path.GetDirectoryName(sourceFilePath) ?? string.Empty;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);
        var sourceExt = Path.GetExtension(sourceFilePath);

        var isSameDirectory = string.Equals(
            Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

        var isSameExtension = string.Equals(sourceExt, targetExtension, StringComparison.OrdinalIgnoreCase);

        // Protection against in-place overwriting original file
        var wouldOverwriteOriginal = isSameDirectory && isSameExtension;

        var baseCandidateName = wouldOverwriteOriginal
            ? $"{fileNameWithoutExt}_converted{targetExtension}"
            : $"{fileNameWithoutExt}{targetExtension}";

        var candidatePath = Path.Combine(outputDirectory, baseCandidateName);

        if (!File.Exists(candidatePath) && !string.Equals(candidatePath, sourceFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return candidatePath;
        }

        // If file exists or points to original, handle according to policy
        if (conflictResolution == ConflictResolution.Overwrite && !wouldOverwriteOriginal)
        {
            return candidatePath;
        }

        // AutoRename logic (e.g. photo.webp -> photo_1.webp, photo_2.webp)
        var counter = 1;
        var baseNameForRenaming = wouldOverwriteOriginal ? $"{fileNameWithoutExt}_converted" : fileNameWithoutExt;

        while (true)
        {
            var newName = $"{baseNameForRenaming}_{counter}{targetExtension}";
            var newPath = Path.Combine(outputDirectory, newName);

            if (!File.Exists(newPath) && !string.Equals(newPath, sourceFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return newPath;
            }

            counter++;
            if (counter > 10000)
            {
                // Safety escape
                return Path.Combine(outputDirectory, $"{baseNameForRenaming}_{Guid.NewGuid():N}{targetExtension}");
            }
        }
    }
}
