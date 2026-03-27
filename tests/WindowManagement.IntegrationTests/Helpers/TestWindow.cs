using System.Windows.Forms;

namespace WindowManagement.IntegrationTests.Helpers;

public class TestWindow : IDisposable
{
    private readonly Form _form;
    private readonly nint _handle;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new();
    private bool _disposed;

    private TestWindow(Options options)
    {
        Form? form = null;

        _thread = new Thread(() =>
        {
            form = new Form
            {
                Text = options.Title,
                Width = options.Width,
                Height = options.Height,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(options.X, options.Y),
                ShowInTaskbar = true,
            };

            if (!options.Resizable)
            {
                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.MaximizeBox = false;
            }

            form.Shown += (_, _) => _ready.Set();
            Application.Run(form);
        });

        _thread.SetApartmentState(ApartmentState.STA);
        _thread.IsBackground = true;
        _thread.Start();

        if (!_ready.Wait(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("TestWindow failed to become ready within 5 seconds.");

        _form = form!;
        _handle = _form.Handle;

        // Allow window to fully render and become queryable by Win32 APIs
        Thread.Sleep(200);
    }

    public static TestWindow Create(Action<Options>? configure = null)
    {
        var options = new Options();
        configure?.Invoke(options);
        return new TestWindow(options);
    }

    public nint Handle => _handle;

    public string Title => Invoke(() => _form.Text);

    public WindowRect Bounds
    {
        get
        {
            var b = Invoke(() => new { _form.Left, _form.Top, _form.Width, _form.Height });
            return new WindowRect(b.Left, b.Top, b.Width, b.Height);
        }
    }

    public void SetTitle(string title) => Invoke(() => _form.Text = title);

    public void SetPosition(int x, int y) =>
        Invoke(() => _form.Location = new System.Drawing.Point(x, y));

    public void SetSize(int width, int height) =>
        Invoke(() => _form.Size = new System.Drawing.Size(width, height));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_form.IsHandleCreated)
        {
            _form.Invoke(() => _form.Close());
        }

        _thread.Join(TimeSpan.FromSeconds(3));
        _ready.Dispose();
    }

    private T Invoke<T>(Func<T> action)
    {
        if (_form.InvokeRequired)
            return (T)_form.Invoke(action);
        return action();
    }

    private void Invoke(Action action)
    {
        if (_form.InvokeRequired)
            _form.Invoke(action);
        else
            action();
    }

    public class Options
    {
        internal string Title { get; private set; } = "TestWindow";
        internal int Width { get; private set; } = 400;
        internal int Height { get; private set; } = 300;
        internal int X { get; private set; } = 100;
        internal int Y { get; private set; } = 100;
        internal bool Resizable { get; private set; } = true;

        public Options WithTitle(string title) { Title = title; return this; }
        public Options WithSize(int width, int height) { Width = width; Height = height; return this; }
        public Options WithPosition(int x, int y) { X = x; Y = y; return this; }
        public Options NonResizable() { Resizable = false; return this; }
    }
}
