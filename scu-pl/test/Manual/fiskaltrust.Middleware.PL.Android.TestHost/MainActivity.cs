using System.Text.Json;
using Android.Content.PM;
using Android.Util;
using Android.Views;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.PL.TestSupport;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers;
using fiskaltrust.storage.serialization.V0;

namespace fiskaltrust.Middleware.PL.Android.TestHost;

/// <summary>
/// Drives the Polish middleware on the device by hand: one button per business case, each sent
/// through the in-process queue to the POSNET printer named in the address field.
/// </summary>
/// <remarks>
/// The queue lives in memory, so after every app start (or address change) the initial operation
/// has to go first — until then the queue is inactive and forwards nothing to the printer.
/// Every receipt sent to a fiscalized printer is a real fiscal document. Only the network address
/// (tcp://) is supported here: the USB driver is owned by the host application, and on Android that is
/// the fiskaltrust Android launcher, where usbhost:// is tested.
/// </remarks>
// No action bar: from Android 15 on, apps draw edge-to-edge and the bar would cover the first rows.
[Activity(Label = "PL TestHost", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
    private const string LogTag = "PLTestHost";
    private const string DefaultDeviceUrl = "tcp://192.168.178.58:6666";

    private EditText _deviceUrl = null!;
    private TextView _output = null!;
    private CashBoxBuilder? _builder;
    private CashBoxBuilderPL? _cashBox;
    private MiddlewareMethods? _middleware;
    private string? _builtForDeviceUrl;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
        layout.SetPadding(32, 32, 32, 32);
        // Keeps the controls clear of the status and navigation bars when drawing edge-to-edge.
        layout.SetFitsSystemWindows(true);

        _deviceUrl = new EditText(this) { Text = DefaultDeviceUrl };
        _deviceUrl.SetSingleLine(true);
        layout.AddView(_deviceUrl);

        AddButton(layout, "Echo", EchoAsync);
        AddButton(layout, "Inbetriebnahme", () => SignSampleAsync("SignRequestLifecycle_InitialOperation/initial-operation.json"));
        AddButton(layout, "Barverkauf", () => SignSampleAsync("SignRequestReceipt_CashSaleReceipt/cash-sale.json"));
        AddButton(layout, "Tagesabschluss", () => SignSampleAsync("SignRequestDailyOperations_DailyClosing/daily-closing.json"));

        _output = new TextView(this) { TextSize = 12 };
        var scroll = new ScrollView(this);
        scroll.AddView(_output);
        layout.AddView(scroll);

        SetContentView(layout);
    }

    private void AddButton(LinearLayout layout, string text, Func<Task> action)
    {
        var button = new Button(this) { Text = text };
        button.Click += async (_, _) =>
        {
            button.Enabled = false;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Write($"{text} fehlgeschlagen: {ex}");
            }
            finally
            {
                button.Enabled = true;
            }
        };
        layout.AddView(button);
    }

    private async Task EchoAsync()
    {
        var middleware = await GetMiddlewareAsync();
        var response = await Task.Run(() => middleware.Echo(new EchoRequest { Message = "Hello from Android" }));
        Write($"Echo: {response?.Message}");
    }

    private async Task SignSampleAsync(string sample)
    {
        var middleware = await GetMiddlewareAsync();
        var json = BusinessCaseSample.Resolve(await ReadAssetAsync($"BusinessCases/{sample}"), _builder!.CashBoxId, _builder.PosSystemId);
        var request = JsonSerializer.Deserialize<ReceiptRequest>(json)!;
        // The samples carry fixed references and moments for the end-to-end suite; a run by hand
        // should look like a receipt made now.
        request.cbReceiptReference = Guid.NewGuid().ToString()[..8];
        request.cbReceiptMoment = DateTime.UtcNow;

        Write($"{sample} -> {_builtForDeviceUrl}");
        var response = await Task.Run(() => middleware.Sign(request));
        Log.Info(LogTag, JsonSerializer.Serialize(response));
        Write($"ftState=0x{response?.ftState:X}, ftReceiptIdentification={response?.ftReceiptIdentification}, Signaturen={response?.ftSignatures?.Count ?? 0}");
        foreach (var signature in response?.ftSignatures ?? [])
        {
            Write($"  {signature.Caption}: {signature.Data}");
        }
    }

    /// <summary>
    /// Builds the cashbox once per device address, from the launcher's PL configuration with only the
    /// address replaced — the ids and init tables stay the ones that file pins.
    /// </summary>
    private async Task<MiddlewareMethods> GetMiddlewareAsync()
    {
        var deviceUrl = _deviceUrl.Text?.Trim() is { Length: > 0 } url ? url : DefaultDeviceUrl;
        if (_middleware is not null && _builtForDeviceUrl == deviceUrl)
        {
            return _middleware;
        }

        var configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<ftCashBoxConfiguration>(await ReadAssetAsync("cashbox-configuration-posnet.json"))!;
        var queueConfiguration = configuration.ftQueues.Single();
        var scuConfiguration = configuration.ftSignaturCreationDevices.Single();
        scuConfiguration.Configuration["DeviceUrl"] = deviceUrl;

        ReleaseMiddleware();
        _cashBox = new CashBoxBuilderPL();
        _builder = new CashBoxBuilder(_cashBox, queueConfiguration, scuConfiguration, configuration.ftCashBoxId, Guid.NewGuid());
        _middleware = await Task.Run(_builder.Build);
        _builtForDeviceUrl = deviceUrl;
        Write($"Middleware aufgebaut: Queue {_builder.QueueId}, SCU {_builder.ScuId}, Drucker {deviceUrl}. Zuerst die Inbetriebnahme senden.");
        return _middleware;
    }

    private void ReleaseMiddleware()
    {
        _cashBox?.Dispose();
        _cashBox = null;
        _builder = null;
        _middleware = null;
        _builtForDeviceUrl = null;
    }

    protected override void OnDestroy()
    {
        ReleaseMiddleware();
        base.OnDestroy();
    }

    private async Task<string> ReadAssetAsync(string path)
    {
        using var reader = new StreamReader(Assets!.Open(path));
        return await reader.ReadToEndAsync();
    }

    private void Write(string message)
    {
        Log.Info(LogTag, message);
        RunOnUiThread(() => _output.Append(message + "\n"));
    }
}
