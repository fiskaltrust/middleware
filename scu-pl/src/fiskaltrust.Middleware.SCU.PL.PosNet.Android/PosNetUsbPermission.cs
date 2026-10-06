using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Android;

/// <summary>
/// Asks the user for access to the printer, which Android requires before an app may open a USB
/// device. Call it from the app's foreground, before the first receipt — the SCU itself only reports
/// a missing permission, as an unreachable printer.
/// </summary>
/// <remarks>
/// An app that declares a <c>USB_DEVICE_ATTACHED</c> intent filter with a device filter for the
/// printer gets the permission when the user accepts the prompt shown on plugging it in, and need
/// not ask here.
/// </remarks>
public static class PosNetUsbPermission
{
    /// <returns>Whether access is granted; <c>false</c> also when the device is not attached.</returns>
    public static Task<bool> RequestAsync(Context context, PosNetDeviceAddress.UsbHost address)
    {
        var manager = UsbDevices.Manager(context);
        var device = UsbDevices.Find(manager, address);
        if (device is null)
        {
            return Task.FromResult(false);
        }
        if (manager.HasPermission(device))
        {
            return Task.FromResult(true);
        }

        var action = $"{context.PackageName}.POSNET_USB_PERMISSION";
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiver = new PermissionReceiver(result);
        var filter = new IntentFilter(action);
        // Android 14 refuses a runtime receiver that does not say whether it is exported.
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            context.RegisterReceiver(receiver, filter, ReceiverFlags.NotExported);
        }
        else
        {
            context.RegisterReceiver(receiver, filter);
        }

        // The system fills in the result extras, so the intent must be mutable (Android 12+), and
        // explicit through its package so that a mutable intent is allowed at all (Android 14+).
        var intent = new Intent(action).SetPackage(context.PackageName);
        var flags = OperatingSystem.IsAndroidVersionAtLeast(31) ? PendingIntentFlags.Mutable : 0;
        manager.RequestPermission(device, PendingIntent.GetBroadcast(context, 0, intent, flags));

        return result.Task.ContinueWith(
            granted =>
            {
                context.UnregisterReceiver(receiver);
                return granted.Result;
            },
            TaskScheduler.Default);
    }

    private sealed class PermissionReceiver(TaskCompletionSource<bool> result) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
            => result.TrySetResult(intent?.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false) == true);
    }
}
