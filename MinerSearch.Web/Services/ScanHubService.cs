using Microsoft.AspNetCore.SignalR;
using MSearch;
using MSearch.Models;

namespace MinerSearch.Web.Services;

public class ScanHubService
{
    private readonly IHubContext<Hubs.ScanHub> _hubContext;
    private ScanState _currentState = ScanState.Idle;
    private ScanProgressModel _lastProgress = new();

    public ScanHubService(IHubContext<Hubs.ScanHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task StartScanAsync(string connectionId, string scanType, string? customPath)
    {
        _currentState = ScanState.Scanning;
        
        // Simulate scan progress for demo
        // In production, this would connect to the actual scanner
        for (int i = 0; i <= 100; i += 10)
        {
            if (_currentState != ScanState.Scanning)
                break;

            var progress = new ScanProgressModel
            {
                FilesScanned = i * 100,
                ThreatsFound = i > 50 ? 2 : 0,
                PercentComplete = i,
                CurrentFile = $"C:\\Windows\\System32\\file_{i}.dll",
                State = ScanState.Scanning
            };

            _lastProgress = progress;
            await _hubContext.Clients.Client(connectionId).SendAsync("ScanProgress", progress);
            
            await Task.Delay(500);
        }

        _currentState = ScanState.Completed;
        var result = new ScanResultModel
        {
            ScanDate = DateTime.Now,
            Duration = TimeSpan.FromSeconds(5),
            TotalFilesScanned = _lastProgress.FilesScanned,
            ThreatsDetected = _lastProgress.ThreatsFound,
            Summary = "Scan completed successfully"
        };

        await _hubContext.Clients.Client(connectionId).SendAsync("ScanCompleted", result);
    }

    public async Task StopScanAsync()
    {
        _currentState = ScanState.Cancelled;
        await Task.CompletedTask;
    }

    public object GetStatus()
    {
        return new
        {
            State = _currentState.ToString(),
            Progress = _lastProgress
        };
    }
}
