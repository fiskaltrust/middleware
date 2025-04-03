using System;
using System.Reflection;

namespace fiskaltrust.Middleware.Queue.Helpers;

public static class VersionHelper
{
    public static string GetCurrentMiddlewareVersion(Type assemblyType)
    {
        if(assemblyType == null)
        {
            return typeof(VersionHelper).Assembly.GetName().Version?.ToString();
        }
        var assemblyName = assemblyType.Assembly.GetName();
        var versionAttribute = assemblyType.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split(new char[] { '+', '-' })[0];
        var version = Version.TryParse(versionAttribute, out var result)
            ? new Version(result.Major, result.Minor, result.Build, 0)
        : new Version(assemblyName.Version.Major, assemblyName.Version.Minor, assemblyName.Version.Build, 0);
        
        return version.ToString();
    } 
}
