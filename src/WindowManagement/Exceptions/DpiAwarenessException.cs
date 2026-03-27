namespace WindowManagement.Exceptions;

public class DpiAwarenessException : WindowManagementException
{
    public DpiAwarenessException()
        : base("Process must be configured for per-monitor v2 DPI awareness. " +
               "Set DpiAwareness in your app manifest or call SetProcessDpiAwarenessContext before creating WindowManager.")
    { }
}
