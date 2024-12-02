namespace fiskaltrust.Middleware.Queue.Helpers;

public class VersionHelper
{
    public static string GetCurrentMiddlewareVersion()
    {
        var version = typeof(SignProcessor).Assembly.GetName().Version;
        return version?.ToString();
    }
}