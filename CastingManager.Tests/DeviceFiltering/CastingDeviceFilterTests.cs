using CastingManager.Core.DeviceFiltering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CastingManager.Tests.DeviceFiltering;

[TestClass]
public class CastingDeviceFilterTests
{
    [TestMethod]
    public void ShouldInclude_ReturnsTrue_WhenSupportsVideoIsTrue()
    {
        Assert.IsTrue(CastingDeviceFilter.ShouldInclude(true));
    }

    [TestMethod]
    public void ShouldInclude_ReturnsFalse_WhenSupportsVideoIsFalse()
    {
        Assert.IsFalse(CastingDeviceFilter.ShouldInclude(false));
    }

    [TestMethod]
    public void ShouldInclude_ReturnsFalse_WhenSupportsVideoIsNull()
    {
        Assert.IsFalse(CastingDeviceFilter.ShouldInclude(null));
    }
}
