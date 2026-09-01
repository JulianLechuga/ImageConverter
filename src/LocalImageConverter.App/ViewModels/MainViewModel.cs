using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalImageConverter.App.Converters;
using LocalImageConverter.Core.Models;
using LocalImageConverter.Core.Services;
using ResizeMode = LocalImageConverter.Core.Models.ResizeMode;

namespace LocalImageConverter.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IImageFormatCatalog _formatCatalog;
    private readonly IImageScanner _imageScanner;
    private readonly IFileNameResolver _fileNameResolver;
    private readonly IConversionQueue _conversionQueue;
    private readonly ISettingsService _settingsService;
    private readonly ILoggerService _logger;

    public ObservableCollection<ImageItemViewModel> Items { get; } = new();
    public IReadOnlyList<ImageFormatDefinition> AvailableFormats { get; }
    public IReadOnlyList<PresetDefinition> AvailablePresets { get; }

    [ObservableProperty]
    private ImageItemViewModel? _selectedItem;

    [ObservableProperty]
    private ImageFormatDefinition _selectedFormat;

    [ObservableProperty]
    private int _quality = 85;

    [ObservableProperty]
    private ResizeMode _selectedResizeMode = ResizeMode.KeepOriginal;

    [ObservableProperty]
    private int _selectedMaxDimension = 1920;

    [ObservableProperty]
    private int? _customWidth;

    [ObservableProperty]
    private int? _customHeight;

    [ObservableProperty]
    private bool _keepAspectRatio = true;

    [ObservableProperty]
    private MetadataOption _selectedMetadataOption = MetadataOption.KeepAll;

    [ObservableProperty]
    private AlphaBackgroundColor _selectedAlphaBackground = AlphaBackgroundColor.White;

    [ObservableProperty]
    private string _customAlphaHex = "#FFFFFF";

    [ObservableProperty]
    private OutputDirectoryMode _selectedOutputMode = OutputDirectoryMode.ConvertedSubfolder;

    [ObservableProperty]
    private string _customOutputFolder = string.Empty;

    [ObservableProperty]
    private ConflictResolution _selectedConflictResolution = ConflictResolution.AutoRename;

    [ObservableProperty]
    private int _maxConcurrency = Math.Max(1, Math.Min(4, Environment.ProcessorCount));

    [ObservableProperty]
    private bool _scanSubfolders = true;

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private bool _isCancelling;

    [ObservableProperty]
    private double _overallProgressPercentage;

    [ObservableProperty]
    private string _statusHeader = "Listo para convertir";

    [ObservableProperty]
    private string _statusDetail = "100 % local. Tus imágenes nunca salen de tu computadora.";

    [ObservableProperty]
    private bool _isAdvancedSettingsExpanded;

    [ObservableProperty]
    private bool _hasCompletedBatch;

    [ObservableProperty]
    private long _batchOriginalBytes;

    [ObservableProperty]
    private long _batchConvertedBytes;

    [ObservableProperty]
    private long _batchSavedBytes;

    [ObservableProperty]
    private double _batchSavingsPercentage;

    [ObservableProperty]
    private int _batchSuccessCount;

    [ObservableProperty]
    private int _batchErrorCount;

    [ObservableProperty]
    private string? _lastOutputDirectory;

    public bool HasItems => Items.Count > 0;
    public bool HasNoItems => Items.Count == 0;
    public int TotalItemsCount => Items.Count;

    public long TotalInputSizeBytes => Items.Sum(i => i.FileSizeBytes);
    public string TotalInputSizeFormatted => ByteSizeConverter.FormatBytes(TotalInputSizeBytes);

    public string TotalSummaryText => $"{TotalItemsCount} {(TotalItemsCount == 1 ? "archivo" : "archivos")} • {TotalInputSizeFormatted}";

    public bool ShowQualityControl => SelectedFormat?.SupportsLossyQuality ?? false;
    public bool ShowAlphaBackgroundControl => !(SelectedFormat?.SupportsAlpha ?? true);
    public bool ShowCustomResizeInputs => SelectedResizeMode == ResizeMode.CustomDimensions;
    public bool ShowMaxDimensionDropdown => SelectedResizeMode == ResizeMode.MaxDimension;
    public bool ShowCustomFolderInput => SelectedOutputMode == OutputDirectoryMode.CustomFolder;

    public MainViewModel(
        IImageFormatCatalog formatCatalog,
        IImageScanner imageScanner,
        IFileNameResolver fileNameResolver,
        IConversionQueue conversionQueue,
        ISettingsService settingsService,
        ILoggerService logger)
    {
        _formatCatalog = formatCatalog;
        _imageScanner = imageScanner;
        _fileNameResolver = fileNameResolver;
        _conversionQueue = conversionQueue;
        _settingsService = settingsService;
        _logger = logger;

        AvailableFormats = _formatCatalog.GetAllFormats();
        AvailablePresets = PresetDefinition.Defaults;

        // Load settings
        var settings = _settingsService.LoadSettings();
        _selectedFormat = _formatCatalog.GetFormatById(settings.TargetFormatId) ?? AvailableFormats[0];
        _quality = settings.Quality;
        _selectedResizeMode = settings.ResizeMode;
        _selectedMaxDimension = settings.MaxDimension;
        _keepAspectRatio = settings.KeepAspectRatio;
        _selectedMetadataOption = settings.MetadataOption;
        _selectedAlphaBackground = settings.AlphaBackground;
        _customAlphaHex = settings.CustomAlphaBackgroundHex;
        _selectedOutputMode = settings.OutputDirectoryMode;
        _customOutputFolder = settings.CustomOutputDirectory ?? string.Empty;
        _selectedConflictResolution = settings.ConflictResolution;
        _maxConcurrency = settings.MaxConcurrency;
        _scanSubfolders = settings.ScanSubfolders;

        _conversionQueue.ProgressChanged += OnQueueProgressChanged;
        _conversionQueue.ItemCompleted += OnQueueItemCompleted;
        _conversionQueue.ItemFailed += OnQueueItemFailed;
    }

    partial void OnSelectedFormatChanged(ImageFormatDefinition value)
    {
        if (value != null)
        {
            if (value.SupportsLossyQuality && Quality <= 0)
            {
                Quality = value.DefaultQuality;
            }
            OnPropertyChanged(nameof(ShowQualityControl));
            OnPropertyChanged(nameof(ShowAlphaBackgroundControl));
            SaveCurrentSettings();
        }
    }

    partial void OnSelectedResizeModeChanged(ResizeMode value)
    {
        OnPropertyChanged(nameof(ShowCustomResizeInputs));
        OnPropertyChanged(nameof(ShowMaxDimensionDropdown));
        SaveCurrentSettings();
    }

    partial void OnSelectedOutputModeChanged(OutputDirectoryMode value)
    {
        OnPropertyChanged(nameof(ShowCustomFolderInput));
        SaveCurrentSettings();
    }

    [RelayCommand]
    public async Task AddFilesAsync(string[]? filePaths = null)
    {
        if (filePaths == null || filePaths.Length == 0)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Title = "Seleccionar imágenes",
                Filter = "Imágenes soportadas|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.tiff;*.tif;*.gif;*.ico;*.avif;*.heic|Todos los archivos|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                filePaths = dialog.FileNames;
            }
            else
            {
                return;
            }
        }

        await ProcessDiscoveredPathsAsync(filePaths);
    }

    [RelayCommand]
    public async Task AddFolderAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Seleccionar carpeta con imágenes",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            await ProcessDiscoveredPathsAsync(new[] { dialog.FolderName });
        }
    }

    public async Task ProcessDiscoveredPathsAsync(IEnumerable<string> paths)
    {
        StatusHeader = "Analizando archivos...";
        StatusDetail = "Buscando imágenes compatibles...";

        var existingPaths = new HashSet<string>(Items.Select(i => i.FilePath), StringComparer.OrdinalIgnoreCase);

        var progress = new Progress<int>(count =>
        {
            StatusDetail = $"Analizadas {count} imágenes...";
        });

        var scanned = await _imageScanner.ScanPathsAsync(paths, ScanSubfolders, progress);
        var addedCount = 0;

        foreach (var item in scanned)
        {
            if (!existingPaths.Contains(item.FilePath))
            {
                Items.Add(new ImageItemViewModel(item, _imageScanner));
                existingPaths.Add(item.FilePath);
                addedCount++;
            }
        }

        UpdateListStats();
        StatusHeader = "Listo para convertir";
        StatusDetail = addedCount > 0
            ? $"Se añadieron {addedCount} imágenes a la lista."
            : "No se encontraron nuevas imágenes compatibles.";
    }

    [RelayCommand]
    public void ClearList()
    {
        if (IsConverting) return;
        Items.Clear();
        HasCompletedBatch = false;
        SelectedItem = null;
        UpdateListStats();
        StatusHeader = "Listo para convertir";
        StatusDetail = "100 % local. Tus imágenes nunca salen de tu computadora.";
    }

    [RelayCommand]
    public void RemoveItem(ImageItemViewModel? item)
    {
        if (item != null && !IsConverting)
        {
            Items.Remove(item);
            if (SelectedItem == item)
            {
                SelectedItem = Items.FirstOrDefault();
            }
            UpdateListStats();
        }
    }

    [RelayCommand]
    public void ApplyPreset(PresetDefinition? preset)
    {
        if (preset == null) return;

        var format = _formatCatalog.GetFormatById(preset.TargetFormatId);
        if (format != null)
        {
            SelectedFormat = format;
        }
        Quality = preset.Quality;
        SelectedResizeMode = preset.ResizeMode;
        SelectedMaxDimension = preset.MaxDimension;
        SelectedMetadataOption = preset.MetadataOption;

        SaveCurrentSettings();
    }

    [RelayCommand]
    public async Task StartConversionAsync()
    {
        if (Items.Count == 0 || IsConverting) return;

        IsConverting = true;
        IsCancelling = false;
        HasCompletedBatch = false;
        OverallProgressPercentage = 0;

        BatchOriginalBytes = 0;
        BatchConvertedBytes = 0;
        BatchSavedBytes = 0;
        BatchSavingsPercentage = 0;
        BatchSuccessCount = 0;
        BatchErrorCount = 0;

        StatusHeader = $"Convirtiendo {Items.Count} imágenes...";
        StatusDetail = "Iniciando procesamiento local...";

        var options = new ConversionOptions
        {
            TargetFormat = SelectedFormat,
            Quality = Quality,
            ResizeMode = SelectedResizeMode,
            MaxDimension = SelectedMaxDimension,
            CustomWidth = CustomWidth,
            CustomHeight = CustomHeight,
            KeepAspectRatio = KeepAspectRatio,
            MetadataOption = SelectedMetadataOption,
            AlphaBackground = SelectedAlphaBackground,
            CustomAlphaBackgroundHex = CustomAlphaHex,
            OutputDirectoryMode = SelectedOutputMode,
            CustomOutputDirectory = CustomOutputFolder,
            ConflictResolution = SelectedConflictResolution,
            MaxConcurrency = MaxConcurrency,
            AutoOrient = true
        };

        // Determine destination folder to open later
        if (Items.Count > 0)
        {
            LastOutputDirectory = _fileNameResolver.DetermineOutputDirectory(Items[0].FilePath, options);
        }

        var models = Items.Select(i => i.Model).ToList();
        var results = await _conversionQueue.ExecuteQueueAsync(models, options);

        IsConverting = false;
        HasCompletedBatch = true;

        BatchSuccessCount = results.Count(r => r.Success);
        BatchErrorCount = results.Count(r => !r.Success);
        BatchOriginalBytes = results.Where(r => r.Success).Sum(r => r.OriginalBytes);
        BatchConvertedBytes = results.Where(r => r.Success).Sum(r => r.ConvertedBytes);
        BatchSavedBytes = Math.Max(0, BatchOriginalBytes - BatchConvertedBytes);
        BatchSavingsPercentage = BatchOriginalBytes > 0
            ? Math.Round((double)BatchSavedBytes / BatchOriginalBytes * 100.0, 1)
            : 0;

        // Sync ViewModels
        foreach (var item in Items)
        {
            item.SyncFromModel();
        }

        if (IsCancelling)
        {
            StatusHeader = "Conversión cancelada";
            StatusDetail = $"{BatchSuccessCount} convertidas, {Items.Count - BatchSuccessCount} canceladas/pendientes.";
        }
        else if (BatchErrorCount == 0)
        {
            StatusHeader = $"✓ {BatchSuccessCount} {(BatchSuccessCount == 1 ? "imagen convertida" : "imágenes convertidas")}";
            StatusDetail = $"Ahorro total: {ByteSizeConverter.FormatBytes(BatchSavedBytes)} ({BatchSavingsPercentage}%)";
        }
        else
        {
            StatusHeader = $"Completado con advertencias: {BatchSuccessCount} correctas, {BatchErrorCount} con errores";
            StatusDetail = "Revisa los elementos marcados en rojo en la lista.";
        }
    }

    [RelayCommand]
    public void CancelConversion()
    {
        if (!IsConverting || IsCancelling) return;
        IsCancelling = true;
        StatusDetail = "Cancelando operaciones pendientes...";
        _conversionQueue.Cancel();
    }

    [RelayCommand]
    public void OpenOutputFolder()
    {
        try
        {
            var dirToOpen = LastOutputDirectory;
            if (string.IsNullOrWhiteSpace(dirToOpen) || !Directory.Exists(dirToOpen))
            {
                if (SelectedOutputMode == OutputDirectoryMode.CustomFolder && Directory.Exists(CustomOutputFolder))
                {
                    dirToOpen = CustomOutputFolder;
                }
                else if (Items.Count > 0)
                {
                    var sourceDir = Path.GetDirectoryName(Items[0].FilePath);
                    dirToOpen = Path.Combine(sourceDir ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Converted");
                }
            }

            if (!string.IsNullOrWhiteSpace(dirToOpen) && Directory.Exists(dirToOpen))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dirToOpen,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed opening output folder", ex);
        }
    }

    [RelayCommand]
    public void BrowseCustomOutputFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Seleccionar carpeta de destino para imágenes convertidas",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            CustomOutputFolder = dialog.FolderName;
            SaveCurrentSettings();
        }
    }

    [RelayCommand]
    public void OpenLogsFolder()
    {
        try
        {
            var logDir = _logger.GetLogDirectoryPath();
            if (Directory.Exists(logDir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed opening logs folder", ex);
        }
    }

    [RelayCommand]
    public void ToggleAdvancedSettings()
    {
        IsAdvancedSettingsExpanded = !IsAdvancedSettingsExpanded;
    }

    private void OnQueueProgressChanged(object? sender, QueueProgressReport report)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            OverallProgressPercentage = report.ProgressPercentage;
            StatusHeader = $"Convirtiendo {report.CompletedItems + report.ErrorItems} de {report.TotalItems} ({Math.Round(report.ProgressPercentage)}%)";
            StatusDetail = $"{report.CurrentFileName} → {SelectedFormat.DisplayName}";

            // Sync item states
            foreach (var item in Items)
            {
                item.SyncFromModel();
            }
        });
    }

    private void OnQueueItemCompleted(object? sender, ConversionResult result)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var item = Items.FirstOrDefault(i => string.Equals(i.FilePath, result.SourceFilePath, StringComparison.OrdinalIgnoreCase));
            item?.SyncFromModel();
        });
    }

    private void OnQueueItemFailed(object? sender, ConversionResult result)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var item = Items.FirstOrDefault(i => string.Equals(i.FilePath, result.SourceFilePath, StringComparison.OrdinalIgnoreCase));
            item?.SyncFromModel();
        });
    }

    private void UpdateListStats()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasNoItems));
        OnPropertyChanged(nameof(TotalItemsCount));
        OnPropertyChanged(nameof(TotalInputSizeBytes));
        OnPropertyChanged(nameof(TotalInputSizeFormatted));
        OnPropertyChanged(nameof(TotalSummaryText));
    }

    private void SaveCurrentSettings()
    {
        var settings = new AppSettings
        {
            TargetFormatId = SelectedFormat?.Id ?? "webp",
            Quality = Quality,
            ResizeMode = SelectedResizeMode,
            MaxDimension = SelectedMaxDimension,
            KeepAspectRatio = KeepAspectRatio,
            MetadataOption = SelectedMetadataOption,
            AlphaBackground = SelectedAlphaBackground,
            CustomAlphaBackgroundHex = CustomAlphaHex,
            OutputDirectoryMode = SelectedOutputMode,
            CustomOutputDirectory = CustomOutputFolder,
            ConflictResolution = SelectedConflictResolution,
            MaxConcurrency = MaxConcurrency,
            ScanSubfolders = ScanSubfolders
        };

        _settingsService.SaveSettings(settings);
    }
}
