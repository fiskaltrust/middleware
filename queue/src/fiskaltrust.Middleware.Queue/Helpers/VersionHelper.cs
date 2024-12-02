namespace fiskaltrust.Middleware.Queue.Helpers;

public static class VersionHelper
{
    private static readonly string _currentMiddlewareVersion;

    static VersionHelper()
    {
        var version = typeof(VersionHelper).Assembly.GetName().Version;
        _currentMiddlewareVersion = version?.ToString();
    }
    
    public static string GetCurrentMiddlewareVersion() => _currentMiddlewareVersion;
}
