using WindowManagement.LowLevel.Internal;
using Xunit;

namespace WindowManagement.IntegrationTests.Helpers;

public class RequiresMixedResolutionFactAttribute : FactAttribute
{
    public RequiresMixedResolutionFactAttribute()
    {
        var displays = new DisplayApi().GetAll();
        if (displays.Count < 2 ||
            displays.Select(d => (d.Bounds.Width, d.Bounds.Height)).Distinct().Count() < 2)
            Skip = "Requires monitors with different resolutions";
    }
}
