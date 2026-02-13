using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSearch.Core;
using MSearch.Infrastructure;
using MSearch.Services;
using MSearch.UI.ViewModels;
using MSearch.UI.Views;
using Serilog;
using System.IO;
using System.Windows;

namespace MSearch;

public partial class App : Application
{
    private static IServiceProvider? _serviceProvider;
    public static IServiceProvider ServiceProvider => _serviceProvider!;

    public static ILoggerFactory LoggerFactory { get; private set; } = null!;
    public static Serilog.ILogger Logger { get; private set; } = null!;

    public static Theme CurrentTheme { get; set; } = Theme.Light;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureLogging();
        ConfigureServices();
        ConfigureExceptionHandling();
        
        Logger.Information("MinerSearch v{Version} started", GetType().Assembly.GetName().Version);

        LoadSettings();
    }

    private void ConfigureLogging()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinerSearch", "Logs", "minersearch-.log");

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Logger = Log.Logger;
        LoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(Log.Logger);
        });
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core Services
        services.AddSingleton<IScannerService, ScannerService>();
        services.AddSingleton<IQuarantineService, QuarantineService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ITelegramBotService, TelegramBotService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<IConfigService, ConfigService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ScanViewModel>();
        services.AddTransient<QuarantineViewModel>();
        services.AddTransient<SettingsViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Logger.Fatal(ex, "Unhandled domain exception");
            MessageBox.Show($"Critical error: {ex?.Message}", "MinerSearch Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Logger.Error(args.Exception, "Unhandled dispatcher exception");
            MessageBox.Show($"Error: {args.Exception.Message}", "MinerSearch Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Logger.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    private void LoadSettings()
    {
        var configService = ServiceProvider.GetRequiredService<IConfigService>();
        configService.Load();

        var theme = configService.GetValue<string>("theme", "Light");
        CurrentTheme = theme == "Dark" ? Theme.Dark : Theme.Light;

        var themeService = ServiceProvider.GetRequiredService<IThemeService>();
        themeService.SetTheme(CurrentTheme);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Information("MinerSearch shutting down");
        
        var trayService = ServiceProvider.GetService<ITrayIconService>();
        trayService?.Dispose();
        
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
