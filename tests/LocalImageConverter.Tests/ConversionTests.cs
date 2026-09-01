using ImageMagick;
using LocalImageConverter.Core.Models;
using LocalImageConverter.Core.Services;
using Xunit;

namespace LocalImageConverter.Tests;

public class ConversionTests : IDisposable
{
    private readonly string _testDir;
    private readonly IImageFormatCatalog _catalog;
    private readonly IFileNameResolver _fileNameResolver;
    private readonly IImageConverter _converter;
    private readonly IImageScanner _scanner;

    public ConversionTests()
    {
        _testDir = TestImageGenerator.CreateTestDirectory();
        _catalog = new ImageFormatCatalog();
        _fileNameResolver = new FileNameResolver();
        _converter = new ImageMagickConverter(_fileNameResolver);
        _scanner = new ImageScanner(_catalog);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task CasoA_JpgToPng_ConvertsSuccessfully()
    {
        // Arrange
        var jpgPath = TestImageGenerator.CreateSampleJpg(_testDir, "test_a.jpg", 800, 600);
        var fileInfo = await _scanner.InspectFileAsync(jpgPath);
        Assert.NotNull(fileInfo);

        var pngFormat = _catalog.GetFormatById("png");
        Assert.NotNull(pngFormat);

        var options = new ConversionOptions
        {
            TargetFormat = pngFormat,
            OutputDirectoryMode = OutputDirectoryMode.SameFolderAsOriginal
        };

        // Act
        var result = await _converter.ConvertAsync(fileInfo, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DestinationFilePath);
        Assert.True(File.Exists(result.DestinationFilePath));
        Assert.EndsWith(".png", result.DestinationFilePath);

        using var outputImage = new MagickImage(result.DestinationFilePath);
        Assert.Equal(MagickFormat.Png, outputImage.Format);
        Assert.Equal(800u, outputImage.Width);
        Assert.Equal(600u, outputImage.Height);
    }

    [Fact]
    public async Task CasoB_TransparentPngToJpg_ReplacesAlphaWithWhiteBackground()
    {
        // Arrange
        var pngPath = TestImageGenerator.CreateTransparentPng(_testDir, "transparent.png", 400, 400);
        var fileInfo = await _scanner.InspectFileAsync(pngPath);
        Assert.NotNull(fileInfo);

        var jpgFormat = _catalog.GetFormatById("jpg");
        Assert.NotNull(jpgFormat);

        var options = new ConversionOptions
        {
            TargetFormat = jpgFormat,
            AlphaBackground = AlphaBackgroundColor.White,
            OutputDirectoryMode = OutputDirectoryMode.SameFolderAsOriginal
        };

        // Act
        var result = await _converter.ConvertAsync(fileInfo, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DestinationFilePath);
        Assert.True(File.Exists(result.DestinationFilePath));
        Assert.EndsWith(".jpg", result.DestinationFilePath);

        using var outputImage = new MagickImage(result.DestinationFilePath);
        Assert.Equal(MagickFormat.Jpeg, outputImage.Format);
        Assert.False(outputImage.HasAlpha);

        // Verify that top-left pixel (originally transparent) is now white
        using var pixels = outputImage.GetPixels();
        var topLeftPixel = pixels.GetPixel(0, 0).ToColor();
        Assert.NotNull(topLeftPixel);
        Assert.Equal(255, topLeftPixel.R);
        Assert.Equal(255, topLeftPixel.G);
        Assert.Equal(255, topLeftPixel.B);
    }

    [Fact]
    public async Task CasoC_JpgToWebP_ConfigurableQuality()
    {
        // Arrange
        var jpgPath = TestImageGenerator.CreateSampleJpg(_testDir, "quality_test.jpg", 1200, 800);
        var fileInfo = await _scanner.InspectFileAsync(jpgPath);
        Assert.NotNull(fileInfo);

        var webpFormat = _catalog.GetFormatById("webp");
        Assert.NotNull(webpFormat);

        var optionsHigh = new ConversionOptions
        {
            TargetFormat = webpFormat,
            Quality = 95,
            OutputDirectoryMode = OutputDirectoryMode.SameFolderAsOriginal
        };

        var optionsLow = new ConversionOptions
        {
            TargetFormat = webpFormat,
            Quality = 30,
            OutputDirectoryMode = OutputDirectoryMode.SameFolderAsOriginal
        };

        // Act
        var resultHigh = await _converter.ConvertAsync(fileInfo, optionsHigh);
        var resultLow = await _converter.ConvertAsync(fileInfo, optionsLow);

        // Assert
        Assert.True(resultHigh.Success);
        Assert.True(resultLow.Success);
        Assert.True(File.Exists(resultHigh.DestinationFilePath));
        Assert.True(File.Exists(resultLow.DestinationFilePath));

        // High quality should generally have equal or larger file size than low quality
        Assert.True(resultHigh.ConvertedBytes >= resultLow.ConvertedBytes);
    }

    [Fact]
    public async Task CasoD_BatchQueue_ProcessesMultipleFilesCorrectly()
    {
        // Arrange
        var files = new List<ImageFileInfo>();
        for (int i = 0; i < 8; i++)
        {
            var p = TestImageGenerator.CreateSampleJpg(_testDir, $"batch_{i}.jpg", 400, 300);
            var info = await _scanner.InspectFileAsync(p);
            Assert.NotNull(info);
            files.Add(info);
        }

        var webpFormat = _catalog.GetFormatById("webp")!;
        var options = new ConversionOptions
        {
            TargetFormat = webpFormat,
            MaxConcurrency = 4,
            OutputDirectoryMode = OutputDirectoryMode.ConvertedSubfolder
        };

        var queue = new ConversionQueue(_converter);

        // Act
        var results = await queue.ExecuteQueueAsync(files, options);

        // Assert
        Assert.Equal(8, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(files, f => Assert.Equal(ItemStatus.Completed, f.Status));
    }

    [Fact]
    public async Task CasoE_Cancellation_CancelsPendingJobsAndCleansUp()
    {
        // Arrange
        var files = new List<ImageFileInfo>();
        for (int i = 0; i < 15; i++)
        {
            var p = TestImageGenerator.CreateSampleJpg(_testDir, $"cancel_batch_{i}.jpg", 600, 400);
            var info = await _scanner.InspectFileAsync(p);
            Assert.NotNull(info);
            files.Add(info);
        }

        var webpFormat = _catalog.GetFormatById("webp")!;
        var options = new ConversionOptions
        {
            TargetFormat = webpFormat,
            MaxConcurrency = 1, // Single worker to ensure cancellation triggers before all finish
            OutputDirectoryMode = OutputDirectoryMode.ConvertedSubfolder
        };

        var queue = new ConversionQueue(_converter);
        using var cts = new CancellationTokenSource();

        // Act
        queue.ProgressChanged += (sender, report) =>
        {
            if (report.CompletedItems >= 2)
            {
                cts.Cancel();
            }
        };

        var results = await queue.ExecuteQueueAsync(files, options, cts.Token);

        // Assert
        Assert.Contains(files, f => f.Status == ItemStatus.Cancelled);
        // Original files must remain 100% intact
        Assert.All(files, f => Assert.True(File.Exists(f.FilePath)));
    }

    [Fact]
    public async Task CasoF_Resize_4000x3000ToMax1920_MaintainsAspectRatio()
    {
        // Arrange
        var largeJpg = TestImageGenerator.CreateLargeImage(_testDir, "large_4000x3000.jpg", 4000, 3000);
        var fileInfo = await _scanner.InspectFileAsync(largeJpg);
        Assert.NotNull(fileInfo);

        var webpFormat = _catalog.GetFormatById("webp")!;
        var options = new ConversionOptions
        {
            TargetFormat = webpFormat,
            ResizeMode = ResizeMode.MaxDimension,
            MaxDimension = 1920,
            OutputDirectoryMode = OutputDirectoryMode.SameFolderAsOriginal
        };

        // Act
        var result = await _converter.ConvertAsync(fileInfo, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DestinationFilePath);

        using var outputImage = new MagickImage(result.DestinationFilePath);
        Assert.Equal(1920u, outputImage.Width);
        Assert.Equal(1440u, outputImage.Height); // 4000x3000 scaled down to 1920 maintains 4:3 = 1440
    }

    [Fact]
    public async Task CasoG_ExifAutoOrient_AppliesCorrectOrientation()
    {
        // Arrange
        var orientedJpg = TestImageGenerator.CreateOrientedJpg(_testDir, "exif_orient.jpg");
        var fileInfo = await _scanner.InspectFileAsync(orientedJpg);
        Assert.NotNull(fileInfo);

        var pngFormat = _catalog.GetFormatById("png")!;
        var options = new ConversionOptions
        {
            TargetFormat = pngFormat,
            AutoOrient = true,
            OutputDirectoryMode = OutputDirectoryMode.SameFolderAsOriginal
        };

        // Act
        var result = await _converter.ConvertAsync(fileInfo, options);

        // Assert
        Assert.True(result.Success);
        using var outputImage = new MagickImage(result.DestinationFilePath);
        // After auto orient of 600x400 with RightTop (90 deg), dimensions should be 400x600 and TopLeft orientation
        Assert.Equal(400u, outputImage.Width);
        Assert.Equal(600u, outputImage.Height);
        Assert.Equal(OrientationType.TopLeft, outputImage.Orientation);
    }

    [Fact]
    public async Task CasoH_CorruptedFile_ProducesIndividualErrorAndQueueContinues()
    {
        // Arrange
        var validJpg = TestImageGenerator.CreateSampleJpg(_testDir, "valid1.jpg");
        var corruptJpg = TestImageGenerator.CreateCorruptedFile(_testDir, "corrupted.jpg");
        var validJpg2 = TestImageGenerator.CreateSampleJpg(_testDir, "valid2.jpg");

        var f1 = await _scanner.InspectFileAsync(validJpg);
        var f2 = new ImageFileInfo
        {
            FilePath = corruptJpg,
            FileName = "corrupted.jpg",
            FileExtension = ".jpg",
            FileSizeBytes = 8
        };
        var f3 = await _scanner.InspectFileAsync(validJpg2);

        var items = new List<ImageFileInfo> { f1!, f2, f3! };

        var pngFormat = _catalog.GetFormatById("png")!;
        var options = new ConversionOptions
        {
            TargetFormat = pngFormat,
            OutputDirectoryMode = OutputDirectoryMode.ConvertedSubfolder
        };

        var queue = new ConversionQueue(_converter);

        // Act
        var results = await queue.ExecuteQueueAsync(items, options);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(ItemStatus.Completed, f1!.Status);
        Assert.Equal(ItemStatus.Error, f2.Status);
        Assert.NotNull(f2.ErrorMessage);
        Assert.Equal(ItemStatus.Completed, f3!.Status);
    }

    [Fact]
    public async Task CasoI_CollisionResolution_AutoRenamesExistingTarget()
    {
        // Arrange
        var jpg1 = TestImageGenerator.CreateSampleJpg(_testDir, "photo.jpg");
        var f1 = await _scanner.InspectFileAsync(jpg1);

        var webpFormat = _catalog.GetFormatById("webp")!;
        var options = new ConversionOptions
        {
            TargetFormat = webpFormat,
            ConflictResolution = ConflictResolution.AutoRename,
            OutputDirectoryMode = OutputDirectoryMode.SameFolderAsOriginal
        };

        // Act: 1st conversion -> creates photo.webp
        var res1 = await _converter.ConvertAsync(f1!, options);
        Assert.True(res1.Success);
        Assert.EndsWith("photo.webp", res1.DestinationFilePath);

        // Act: 2nd conversion with same target -> should create photo_1.webp
        var res2 = await _converter.ConvertAsync(f1!, options);
        Assert.True(res2.Success);
        Assert.EndsWith("photo_1.webp", res2.DestinationFilePath);

        // Act: 3rd conversion -> should create photo_2.webp
        var res3 = await _converter.ConvertAsync(f1!, options);
        Assert.True(res3.Success);
        Assert.EndsWith("photo_2.webp", res3.DestinationFilePath);

        Assert.True(File.Exists(res1.DestinationFilePath));
        Assert.True(File.Exists(res2.DestinationFilePath));
        Assert.True(File.Exists(res3.DestinationFilePath));
    }

    [Fact]
    public async Task InPlaceSameFormat_ProtectsOriginalFileFromOverwrite()
    {
        // Arrange: C:\temp\img.jpg converted to JPG in same directory
        var jpg = TestImageGenerator.CreateSampleJpg(_testDir, "picture.jpg");
        var f = await _scanner.InspectFileAsync(jpg);

        var jpgFormat = _catalog.GetFormatById("jpg")!;
        var options = new ConversionOptions
        {
            TargetFormat = jpgFormat,
            ConflictResolution = ConflictResolution.AutoRename,
            OutputDirectoryMode = OutputDirectoryMode.SameFolderAsOriginal
        };

        // Act
        var result = await _converter.ConvertAsync(f!, options);

        // Assert
        Assert.True(result.Success);
        // Result must NOT be "picture.jpg" (which would overwrite the original)
        Assert.NotEqual(jpg, result.DestinationFilePath);
        Assert.Contains("picture_converted", result.DestinationFilePath);
        Assert.True(File.Exists(jpg)); // Original remains intact
    }
}
