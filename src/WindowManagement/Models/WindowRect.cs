namespace WindowManagement;

public record WindowRect
{
    private readonly int _width;
    private readonly int _height;

    public int X { get; init; }
    public int Y { get; init; }

    public int Width
    {
        get => _width;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _width = value;
        }
    }

    public int Height
    {
        get => _height;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _height = value;
        }
    }

    public int Right => X + Width;
    public int Bottom => Y + Height;

    public WindowRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public void Deconstruct(out int x, out int y, out int width, out int height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }
}
