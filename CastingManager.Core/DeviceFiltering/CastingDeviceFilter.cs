namespace CastingManager.Core.DeviceFiltering;

public static class CastingDeviceFilter
{
    public static bool ShouldInclude(bool? supportsVideo)
        => supportsVideo ?? false;
}
