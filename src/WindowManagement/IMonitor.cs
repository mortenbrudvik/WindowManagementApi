namespace WindowManagement;

public interface IMonitor
{
    nint Handle { get; }
    string DeviceName { get; }
    string DisplayName { get; }
    bool IsPrimary { get; }
    WindowRect Bounds { get; }
    WindowRect WorkArea { get; }
    int Dpi { get; }
    double ScaleFactor { get; }
}
