using LocalImageConverter.App;
using Xunit;

namespace LocalImageConverter.Tests;

public class AssetGenerationTests
{
    [Fact]
    public void GenerateApplicationIcon_CreatesValidIco()
    {
        var appDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "LocalImageConverter.App", "Assets"));
        Directory.CreateDirectory(appDir);
        var iconPath = Path.Combine(appDir, "app.ico");

        AppIconGenerator.GenerateAppIcon(iconPath);

        Assert.True(File.Exists(iconPath));
        var fi = new FileInfo(iconPath);
        Assert.True(fi.Length > 1000);
    }
}
