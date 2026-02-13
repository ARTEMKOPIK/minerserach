using MSearch;
using MSearch.Models;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace MSearch.Services;

public class ScannerService : IScannerService
{
    private readonly IConfigService _configService;
    private readonly IQuarantineService _quarantineService;
    
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isPaused;
    private readonly object _pauseLock = new();
    
    private static readonly int[] MiningPorts = { 1111, 1112, 2020, 3333, 4028, 4040, 4141, 4444, 5555, 6633, 6666, 7001, 7777, 9980, 9999, 10191, 10343, 14433, 20009 };
    
    private static readonly HashSet<string> SuspiciousProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "xmrig", "ccminer", "ethminer", "Claymore", "nicehash", "miner", "bfgminer",
        "sgminer", "cgminer", "asicminer", "minerd", "bitmain", "antminer"
    };

    public ScanState CurrentState { get; private set; } = ScanState.Idle;
    public event EventHandler<ScanProgressModel>? ProgressChanged;
    public event EventHandler<ThreatInfo>? ThreatDetected;
    public event EventHandler<ScanResultModel>? ScanCompleted;

    public ScannerService(IConfigService configService, IQuarantineService quarantineService)
    {
        _configService = configService;
        _quarantineService = quarantineService;
    }

    public async Task<ScanResultModel> StartScanAsync(ScanType type, string? customPath = null, CancellationToken cancellationToken = default)
    {
        if (CurrentState == ScanState.Scanning)
            throw new InvalidOperationException("Scan already in progress");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CurrentState = ScanState.Scanning;
        
        var result = new ScanResultModel
        {
            WasFullScan = type == ScanType.Full,
            ScanDate = DateTime.Now
        };

        var stopwatch = Stopwatch.StartNew();
        var progress = new ScanProgressModel { State = ScanState.Scanning };

        try
        {
            App.Logger?.Information("Starting {ScanType} scan", type);

            // Scan processes
            if (_configService.Settings.ScanProcesses)
            {
                await ScanProcessesAsync(progress, result, _cancellationTokenSource.Token);
            }

            // Scan files
            if (_configService.Settings.ScanFiles)
            {
                var scanPaths = type == ScanType.Custom && !string.IsNullOrEmpty(customPath)
                    ? new[] { customPath }
                    : new[] { Environment.GetFolderPath(Environment.SpecialFolder.Windows) };

                foreach (var path in scanPaths)
                {
                    await ScanDirectoryAsync(path, progress, result, _cancellationTokenSource.Token);
                }
            }

            // Scan registry
            if (_configService.Settings.ScanRegistry)
            {
                await ScanRegistryAsync(progress, result, _cancellationTokenSource.Token);
            }

            // Scan services
            if (_configService.Settings.ScanServices)
            {
                await ScanServicesAsync(progress, result, _cancellationTokenSource.Token);
            }

            // Scan hosts file
            if (_configService.Settings.ScanHostsFile)
            {
                await ScanHostsFileAsync(progress, result, _cancellationTokenSource.Token);
            }

            // Scan scheduled tasks
            if (_configService.Settings.ScanScheduledTasks)
            {
                await ScanScheduledTasksAsync(progress, result, _cancellationTokenSource.Token);
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            CurrentState = ScanState.Completed;
            progress.State = ScanState.Completed;

            App.Logger?.Information("Scan completed. Found {Threats} threats", result.ThreatsDetected);
        }
        catch (OperationCanceledException)
        {
            CurrentState = ScanState.Cancelled;
            progress.State = ScanState.Cancelled;
            App.Logger?.Information("Scan cancelled");
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Scan failed");
            throw;
        }
        finally
        {
            result.TotalFilesScanned = progress.FilesScanned;
            result.ThreatsDetected = progress.ThreatsFound;
            result.ThreatsQuarantined = progress.ThreatsQuarantined;
            result.Threats = progress.RecentThreats;
            result.Summary = GenerateSummary(result);
            
            ScanCompleted?.Invoke(this, result);
        }

        return result;
    }

    public void PauseScan()
    {
        lock (_pauseLock)
        {
            _isPaused = true;
            CurrentState = ScanState.Paused;
            App.Logger?.Information("Scan paused");
        }
    }

    public void ResumeScan()
    {
        lock (_pauseLock)
        {
            _isPaused = false;
            CurrentState = ScanState.Scanning;
            App.Logger?.Information("Scan resumed");
        }
    }

    public void CancelScan()
    {
        _cancellationTokenSource?.Cancel();
    }

    private async Task ScanProcessesAsync(ScanProgressModel progress, ScanResultModel result, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            foreach (var process in Process.GetProcesses())
            {
                ct.ThrowIfCancellationRequested();
                WaitIfPaused();

                try
                {
                    var processName = process.ProcessName.ToLower();
                    
                    // Check suspicious process names
                    if (SuspiciousProcessNames.Any(s => processName.Contains(s)))
                    {
                        var threat = new ThreatInfo
                        {
                            FileName = process.ProcessName,
                            FilePath = GetProcessPath(process),
                            Type = ThreatType.SuspiciousProcess,
                            Severity = ThreatSeverity.High,
                            Description = $"Suspicious process detected: {process.ProcessName}",
                            Details = new Dictionary<string, string>
                            {
                                ["PID"] = process.Id.ToString(),
                                ["Memory"] = $"{process.WorkingSet64 / 1024 / 1024} MB"
                            }
                        };

                        progress.ThreatsFound++;
                        progress.RecentThreats.Add(threat);
                        result.Threats.Add(threat);
                        ThreatDetected?.Invoke(this, threat);
                        
                        App.Logger?.Warning("Suspicious process detected: {ProcessName} (PID: {PID})", 
                            process.ProcessName, process.Id);
                    }

                    // Check process connections (mining ports)
                    try
                    {
                        var modules = process.Modules;
                        foreach (ProcessModule module in modules)
                        {
                            if (module.FileName.Contains("miner", StringComparison.OrdinalIgnoreCase))
                            {
                                var threat = new ThreatInfo
                                {
                                    FileName = process.ProcessName,
                                    FilePath = module.FileName,
                                    Type = ThreatType.Miner,
                                    Severity = ThreatSeverity.Critical,
                                    Description = $"Potential miner process: {process.ProcessName}"
                                };
                                
                                progress.ThreatsFound++;
                                progress.RecentThreats.Add(threat);
                                ThreatDetected?.Invoke(this, threat);
                            }
                        }
                    }
                    catch { }
                }
                catch { }
                finally
                {
                    process.Dispose();
                }
            }
        }, ct);
    }

    private async Task ScanDirectoryAsync(string path, ScanProgressModel progress, ScanResultModel result, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            try
            {
                var maxDepth = _configService.Settings.MaxScanDepth;
                ScanDirectoryRecursive(path, 0, maxDepth, progress, result, ct);
            }
            catch (Exception ex)
            {
                App.Logger?.Warning(ex, "Error scanning directory: {Path}", path);
            }
        }, ct);
    }

    private void ScanDirectoryRecursive(string path, int currentDepth, int maxDepth, 
        ScanProgressModel progress, ScanResultModel result, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        WaitIfPaused();

        if (currentDepth > maxDepth)
            return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                ct.ThrowIfCancellationRequested();
                WaitIfPaused();

                progress.CurrentFile = file;
                progress.FilesScanned++;
                
                if (progress.FilesScanned % 100 == 0)
                {
                    ProgressChanged?.Invoke(this, progress);
                }

                // Check file for suspicious content
                if (IsSuspiciousFile(file))
                {
                    var threat = new ThreatInfo
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        Type = ThreatType.SuspiciousFile,
                        Severity = ThreatSeverity.High,
                        Description = "Suspicious file detected"
                    };

                    progress.ThreatsFound++;
                    progress.RecentThreats.Add(threat);
                    result.Threats.Add(threat);
                    ThreatDetected?.Invoke(this, threat);
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                // Skip system directories
                if (dir.Contains("$Recycle.Bin") || 
                    dir.Contains("System Volume Information") ||
                    dir.Contains("Windows\\WinSxS"))
                    continue;

                ScanDirectoryRecursive(dir, currentDepth + 1, maxDepth, progress, result, ct);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (Exception ex)
        {
            App.Logger?.Debug(ex, "Error accessing: {Path}", path);
        }
    }

    private bool IsSuspiciousFile(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath).ToLower();
            
            // Check for known miner file names
            if (fileName.Contains("xmrig") || 
                fileName.Contains("miner") ||
                fileName.Contains("cryptonight"))
            {
                return true;
            }

            // Check file size (miners are usually small)
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < 10000 || fileInfo.Length > 50 * 1024 * 1024)
                return false;

            // Check for PE header and suspicious patterns
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            // Check MZ header
            if (stream.Length > 2)
            {
                var header = reader.ReadBytes(2);
                if (header[0] != 0x4D || header[1] != 0x5A) // "MZ"
                    return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task ScanRegistryAsync(ScanProgressModel progress, ScanResultModel result, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            var suspiciousKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"
            };

            foreach (var keyPath in suspiciousKeys)
            {
                ct.ThrowIfCancellationRequested();
                WaitIfPaused();

                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                    if (key == null) continue;

                    foreach (var valueName in key.GetValueNames())
                    {
                        var value = key.GetValue(valueName)?.ToString() ?? "";
                        
                        if (value.Contains("miner", StringComparison.OrdinalIgnoreCase) ||
                            value.Contains("xmrig", StringComparison.OrdinalIgnoreCase))
                        {
                            var threat = new ThreatInfo
                            {
                                FilePath = $"{keyPath}\\{valueName}",
                                FileName = valueName,
                                Type = ThreatType.SuspiciousRegistry,
                                Severity = ThreatSeverity.High,
                                Description = $"Suspicious registry entry: {valueName}",
                                Details = new Dictionary<string, string>
                                {
                                    ["Value"] = value
                                }
                            };

                            progress.ThreatsFound++;
                            progress.RecentThreats.Add(threat);
                            ThreatDetected?.Invoke(this, threat);
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Logger?.Debug(ex, "Error scanning registry: {KeyPath}", keyPath);
                }
            }
        }, ct);
    }

    private async Task ScanServicesAsync(ScanProgressModel progress, ScanResultModel result, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            try
            {
                var services = ServiceController.GetServices();
                
                foreach (var service in services)
                {
                    ct.ThrowIfCancellationRequested();
                    WaitIfPaused();

                    try
                    {
                        var serviceName = service.ServiceName.ToLower();
                        
                        if (serviceName.Contains("miner") || 
                            serviceName.Contains("xmrig") ||
                            serviceName.Contains("crypt"))
                        {
                            var threat = new ThreatInfo
                            {
                                FileName = service.DisplayName ?? service.ServiceName,
                                FilePath = service.ServiceName,
                                Type = ThreatType.SuspiciousService,
                                Severity = ThreatSeverity.High,
                                Description = $"Suspicious service detected: {service.ServiceName}",
                                Details = new Dictionary<string, string>
                                {
                                    ["Status"] = service.Status.ToString(),
                                    ["Type"] = service.ServiceType.ToString()
                                }
                            };

                            progress.ThreatsFound++;
                            progress.RecentThreats.Add(threat);
                            ThreatDetected?.Invoke(this, threat);
                        }
                    }
                    catch { }
                    finally
                    {
                        service.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger?.Warning(ex, "Error scanning services");
            }
        }, ct);
    }

    private async Task ScanHostsFileAsync(ScanProgressModel progress, ScanResultModel result, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            try
            {
                var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                
                if (!File.Exists(hostsPath))
                    return;

                var lines = File.ReadAllLines(hostsPath);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    WaitIfPaused();

                    var line = lines[i].Trim();
                    
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;

                    // Check for mining pool IPs
                    foreach (var port in MiningPorts)
                    {
                        if (line.Contains(port.ToString()))
                        {
                            var threat = new ThreatInfo
                            {
                                FilePath = hostsPath,
                                FileName = $"Line {i + 1}",
                                Type = ThreatType.SuspiciousNetwork,
                                Severity = ThreatSeverity.Medium,
                                Description = $"Suspicious hosts entry: {line}",
                                Details = new Dictionary<string, string>
                                {
                                    ["Line"] = line
                                }
                            };

                            progress.ThreatsFound++;
                            progress.RecentThreats.Add(threat);
                            ThreatDetected?.Invoke(this, threat);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger?.Warning(ex, "Error scanning hosts file");
            }
        }, ct);
    }

    private async Task ScanScheduledTasksAsync(ScanProgressModel progress, ScanResultModel result, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            try
            {
                using var taskService = new Microsoft.Win32.TaskScheduler.TaskService();
                var tasks = taskService.AllTasks;
                
                foreach (Microsoft.Win32.TaskScheduler.Task task in tasks)
                {
                    ct.ThrowIfCancellationRequested();
                    WaitIfPaused();

                    try
                    {
                        string taskName = task.Name.ToLower();
                        
                        if (taskName.Contains("miner") || 
                            taskName.Contains("xmrig") ||
                            taskName.Contains("crypt"))
                        {
                            var threat = new ThreatInfo
                            {
                                FileName = task.Name,
                                FilePath = task.Path,
                                Type = ThreatType.SuspiciousProcess,
                                Severity = ThreatSeverity.High,
                                Description = $"Suspicious scheduled task: {task.Name}",
                                Details = new Dictionary<string, string>
                                {
                                    ["State"] = task.State.ToString(),
                                    ["LastRunTime"] = task.LastRunTime.ToString()
                                }
                            };

                            progress.ThreatsFound++;
                            progress.RecentThreats.Add(threat);
                            ThreatDetected?.Invoke(this, threat);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                App.Logger?.Warning(ex, "Error scanning scheduled tasks");
            }
        }, ct);
    }

    private void WaitIfPaused()
    {
        lock (_pauseLock)
        {
            while (_isPaused && CurrentState == ScanState.Paused)
            {
                Monitor.Wait(_pauseLock);
            }
        }
    }

    private string GetProcessPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? "";
        }
        catch
        {
            return "";
        }
    }

    private string GenerateSummary(ScanResultModel result)
    {
        return $"Scan completed in {result.Duration.TotalSeconds:F1} seconds. " +
               $"Found {result.ThreatsDetected} threats. " +
               $"Scanned {result.TotalFilesScanned} files.";
    }
}
