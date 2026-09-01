using ImageMagick;
using ImageMagick.Drawing;

namespace LocalImageConverter.Tests;

public static class TestImageGenerator
{
    public static string CreateTestDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "LocalImageConverterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string CreateSampleJpg(string directory, string filename = "sample.jpg", uint width = 800, uint height = 600)
    {
        var path = Path.Combine(directory, filename);
        using var image = new MagickImage(MagickColors.SteelBlue, width, height);
        image.Write(path, MagickFormat.Jpeg);
        return path;
    }

    public static string CreateTransparentPng(string directory, string filename = "transparent.png", uint width = 400, uint height = 400)
    {
        var path = Path.Combine(directory, filename);
        using var image = new MagickImage(MagickColors.Transparent, width, height);
        // Draw a red circle in center
        var drawables = new Drawables()
            .FillColor(MagickColors.Red)
            .Circle(width / 2.0, height / 2.0, width / 2.0, height / 4.0);
        image.Draw(drawables);
        image.Write(path, MagickFormat.Png);
        return path;
    }

    public static string CreateLargeImage(string directory, string filename = "large.jpg", uint width = 4000, uint height = 3000)
    {
        var path = Path.Combine(directory, filename);
        using var image = new MagickImage(MagickColors.DarkGreen, width, height);
        image.Write(path, MagickFormat.Jpeg);
        return path;
    }

    public static string CreateOrientedJpg(string directory, string filename = "oriented.jpg")
    {
        var path = Path.Combine(directory, filename);
        using var image = new MagickImage(MagickColors.Orange, 600, 400);
        var profile = new ExifProfile();
        profile.SetValue(ExifTag.Orientation, (ushort)6); // 6 = RightTop (Rotate 90 CW)
        image.SetProfile(profile);
        image.Orientation = OrientationType.RightTop;
        image.Write(path, MagickFormat.Jpeg);
        return path;
    }

    public static string CreateCorruptedFile(string directory, string filename = "corrupt.jpg")
    {
        var path = Path.Combine(directory, filename);
        File.WriteAllBytes(path, new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x12, 0x34, 0x56, 0x78 }); // Corrupted JPEG header
        return path;
    }
}
