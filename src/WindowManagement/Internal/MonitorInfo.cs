namespace WindowManagement.Internal;

internal class MonitorInfo : IMonitor
{
    public required nint Handle { get; init; }
    public required string DeviceName { get; init; }
    public required string DisplayName { get; init; }
    public required bool IsPrimary { get; init; }
    public required WindowRect Bounds { get; init; }
    public required WindowRect WorkArea { get; init; }
    public required int Dpi { get; init; }
    public required double ScaleFactor { get; init; }
}
