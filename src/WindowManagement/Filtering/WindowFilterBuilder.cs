namespace WindowManagement.Filtering;

public class WindowFilterBuilder
{
    private bool _altTabOnly = true;
    private string? _processName;
    private string? _titlePattern;
    private string? _className;
    private bool _includeMinimized = true;
    private Func<IWindow, bool>? _predicate;

    public WindowFilterBuilder Unfiltered()
    {
        _altTabOnly = false;
        return this;
    }

    public WindowFilterBuilder WithProcess(string processName)
    {
        _processName = processName;
        return this;
    }

    public WindowFilterBuilder WithTitle(string pattern)
    {
        _titlePattern = pattern;
        return this;
    }

    public WindowFilterBuilder WithClassName(string className)
    {
        _className = className;
        return this;
    }

    public WindowFilterBuilder ExcludeMinimized()
    {
        _includeMinimized = false;
        return this;
    }

    public WindowFilterBuilder Where(Func<IWindow, bool> predicate)
    {
        _predicate = predicate;
        return this;
    }

    public WindowFilter Build() => new()
    {
        AltTabOnly = _altTabOnly,
        ProcessName = _processName,
        TitlePattern = _titlePattern,
        ClassName = _className,
        IncludeMinimized = _includeMinimized,
        Predicate = _predicate
    };
}
