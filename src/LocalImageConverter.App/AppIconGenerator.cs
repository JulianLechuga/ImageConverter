using System.IO;
using ImageMagick;
using ImageMagick.Drawing;

namespace LocalImageConverter.App;

public static class AppIconGenerator
{
    public static void GenerateAppIcon(string outputPath)
    {
        var sizes = new uint[] { 16, 24, 32, 48, 64, 128, 256 };
        using var collection = new MagickImageCollection();

        foreach (var size in sizes)
        {
            var image = new MagickImage(MagickColors.Transparent, size, size);

            // Draw a rounded rectangle with modern gradient background
            var radius = size * 0.22;
            var drawables = new Drawables()
                .FillColor(new MagickColor("#4F46E5")) // Modern indigo
                .RoundRectangle(0, 0, size - 1, size - 1, radius, radius)
                .FillColor(new MagickColor("#06B6D4")) // Cyan accent circle
                .Circle(size * 0.65, size * 0.35, size * 0.65, size * 0.22)
                .FillColor(MagickColors.White)
                // Shutter / image converter glyph
                .Polygon(new[]
                {
                    new PointD(size * 0.25, size * 0.70),
                    new PointD(size * 0.45, size * 0.45),
                    new PointD(size * 0.60, size * 0.58),
                    new PointD(size * 0.75, size * 0.35),
                    new PointD(size * 0.85, size * 0.70)
                });

            image.Draw(drawables);
            image.Format = MagickFormat.Ico;
            collection.Add(image);
        }

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        collection.Write(outputPath, MagickFormat.Ico);
    }
}
