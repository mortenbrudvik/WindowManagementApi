using WindowManagement;

namespace WindowManager.Demo.Models;

public record SnapRequest(IMonitor Monitor, SnapPosition Position);
