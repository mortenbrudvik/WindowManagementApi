using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace WindowManagement.IntegrationTests;

internal static class ModuleInit
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
    }
}
