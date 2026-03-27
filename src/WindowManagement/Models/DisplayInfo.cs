namespace WindowManagement.LowLevel;

public record DisplayInfo(
    nint Handle,
    string DeviceName,
    string DisplayName,
    bool IsPrimary,
    WindowRect Bounds,
    WindowRect WorkArea,
    int Dpi,
    double ScaleFactor);
