using System;
using System.Reflection;

namespace fiskaltrust.Middleware.Queue.Helpers;

public static class VersionHelper
{
    public static string GetCurrentMiddlewareVersion(Type assemblyType)
    {
        assemblyType = assemblyType ?? typeof(VersionHelper);
        var assemblyName = assemblyType.Assembly.GetName();
        var versionAttribute = assemblyType.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split(new char[] { '+', '-' })[0];
        var version = Version.TryParse(versionAttribute, out var result)
            ? new Version(result.Major, result.Minor, result.Build, 0)
        : new Version(assemblyName.Version.Major, assemblyName.Version.Minor, assemblyName.Version.Build, 0);
        
        return version?.ToString();
    } 
}
