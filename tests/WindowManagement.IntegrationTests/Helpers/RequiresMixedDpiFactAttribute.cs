using WindowManagement.LowLevel.Internal;
using Xunit;

namespace WindowManagement.IntegrationTests.Helpers;

public class RequiresMixedDpiFactAttribute : FactAttribute
{
    public RequiresMixedDpiFactAttribute()
    {
        var displays = new DisplayApi().GetAll();
        if (displays.Count < 2 || displays.Select(d => d.Dpi).Distinct().Count() < 2)
            Skip = "Requires monitors with different DPI values";
    }
}
