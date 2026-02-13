namespace MSearch;

public enum Theme
{
    Light,
    Dark
}

public enum ScanType
{
    Full,
    Quick,
    Custom
}

public enum ScanState
{
    Idle,
    Scanning,
    Paused,
    Completed,
    Cancelled
}

public enum ThreatSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ThreatType
{
    Miner,
    Rootkit,
    SuspiciousProcess,
    SuspiciousFile,
    SuspiciousRegistry,
    SuspiciousService,
    SuspiciousNetwork,
    Adware,
    Trojan,
    Worm,
    Other
}
