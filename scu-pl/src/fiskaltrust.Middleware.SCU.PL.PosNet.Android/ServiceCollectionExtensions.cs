using Android.Content;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Android;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Lets the PosNet SCU open a <c>usbhost://</c> DeviceUrl through the Android USB host API.
    /// Register it next to <see cref="ScuBootstrapper.ConfigureServices"/>; the order does not matter.
    /// </summary>
    public static IServiceCollection AddPosNetAndroidUsbHost(this IServiceCollection services, Context context)
        => services.AddSingleton<IPosNetUsbHostLinkFactory>(new AndroidUsbHostLinkFactory(context));
}
