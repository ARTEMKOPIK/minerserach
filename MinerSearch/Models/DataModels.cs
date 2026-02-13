namespace MSearch.Models;

public class ThreatInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public ThreatType Type { get; set; }
    public ThreatSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.Now;
    public QuarantineStatus Status { get; set; } = QuarantineStatus.None;
    public string? QuarantinePath { get; set; }
    public bool IsAutoDetected { get; set; } = true;
    public Dictionary<string, string> Details { get; set; } = new();
}

public enum QuarantineStatus
{
    None,
    Detected,
    Quarantined,
    Restored,
    Deleted
}

public class ScanProgress
{
    public int FilesScanned { get; set; }
    public int ThreatsFound { get; set; }
    public int ThreatsQuarantined { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public double PercentComplete { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan EstimatedRemaining { get; set; }
    public ScanState State { get; set; }
    public List<ThreatInfo> RecentThreats { get; set; } = new();
}

public class ScanResult
{
    public DateTime ScanDate { get; set; } = DateTime.Now;
    public TimeSpan Duration { get; set; }
    public int TotalFilesScanned { get; set; }
    public int ThreatsDetected { get; set; }
    public int ThreatsQuarantined { get; set; }
    public int ThreatsDeleted { get; set; }
    public int ThreatsRestored { get; set; }
    public List<ThreatInfo> Threats { get; set; } = new();
    public bool WasFullScan { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class QuarantineItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OriginalPath { get; set; } = string.Empty;
    public string QuarantinePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public ThreatType ThreatType { get; set; }
    public DateTime QuarantinedAt { get; set; } = DateTime.Now;
    public string? OriginalHash { get; set; }
    public string? Notes { get; set; }
}

public class AppSettings
{
    public string Language { get; set; } = "EN";
    public Theme Theme { get; set; } = Theme.Light;
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool AutoScanOnStartup { get; set; } = false;
    public bool AutoUpdateSignatures { get; set; } = true;
    public int MaxScanDepth { get; set; } = 8;
    public bool ScanProcesses { get; set; } = true;
    public bool ScanFiles { get; set; } = true;
    public bool ScanRegistry { get; set; } = true;
    public bool ScanServices { get; set; } = true;
    public bool ScanScheduledTasks { get; set; } = true;
    public bool ScanHostsFile { get; set; } = true;
    public bool ScanFirewallRules { get; set; } = true;
    public string? CustomScanPath { get; set; }
    public string TelegramBotToken { get; set; } = string.Empty;
    public List<string> TelegramAuthorizedUsers { get; set; } = new();
}

public class SignatureDatabase
{
    public string Version { get; set; } = string.Empty;
    public DateTime LastUpdate { get; set; }
    public string MinAppVersion { get; set; } = string.Empty;
    public List<DomainSignature> Domains { get; set; } = new();
    public List<FileSignature> FileHashes { get; set; } = new();
    public List<ProcessSignature> ProcessNames { get; set; } = new();
    public List<int> Ports { get; set; } = new();
}

public class DomainSignature
{
    public string Hash { get; set; } = string.Empty;
    public string Original { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime AddedDate { get; set; }
}

public class FileSignature
{
    public string Hash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

public class ProcessSignature
{
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string Type { get; set; } = string.Empty;
}
