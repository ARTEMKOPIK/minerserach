using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSearch.Models;
using MSearch.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace MSearch.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IScannerService _scannerService;
    private readonly IQuarantineService _quarantineService;
    private readonly IUpdateService _updateService;
    private readonly IThemeService _themeService;
    private readonly ITrayIconService _trayIconService;
    private readonly IConfigService _configService;

    [ObservableProperty]
    private string _statusText = "Готов к сканированию";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private double _scanProgress;

    [ObservableProperty]
    private string _currentFile = "";

    [ObservableProperty]
    private int _filesScanned;

    [ObservableProperty]
    private int _threatsFound;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _appVersion = "1.5.0";

    [ObservableProperty]
    private string _signatureVersion = "1.0.0";

    [ObservableProperty]
    private ObservableCollection<ThreatInfo> _recentThreats = new();

    public MainViewModel(
        IScannerService scannerService,
        IQuarantineService quarantineService,
        IUpdateService updateService,
        IThemeService themeService,
        ITrayIconService trayIconService,
        IConfigService configService)
    {
        _scannerService = scannerService;
        _quarantineService = quarantineService;
        _updateService = updateService;
        _themeService = themeService;
        _trayIconService = trayIconService;
        _configService = configService;

        IsDarkTheme = _themeService.CurrentTheme == Theme.Dark;
        SignatureVersion = _updateService.CurrentDatabaseVersion;

        // Subscribe to events
        _scannerService.ProgressChanged += OnProgressChanged;
        _scannerService.ThreatDetected += OnThreatDetected;
        _scannerService.ScanCompleted += OnScanCompleted;

        // Initialize tray icon
        _trayIconService.Initialize();
        _trayIconService.TrayIconDoubleClicked += (s, e) => Application.Current.MainWindow?.Show();
        _trayIconService.ScanRequested += async (s, e) => await StartScanAsync();
        _trayIconService.OpenRequested += (s, e) => Application.Current.MainWindow?.Show();
        _trayIconService.ExitRequested += (s, e) => Application.Current.Shutdown();

        // Check for updates on startup
        _ = CheckForUpdatesAsync();
    }

    [RelayCommand]
    private async Task StartFullScan()
    {
        if (IsScanning) return;
        
        StatusText = "Запускаю полное сканирование...";
        IsScanning = true;
        RecentThreats.Clear();
        _trayIconService.SetScanningState(true);

        try
        {
            await _scannerService.StartScanAsync(ScanType.Full);
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка: {ex.Message}";
            App.Logger?.Error(ex, "Scan failed");
        }
    }

    [RelayCommand]
    private async Task StartQuickScan()
    {
        if (IsScanning) return;
        
        StatusText = "Запускаю быстрое сканирование...";
        IsScanning = true;
        RecentThreats.Clear();
        _trayIconService.SetScanningState(true);

        try
        {
            await _scannerService.StartScanAsync(ScanType.Quick);
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка: {ex.Message}";
            App.Logger?.Error(ex, "Scan failed");
        }
    }

    [RelayCommand]
    private void PauseScan()
    {
        _scannerService.PauseScan();
        StatusText = "Сканирование приостановлено";
    }

    [RelayCommand]
    private void ResumeScan()
    {
        _scannerService.ResumeScan();
        StatusText = "Сканирование возобновлено";
    }

    [RelayCommand]
    private void CancelScan()
    {
        _scannerService.CancelScan();
        StatusText = "Сканирование отменено";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        IsDarkTheme = _themeService.CurrentTheme == Theme.Dark;
    }

    [RelayCommand]
    private void OpenQuarantine()
    {
        // Will be handled by view
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // Will be handled by view
    }

    [RelayCommand]
    private void MinimizeToTray()
    {
        Application.Current.MainWindow?.Hide();
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var update = await _updateService.CheckForUpdatesAsync();
            if (update != null)
            {
                await _updateService.DownloadUpdateAsync(update);
                SignatureVersion = update.Version;
                _trayIconService.ShowNotification("Обновление", 
                    $"База сигнатур обновлена до версии {update.Version}");
            }
        }
        catch (Exception ex)
        {
            App.Logger?.Warning(ex, "Failed to check for updates");
        }
    }

    private void OnProgressChanged(object? sender, ScanProgress e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ScanProgress = e.PercentComplete;
            CurrentFile = e.CurrentFile;
            FilesScanned = e.FilesScanned;
            ThreatsFound = e.ThreatsFound;
            StatusText = $"Сканирование... {e.PercentComplete:F1}%";
        });
    }

    private void OnThreatDetected(object? sender, ThreatInfo e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            RecentThreats.Insert(0, e);
            if (RecentThreats.Count > 100)
                RecentThreats.RemoveAt(RecentThreats.Count - 1);
            
            _trayIconService.ShowNotification("Угроза обнаружена!", 
                $"{e.FileName}: {e.Description}", true);
        });
    }

    private void OnScanCompleted(object? sender, ScanResult e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsScanning = false;
            ScanProgress = 100;
            _trayIconService.SetScanningState(false);
            _trayIconService.SetThreatCount(e.ThreatsDetected);
            
            StatusText = e.ThreatsDetected > 0 
                ? $"Обнаружено угроз: {e.ThreatsDetected}" 
                : "Угроз не обнаружено";
        });
    }
}
