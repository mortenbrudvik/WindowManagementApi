namespace WindowManagement.Filtering;

public class WindowFilter
{
    public bool AltTabOnly { get; init; } = true;
    public string? ProcessName { get; init; }
    public string? TitlePattern { get; init; }
    public string? ClassName { get; init; }
    public bool IncludeMinimized { get; init; } = true;
    public Func<IWindow, bool>? Predicate { get; init; }
}
