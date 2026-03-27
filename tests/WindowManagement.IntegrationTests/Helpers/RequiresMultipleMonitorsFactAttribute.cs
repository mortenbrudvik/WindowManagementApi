using WindowManagement.LowLevel.Internal;
using Xunit;

namespace WindowManagement.IntegrationTests.Helpers;

public class RequiresMultipleMonitorsFactAttribute : FactAttribute
{
    public RequiresMultipleMonitorsFactAttribute()
    {
        var displays = new DisplayApi().GetAll();
        if (displays.Count < 2)
            Skip = "Requires multiple monitors";
    }
}
