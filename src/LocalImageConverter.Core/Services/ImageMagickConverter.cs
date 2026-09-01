using System.Diagnostics;
using ImageMagick;
using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public class ImageMagickConverter : IImageConverter
{
    private readonly IFileNameResolver _fileNameResolver;
    private readonly ILoggerService? _logger;

    public ImageMagickConverter(IFileNameResolver fileNameResolver, ILoggerService? logger = null)
    {
        _fileNameResolver = fileNameResolver;
        _logger = logger;
    }

    public async Task<ConversionResult> ConvertAsync(
        ImageFileInfo fileInfo,
        ConversionOptions options,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        string? tempFilePath = null;
        string? finalDestinationPath = null;

        try
        {
            if (!File.Exists(fileInfo.FilePath))
            {
                return ConversionResult.Fail(fileInfo.FilePath, "El archivo original no existe en el disco.");
            }

            // 1. Determine output directory and target filename
            var outputDir = _fileNameResolver.DetermineOutputDirectory(fileInfo.FilePath, options);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            finalDestinationPath = _fileNameResolver.ResolveDestinationFilePath(
                fileInfo.FilePath,
                outputDir,
                options.TargetFormat.DefaultExtension,
                options.ConflictResolution);

            if (options.ConflictResolution == ConflictResolution.Skip && File.Exists(finalDestinationPath))
            {
                var existingInfo = new FileInfo(finalDestinationPath);
                return ConversionResult.Ok(
                    fileInfo.FilePath,
                    finalDestinationPath,
                    fileInfo.FileSizeBytes,
                    existingInfo.Length,
                    stopwatch.ElapsedMilliseconds);
            }

            // Temp file for atomic write
            var tempFileName = $"{Path.GetFileName(finalDestinationPath)}.tmp.{Guid.NewGuid():N}";
            tempFilePath = Path.Combine(outputDir, tempFileName);

            // 2. Perform conversion asynchronously
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if animated GIF / multi-frame
                using var collection = new MagickImageCollection(fileInfo.FilePath);
                cancellationToken.ThrowIfCancellationRequested();

                if (collection.Count == 0)
                {
                    throw new InvalidOperationException("No se pudieron decodificar imágenes del archivo.");
                }

                var supportsAnimation = options.TargetFormat.IsAnimated && (options.TargetFormat.MagickFormat is MagickFormat.Gif or MagickFormat.WebP);

                if (collection.Count > 1 && supportsAnimation)
                {
                    // Multi-frame animation conversion
                    foreach (var frame in collection)
                    {
                        ProcessSingleFrame(frame, options);
                    }

                    collection.Write(tempFilePath, options.TargetFormat.MagickFormat);
                }
                else
                {
                    // Single image or take first frame
                    using var singleImage = collection.Count > 1 ? collection[0].Clone() : collection[0];

                    ProcessSingleFrame(singleImage, options);

                    // Handle ICO specific size restrictions
                    if (options.TargetFormat.MagickFormat == MagickFormat.Ico)
                    {
                        if (singleImage.Width > 256 || singleImage.Height > 256)
                        {
                            singleImage.Resize(new MagickGeometry(256, 256) { IgnoreAspectRatio = false });
                        }
                    }

                    singleImage.Write(tempFilePath, options.TargetFormat.MagickFormat);
                }
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // 3. Verify atomic temp file creation
            if (!File.Exists(tempFilePath))
            {
                throw new IOException("No se pudo generar el archivo temporal de salida.");
            }

            var tempInfo = new FileInfo(tempFilePath);
            if (tempInfo.Length == 0)
            {
                throw new IOException("El archivo generado está vacío (0 bytes).");
            }

            // 4. Move temp file to final destination atomically
            if (File.Exists(finalDestinationPath))
            {
                File.Delete(finalDestinationPath);
            }
            File.Move(tempFilePath, finalDestinationPath);

            var finalInfo = new FileInfo(finalDestinationPath);
            stopwatch.Stop();

            return ConversionResult.Ok(
                fileInfo.FilePath,
                finalDestinationPath,
                fileInfo.FileSizeBytes,
                finalInfo.Length,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            CleanTempFile(tempFilePath);
            return ConversionResult.Fail(fileInfo.FilePath, "Conversión cancelada por el usuario.", fileInfo.FileSizeBytes);
        }
        catch (Exception ex)
        {
            CleanTempFile(tempFilePath);
            _logger?.LogError($"Error convirtiendo '{fileInfo.FileName}' a {options.TargetFormat.DisplayName}", ex);
            return ConversionResult.Fail(fileInfo.FilePath, $"Error: {ex.Message}", fileInfo.FileSizeBytes);
        }
    }

    private void ProcessSingleFrame(IMagickImage<byte> image, ConversionOptions options)
    {
        // 1. Auto-Orientation based on EXIF
        if (options.AutoOrient)
        {
            image.AutoOrient();
        }

        // 2. High quality Resize if requested
        ApplyResize(image, options);

        // 3. Transparency handling (e.g. PNG with Alpha -> JPEG/BMP without Alpha)
        if (!options.TargetFormat.SupportsAlpha && image.HasAlpha)
        {
            var bgColor = GetMagickColor(options.AlphaBackground, options.CustomAlphaBackgroundHex);
            image.ColorAlpha(bgColor);
            image.Alpha(AlphaOption.Off);
            image.ColorSpace = ColorSpace.sRGB;
        }

        // 4. Quality & Lossless settings
        if (options.TargetFormat.SupportsLossyQuality)
        {
            image.Quality = (uint)Math.Clamp(options.Quality, 1, 100);
        }

        // 5. Metadata handling
        ApplyMetadataOptions(image, options.MetadataOption);
    }

    private static void ApplyResize(IMagickImage<byte> image, ConversionOptions options)
    {
        image.FilterType = FilterType.Lanczos;

        switch (options.ResizeMode)
        {
            case ResizeMode.MaxDimension:
                if (options.MaxDimension > 0 && (image.Width > options.MaxDimension || image.Height > options.MaxDimension))
                {
                    var geo = new MagickGeometry((uint)options.MaxDimension, (uint)options.MaxDimension)
                    {
                        IgnoreAspectRatio = false,
                        Greater = true
                    };
                    image.Resize(geo);
                }
                break;

            case ResizeMode.CustomDimensions:
                var targetW = options.CustomWidth.GetValueOrDefault(0);
                var targetH = options.CustomHeight.GetValueOrDefault(0);

                if (targetW > 0 && targetH > 0)
                {
                    var geo = new MagickGeometry((uint)targetW, (uint)targetH)
                    {
                        IgnoreAspectRatio = !options.KeepAspectRatio
                    };
                    image.Resize(geo);
                }
                else if (targetW > 0)
                {
                    var geo = new MagickGeometry((uint)targetW, 0)
                    {
                        IgnoreAspectRatio = false
                    };
                    image.Resize(geo);
                }
                else if (targetH > 0)
                {
                    var geo = new MagickGeometry(0, (uint)targetH)
                    {
                        IgnoreAspectRatio = false
                    };
                    image.Resize(geo);
                }
                break;

            case ResizeMode.KeepOriginal:
            default:
                break;
        }
    }

    private static void ApplyMetadataOptions(IMagickImage<byte> image, MetadataOption option)
    {
        switch (option)
        {
            case MetadataOption.StripAll:
                image.Strip();
                break;

            case MetadataOption.StripPrivateGpsExif:
                var icc = image.GetColorProfile();
                image.Strip();
                if (icc != null)
                {
                    image.SetProfile(icc);
                }
                break;

            case MetadataOption.KeepAll:
            default:
                break;
        }
    }

    private static MagickColor GetMagickColor(AlphaBackgroundColor background, string customHex)
    {
        return background switch
        {
            AlphaBackgroundColor.Black => MagickColors.Black,
            AlphaBackgroundColor.CustomHex => TryParseHex(customHex, MagickColors.White),
            AlphaBackgroundColor.White or _ => MagickColors.White
        };
    }

    private static MagickColor TryParseHex(string hex, MagickColor fallback)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                return new MagickColor(hex);
            }
        }
        catch
        {
            // fallback
        }
        return fallback;
    }

    private static void CleanTempFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
