using Microsoft.AspNetCore.SignalR;
using MinerSearch.Web.Services;

namespace MinerSearch.Web.Hubs;

public class ScanHub : Hub
{
    private readonly ScanHubService _scanService;
    private readonly ILogger<ScanHub> _logger;

    public ScanHub(ScanHubService scanService, ILogger<ScanHub> logger)
    {
        _scanService = scanService;
        _logger = logger;
    }

    public async Task StartScan(string scanType, string? customPath = null)
    {
        _logger.LogInformation("Starting scan: {ScanType}, Path: {Path}", scanType, customPath);
        await _scanService.StartScanAsync(Context.ConnectionId, scanType, customPath);
    }

    public async Task StopScan()
    {
        _logger.LogInformation("Stopping scan");
        await _scanService.StopScanAsync();
    }

    public async Task GetStatus()
    {
        var status = _scanService.GetStatus();
        await Clients.Caller.SendAsync("StatusUpdate", status);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
