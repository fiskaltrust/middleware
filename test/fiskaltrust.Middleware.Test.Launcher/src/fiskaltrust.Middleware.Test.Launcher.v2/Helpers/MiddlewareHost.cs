using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;
using fiskaltrust.storage.serialization.V0;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

/// <summary>
/// Puts the in-memory middleware built by <see cref="CashBoxBuilder"/> behind HTTP so requests can
/// come from Postman, curl or a POS instead of being written into <c>Program.cs</c>. The queue, its
/// storage and the SCU are exactly the ones the scripted run uses — this only adds a transport.
///
/// This is a debug host, not the production one: the routes are named after the operations rather
/// than following the launcher's REST contract, and everything lives in memory, so a restart means
/// a new cashbox with a new queue that has to be activated again.
/// </summary>
static class MiddlewareHost
{
    /// <summary>Property names stay as declared, so payloads match the committed sample requests.</summary>
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static async Task RunAsync(
        CashBoxBuilder cashBox,
        MiddlewareMethods middleware,
        string url,
        PackageConfiguration queueConfiguration,
        PackageConfiguration scuConfiguration)
    {
        // The queue starts inactive, so without this every receipt below would fail on a fresh
        // start. It happens here rather than being left to the caller because the alternative — a
        // host whose first request always fails — is the more surprising one. MW_SKIP_ACTIVATION
        // exists for the case where failing activation is what you want to look at.
        var activation = Environment.GetEnvironmentVariable("MW_SKIP_ACTIVATION") == "1"
            ? (Succeeded: (bool?) null, Report: (object) "skipped (MW_SKIP_ACTIVATION=1)")
            : await DescribeAsync(() => middleware.Sign(InitialOperationReceipt(cashBox)));

        var host = WebApplication.CreateBuilder();
        host.WebHost.UseUrls(url);
        var app = host.Build();

        var routes = new[]
        {
            "GET  /                — this index",
            "POST /echo            — EchoRequest",
            "POST /sign            — ReceiptRequest",
            "POST /activate        — re-sends the initial-operation receipt",
            "POST /journal         — JournalRequest",
            $"GET  /journal/{{type}}  — {string.Join(" | ", Enum.GetNames<JournalType>())}",
            "GET  /samples         — the committed business cases",
            "GET  /samples/{name}  — the request that would be sent, placeholders resolved",
            "POST /samples/{name}  — signs it",
        };

        app.MapGet("/", () => Results.Json(new
        {
            cashBox.Market,
            Queue = queueConfiguration.Package,
            Scu = scuConfiguration.Package,
            // Converted, not passed through: the values in here were deserialized by Newtonsoft, and
            // System.Text.Json does not know a JObject or JArray — it enumerates them into nested
            // empty arrays, so a configured VAT rate table reads as [[[[]],[[]]],…] and looks lost
            // when it is not. This is the same conversion the builder applies before handing the
            // configuration to the SCU, so the index shows what the SCU actually got.
            ScuConfiguration = scuConfiguration.Configuration.ToSystemTextJsonValues(),
            Ids = new
            {
                cashBox.CashBoxId,
                cashBox.PosSystemId,
                cashBox.QueueId,
                cashBox.ScuId,
            },
            // Which init_ tables the configuration brought and which this launcher had to invent: a
            // configured cashbox and a synthesized one look identical from the outside.
            Tables = new
            {
                cashBox.ConfiguredTables,
                cashBox.SynthesizedTables,
            },
            Activation = activation.Report,
            Samples = Samples(cashBox).Keys,
            Routes = routes,
        }, Json));

        // These four write the response themselves, so they are RequestDelegates rather than route
        // handlers whose return value would be serialized. Each is a named local function with an
        // explicit non-generic Task: an inline lambda leaves both the overload choice and the
        // returned type to inference, and ASP0016 then reads the innermost call's Task<T> as the
        // handler's own and warns that a value is being discarded.
        Task Echo(HttpContext context)
            => RespondAsync(context, async () => await middleware.Echo(await ReadAsync<EchoRequest>(context)));

        Task Sign(HttpContext context)
            => SignAsync(context, middleware, () => ReadAsync<ReceiptRequest>(context));

        Task Activate(HttpContext context)
            => SignAsync(context, middleware, () => Task.FromResult(InitialOperationReceipt(cashBox)));

        Task Journal(HttpContext context)
            => WriteJournalAsync(context, middleware, () => ReadAsync<JournalRequest>(context));

        app.MapPost("/echo", Echo);
        app.MapPost("/sign", Sign);
        app.MapPost("/activate", Activate);
        app.MapPost("/journal", Journal);

        app.MapGet("/journal/{type}", (HttpContext context, string type)
            => WriteJournalAsync(context, middleware, () => Enum.TryParse<JournalType>(type, ignoreCase: true, out var journalType)
                ? Task.FromResult(new JournalRequest { ftJournalType = journalType })
                : throw new ArgumentException($"'{type}' is not a journal type. Known types: {string.Join(", ", Enum.GetNames<JournalType>())}.")));

        app.MapGet("/samples", () => Results.Json(Samples(cashBox), Json));

        app.MapGet("/samples/{name}", (HttpContext context, string name)
            => RespondAsync(context, async () => JsonSerializer.Deserialize<ReceiptRequest>(await ReadSampleAsync(cashBox, name), Json)));

        app.MapPost("/samples/{name}", (HttpContext context, string name)
            => SignAsync(context, middleware, async () =>
                JsonSerializer.Deserialize<ReceiptRequest>(await ReadSampleAsync(cashBox, name), Json)!));

        if (activation.Succeeded == false)
        {
            Console.WriteLine($"""

                !! The queue was not activated, so every receipt below is answered without ever reaching
                !! {scuConfiguration.Package} — no printout, no fiscal document. See Activation in the index.
                {(cashBox.Market == "PL" ? "   A non-fiscal register cannot activate a PL queue; MW_PL_ASSUME_FISCALIZED=1 gets past that gate." : "")}
                """);
        }

        Console.WriteLine($"""

            {queueConfiguration.Package} ({cashBox.Market}) on {scuConfiguration.Package}, listening on {url}
            CashBoxId {cashBox.CashBoxId}, QueueId {cashBox.QueueId}, ScuId {cashBox.ScuId}
            Initial operation: {JsonSerializer.Serialize(activation.Report, Json)}

            {string.Join(Environment.NewLine + "            ", routes)}

            """);

        await app.RunAsync();
    }

    private static ReceiptRequest InitialOperationReceipt(CashBoxBuilder cashBox) => new()
    {
        ftCashBoxID = cashBox.CashBoxId,
        cbReceiptMoment = DateTime.UtcNow,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString()[..8],
        cbChargeItems = [],
        cbPayItems = [],
        Currency = cashBox.Market == "PL" ? Currency.PLN : Currency.EUR,
        ftReceiptCase = ReceiptCase.InitialOperationReceipt0x4001.WithCountry(cashBox.Market),
    };

    /// <summary>
    /// The committed business cases, keyed by the directory name — the same key the scripted run in
    /// <c>Program.cs</c> uses. They are read per request rather than once at startup, so editing a
    /// sample and re-posting it does not need a restart.
    /// </summary>
    private static SortedDictionary<string, string> Samples(CashBoxBuilder cashBox)
    {
        var root = Path.Join(AppContext.BaseDirectory, "json-requests", cashBox.Market.ToUpperInvariant());
        var samples = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(root))
        {
            // Only PL's samples are committed (see .gitignore), so this folder is legitimately
            // absent for another market. Enumerating anyway would throw out of the route handler as
            // an unhandled 500 — and, because ReadSampleAsync composes its 400 message from this
            // list, would turn an unknown sample name into a 500 as well.
            return samples;
        }

        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            // One file per business case is the layout in the repo; if that ever changes, the index
            // should say which files are there rather than silently pick one.
            var files = Directory.EnumerateFiles(directory).Select(Path.GetFileName);
            samples.Add(Path.GetFileName(directory)!, string.Join(", ", files));
        }
        return samples;
    }

    private static async Task<string> ReadSampleAsync(CashBoxBuilder cashBox, string name)
    {
        var directory = Path.Join(AppContext.BaseDirectory, "json-requests", cashBox.Market.ToUpperInvariant(), name);
        if (!Directory.Exists(directory))
        {
            throw new FileNotFoundException($"There is no sample named {name}. Known samples: {string.Join(", ", Samples(cashBox).Keys)}.");
        }

        var files = Directory.EnumerateFiles(directory).ToList();
        if (files.Count != 1)
        {
            throw new InvalidOperationException($"The sample {name} holds {files.Count} files; this host sends a business case only when it is a single request.");
        }

        // cbReceiptReference is left as it stands in the file on purpose: the samples reference each
        // other — the return receipt points at the cash sale — which only works with stable
        // references. Post to /sign with your own reference when you need a fresh one.
        return (await File.ReadAllTextAsync(files[0]))
            .Replace("{{ ftCashBoxID }}", cashBox.CashBoxId.ToString())
            .Replace("{{ ftPosSystemID }}", cashBox.PosSystemId.ToString());
    }

    private static async Task<T> ReadAsync<T>(HttpContext context)
        => await context.Request.ReadFromJsonAsync<T>(Json)
            ?? throw new ArgumentException($"The request body was empty or not a {typeof(T).Name}.");

    /// <summary>
    /// Signs and answers exactly what the middleware answered: 200 with the ReceiptResponse, whatever
    /// the state says. A receipt the queue stored without signing is not a failed request — the queue
    /// accepted and persisted it — and callers rely on that status code, so the outcome stays where
    /// the middleware puts it, in <c>ftState</c>.
    ///
    /// What the response cannot carry is the <em>reason</em>: the queue writes that to the action
    /// journal, not to the receipt. So an unsigned receipt is reported on the console, where whoever
    /// started this debug host is looking anyway — no HTTP contract is bent to say it.
    /// </summary>
    private static async Task SignAsync(HttpContext context, MiddlewareMethods middleware, Func<Task<ReceiptRequest>> request)
    {
        try
        {
            var response = await middleware.Sign(await request());
            await context.Response.WriteAsJsonAsync(response, Json);
            await ReportUnsignedAsync(middleware, response);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    /// <summary>Puts an unsigned receipt and the queue's reason for it on the console.</summary>
    private static async Task ReportUnsignedAsync(MiddlewareMethods middleware, ReceiptResponse? response)
    {
        if (Diagnose(response) is not { } outcome)
        {
            return;
        }

        Console.WriteLine($"!! {outcome}");
        if (response is null)
        {
            return;
        }

        foreach (var message in await ActionJournalMessagesAsync(middleware, response.ftQueueItemID))
        {
            Console.WriteLine($"   {message}");
        }
    }

    /// <summary>
    /// Why the receipt was stored without being signed, or null if it was signed. Storing is not the
    /// same as signing and an unsigned receipt is not an error, so this never changes an answer — it
    /// only decides what gets logged. SecurityMechanismDeactivated is the flag behind the quiet case:
    /// the queue is not activated (or was taken out of operation), which the SignProcessor answers
    /// before a receipt ever reaches the SCU, so nothing is printed and no fiscal document exists.
    /// </summary>
    private static string? Diagnose(ReceiptResponse? response)
    {
        if (response?.ftState is not { } state)
        {
            return "The middleware returned no receipt response.";
        }

        var reasons = new List<string>();
        if (state.IsState(State.Error))
        {
            reasons.Add("the state carries Error");
        }
        if (state.IsState(State.Fail))
        {
            reasons.Add("the state carries Fail");
        }
        if (state.IsFlag(StateFlags.SecurityMechanismDeactivated))
        {
            reasons.Add("the security mechanism is deactivated — the queue is not activated or out of operation, so the receipt never reached the SCU");
        }

        return reasons.Count == 0
            ? null
            : $"The receipt was stored, but not signed: {string.Join("; ", reasons)} (ftState 0x{(ulong) state:X16}).";
    }

    /// <summary>
    /// The action journal entries the queue wrote for this queue item. The reason a receipt was
    /// turned away lives there and not in the response, so without this the caller is left with a
    /// state number.
    /// </summary>
    private static async Task<List<string>> ActionJournalMessagesAsync(MiddlewareMethods middleware, Guid? queueItemId)
    {
        try
        {
            var (_, stream) = await middleware.Journal(new JournalRequest { ftJournalType = JournalType.ActionJournal });
            using var document = await JsonDocument.ParseAsync(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return document.RootElement.EnumerateArray()
                .Where(entry => queueItemId is null
                    || (entry.TryGetProperty("ftQueueItemId", out var id)
                        && id.ValueKind == JsonValueKind.String
                        && id.GetGuid() == queueItemId))
                .Select(entry => entry.TryGetProperty("Message", out var message) ? message.GetString() : null)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(message => message!)
                .ToList();
        }
        catch (Exception ex)
        {
            // A debug host must not turn a failing explanation into a failing request.
            return [$"The action journal could not be read: {ex.Message}"];
        }
    }

    /// <summary>Runs the request and reports what the middleware answered, or what went wrong.</summary>
    private static async Task RespondAsync(HttpContext context, Func<Task<object?>> operation)
    {
        try
        {
            await context.Response.WriteAsJsonAsync(await operation(), Json);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private static async Task WriteJournalAsync(HttpContext context, MiddlewareMethods middleware, Func<Task<JournalRequest>> request)
    {
        try
        {
            var (contentType, response) = await middleware.Journal(await request());
            context.Response.ContentType = contentType.ToString();
            await response.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        // The response body is all you have in Postman, so the type goes with the message: a device
        // being unreachable reads very differently from a malformed request.
        context.Response.StatusCode = ex is ArgumentException or FileNotFoundException or JsonException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;
        return context.Response.WriteAsJsonAsync(new { Error = ex.GetType().FullName, ex.Message }, Json);
    }

    /// <summary>
    /// Startup activation must not take the host down with it — a failure is a result to look at. The
    /// verdict is returned next to the report so the caller can act on it without re-reading the JSON.
    /// </summary>
    private static async Task<(bool? Succeeded, object Report)> DescribeAsync(Func<Task<ReceiptResponse?>> sign)
    {
        try
        {
            var response = await sign();
            var succeeded = Diagnose(response) is null;
            return (succeeded, new { Succeeded = succeeded, Response = response });
        }
        catch (Exception ex)
        {
            return (false, new { Succeeded = false, Error = ex.GetType().FullName, ex.Message });
        }
    }
}
