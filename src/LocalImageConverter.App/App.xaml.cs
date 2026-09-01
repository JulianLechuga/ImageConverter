using System.IO;
using System.Windows;
using LocalImageConverter.App.ViewModels;
using LocalImageConverter.Core.Services;

namespace LocalImageConverter.App;

public partial class App : Application
{
    private ILoggerService? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup Logger
        _logger = new FileLoggerService();
        _logger.LogInfo("=== Local Image Converter iniciado ===");

        // Setup Global Exception Handlers
        SetupExceptionHandling();

        // Ensure App Icon exists
        EnsureAppIcon();

        // Initialize Services & ViewModel
        var catalog = new ImageFormatCatalog();
        var scanner = new ImageScanner(catalog, _logger);
        var fileNameResolver = new FileNameResolver();
        var converter = new ImageMagickConverter(fileNameResolver, _logger);
        var queue = new ConversionQueue(converter, _logger);
        var settingsService = new SettingsService(_logger);

        var mainViewModel = new MainViewModel(
            catalog,
            scanner,
            fileNameResolver,
            queue,
            settingsService,
            _logger);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
    }

    private void EnsureAppIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
            if (!File.Exists(iconPath))
            {
                AppIconGenerator.GenerateAppIcon(iconPath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Could not ensure icon: {ex.Message}");
        }
    }

    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            _logger?.LogError("Unhandled AppDomain Exception", ex);
            MessageBox.Show(
                "Ocurrió un error inesperado en la aplicación.\nSe ha registrado el detalle técnico en los logs.",
                "Local Image Converter",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        };

        DispatcherUnhandledException += (s, e) =>
        {
            _logger?.LogError("Unhandled Dispatcher Exception", e.Exception);
            MessageBox.Show(
                $"Ocurrió un error en la interfaz: {e.Exception.Message}\nSe ha registrado el detalle en los logs.",
                "Local Image Converter",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            _logger?.LogError("Unobserved Task Exception", e.Exception);
            e.SetObserved();
        };
    }
}
