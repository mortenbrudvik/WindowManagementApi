namespace WindowManagement.Internal;

internal static class SnapCalculator
{
    public static WindowRect Calculate(WindowRect workArea, SnapPosition position)
    {
        var halfWidth = workArea.Width / 2;
        var halfHeight = workArea.Height / 2;

        return position switch
        {
            SnapPosition.Fill => workArea,

            SnapPosition.Left => new WindowRect(
                workArea.X, workArea.Y, halfWidth, workArea.Height),

            SnapPosition.Right => new WindowRect(
                workArea.X + halfWidth, workArea.Y, workArea.Width - halfWidth, workArea.Height),

            SnapPosition.Top => new WindowRect(
                workArea.X, workArea.Y, workArea.Width, halfHeight),

            SnapPosition.Bottom => new WindowRect(
                workArea.X, workArea.Y + halfHeight, workArea.Width, workArea.Height - halfHeight),

            SnapPosition.TopLeft => new WindowRect(
                workArea.X, workArea.Y, halfWidth, halfHeight),

            SnapPosition.TopRight => new WindowRect(
                workArea.X + halfWidth, workArea.Y, workArea.Width - halfWidth, halfHeight),

            SnapPosition.BottomLeft => new WindowRect(
                workArea.X, workArea.Y + halfHeight, halfWidth, workArea.Height - halfHeight),

            SnapPosition.BottomRight => new WindowRect(
                workArea.X + halfWidth, workArea.Y + halfHeight, workArea.Width - halfWidth, workArea.Height - halfHeight),

            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
        };
    }
}
