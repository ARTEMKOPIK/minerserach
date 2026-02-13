using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MSearch.Services;

public class TrayIconService : ITrayIconService
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private bool _isScanning;
    private int _threatCount;
    
    public event EventHandler? TrayIconDoubleClicked;
    public event EventHandler? ScanRequested;
    public event EventHandler? OpenRequested;
    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Открыть MinerSearch", null, (s, e) => OpenRequested?.Invoke(this, EventArgs.Empty));
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Запустить сканирование", null, (s, e) => ScanRequested?.Invoke(this, EventArgs.Empty));
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Выход", null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = "MinerSearch",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.DoubleClick += (s, e) => TrayIconDoubleClicked?.Invoke(this, EventArgs.Empty);
        
        App.Logger?.Information("Tray icon initialized");
    }

    public void ShowNotification(string title, string message, bool isWarning = false)
    {
        if (_notifyIcon == null) return;

        Application.Current?.Dispatcher.Invoke(() =>
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = isWarning 
                ? ToolTipIcon.Warning 
                : ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(5000);
        });
    }

    public void SetScanningState(bool isScanning)
    {
        _isScanning = isScanning;
        UpdateTooltip();
    }

    public void SetThreatCount(int count)
    {
        _threatCount = count;
        UpdateTooltip();
    }

    public void UpdateTooltip(string? text = null)
    {
        if (_notifyIcon == null) return;

        var tooltip = "MinerSearch";
        
        if (_isScanning)
            tooltip += " - Сканирование...";
        else if (_threatCount > 0)
            tooltip += $" - {_threatCount} угроз";
        else
            tooltip += " - Защищено";
        
        if (!string.IsNullOrEmpty(text))
            tooltip = text;

        _notifyIcon.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        
        _contextMenu?.Dispose();
        
        App.Logger?.Information("Tray icon disposed");
    }
}
