using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalImageConverter.App.Converters;
using LocalImageConverter.Core.Models;
using LocalImageConverter.Core.Services;

namespace LocalImageConverter.App.ViewModels;

public partial class ImageItemViewModel : ObservableObject
{
    private readonly ImageFileInfo _model;
    private readonly IImageScanner _scanner;
    private BitmapImage? _thumbnailImage;
    private bool _thumbnailLoading;

    public ImageFileInfo Model => _model;

    public string Id => _model.Id;
    public string FilePath => _model.FilePath;
    public string FileName => _model.FileName;
    public string FileExtension => _model.FileExtension.ToUpperInvariant().TrimStart('.');
    public long FileSizeBytes => _model.FileSizeBytes;
    public string FileSizeFormatted => ByteSizeConverter.FormatBytes(_model.FileSizeBytes);

    public uint Width => _model.Width;
    public uint Height => _model.Height;
    public string DimensionsFormatted => Width > 0 && Height > 0 ? $"{Width} × {Height}" : "-";
    public string DetectedFormat => _model.DetectedFormat;
    public bool HasAlpha => _model.HasAlpha;
    public bool IsAnimated => _model.IsAnimated;

    [ObservableProperty]
    private ItemStatus _status;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _convertedFilePath;

    [ObservableProperty]
    private long? _convertedFileSizeBytes;

    [ObservableProperty]
    private long _durationMs;

    public BitmapImage? ThumbnailImage
    {
        get
        {
            if (_thumbnailImage == null && !_thumbnailLoading)
            {
                _ = LoadThumbnailAsync();
            }
            return _thumbnailImage;
        }
        private set => SetProperty(ref _thumbnailImage, value);
    }

    public string StatusDisplayText => Status switch
    {
        ItemStatus.Waiting => "Esperando",
        ItemStatus.Processing => "Procesando...",
        ItemStatus.Completed => "Completado",
        ItemStatus.Error => "Error",
        ItemStatus.Cancelled => "Cancelado",
        _ => "Desconocido"
    };

    public string ConvertedSizeFormatted => ConvertedFileSizeBytes.HasValue
        ? ByteSizeConverter.FormatBytes(ConvertedFileSizeBytes.Value)
        : "-";

    public string SavingsDisplayText
    {
        get
        {
            if (!ConvertedFileSizeBytes.HasValue || FileSizeBytes <= 0) return string.Empty;
            var diff = FileSizeBytes - ConvertedFileSizeBytes.Value;
            if (diff <= 0) return "Sin ahorro";
            var pct = Math.Round((double)diff / FileSizeBytes * 100.0, 1);
            return $"-{ByteSizeConverter.FormatBytes(diff)} ({pct}%)";
        }
    }

    public ImageItemViewModel(ImageFileInfo model, IImageScanner scanner)
    {
        _model = model;
        _scanner = scanner;
        _status = model.Status;
        _errorMessage = model.ErrorMessage;
        _convertedFilePath = model.ConvertedFilePath;
        _convertedFileSizeBytes = model.ConvertedFileSizeBytes;
        _durationMs = model.DurationMs;
    }

    public void SyncFromModel()
    {
        Status = _model.Status;
        ErrorMessage = _model.ErrorMessage;
        ConvertedFilePath = _model.ConvertedFilePath;
        ConvertedFileSizeBytes = _model.ConvertedFileSizeBytes;
        DurationMs = _model.DurationMs;
        OnPropertyChanged(nameof(StatusDisplayText));
        OnPropertyChanged(nameof(ConvertedSizeFormatted));
        OnPropertyChanged(nameof(SavingsDisplayText));
    }

    private async Task LoadThumbnailAsync()
    {
        _thumbnailLoading = true;
        try
        {
            var bytes = await _scanner.GenerateThumbnailAsync(FilePath, 128);
            if (bytes != null && bytes.Length > 0)
            {
                var bitmap = new BitmapImage();
                using var stream = new MemoryStream(bytes);
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                ThumbnailImage = bitmap;
            }
        }
        catch
        {
            // Ignore thumbnail load failures
        }
        finally
        {
            _thumbnailLoading = false;
        }
    }
}
