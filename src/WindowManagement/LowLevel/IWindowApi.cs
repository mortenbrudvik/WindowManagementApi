namespace WindowManagement.LowLevel;

public interface IWindowApi
{
    nint GetForeground();
    IReadOnlyList<nint> Enumerate(bool altTabOnly = true);

    string GetTitle(nint hwnd);
    string GetClassName(nint hwnd);
    int GetProcessId(nint hwnd);
    WindowRect GetBounds(nint hwnd);
    WindowRect GetClientBounds(nint hwnd);
    WindowState GetState(nint hwnd);
    bool IsVisible(nint hwnd);
    bool IsTopmost(nint hwnd);
    bool IsValid(nint hwnd);
    bool IsResizable(nint hwnd);
    (int left, int top, int right, int bottom) GetInvisibleBorders(nint hwnd);
    uint GetDpi(nint hwnd);

    void Move(nint hwnd, int x, int y);
    void Resize(nint hwnd, int width, int height);
    void SetBounds(nint hwnd, WindowRect bounds);
    void SetState(nint hwnd, WindowState state);
    void Focus(nint hwnd);
    void SetTopmost(nint hwnd, bool topmost);
    bool RestoreMinimized(nint hwnd);
    void BringToFront(nint hwnd);
}
