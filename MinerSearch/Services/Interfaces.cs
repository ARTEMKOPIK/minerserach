using MSearch.Models;

namespace MSearch.Services;

public interface IScannerService
{
    event EventHandler<ScanProgress>? ProgressChanged;
    event EventHandler<ThreatInfo>? ThreatDetected;
    event EventHandler<ScanResult>? ScanCompleted;

    ScanState CurrentState { get; }
    Task<ScanResult> StartScanAsync(ScanType type, string? customPath = null, CancellationToken cancellationToken = default);
    void PauseScan();
    void ResumeScan();
    void CancelScan();
}

public interface IQuarantineService
{
    Task<List<QuarantineItem>> GetQuarantineItemsAsync();
    Task<bool> QuarantineFileAsync(ThreatInfo threat);
    Task<bool> RestoreFileAsync(string quarantineId);
    Task<bool> DeleteFileAsync(string quarantineId);
    Task<bool> DeleteAllAsync();
}

public interface IUpdateService
{
    event EventHandler<SignatureDatabase>? DatabaseUpdated;

    Task<SignatureDatabase?> CheckForUpdatesAsync();
    Task<bool> DownloadUpdateAsync(SignatureDatabase database);
    string CurrentDatabaseVersion { get; }
    DateTime LastUpdateCheck { get; }
}

public interface IThemeService
{
    Theme CurrentTheme { get; }
    event EventHandler<Theme>? ThemeChanged;
    void SetTheme(Theme theme);
    void ToggleTheme();
    void ApplySystemTheme();
}

public interface ITelegramBotService
{
    event EventHandler<string>? MessageReceived;
    event EventHandler<ThreatInfo>? ThreatNotification;

    bool IsRunning { get; }
    Task StartAsync(string token);
    Task StopAsync();
    Task SendMessageAsync(long chatId, string message);
    Task SendScanProgressAsync(long chatId, ScanProgress progress);
    Task SendThreatNotificationAsync(long chatId, ThreatInfo threat);
}

public interface ITrayIconService : IDisposable
{
    event EventHandler? TrayIconDoubleClicked;
    event EventHandler? ScanRequested;
    event EventHandler? OpenRequested;
    event EventHandler? ExitRequested;

    void Initialize();
    void ShowNotification(string title, string message, bool isWarning = false);
    void SetScanningState(bool isScanning);
    void SetThreatCount(int count);
    void UpdateTooltip(string text);
}

public interface IConfigService
{
    AppSettings Settings { get; }
    void Load();
    void Save();
    T GetValue<T>(string key, T defaultValue);
    void SetValue<T>(string key, T value);
}
