using System.Reflection;
using System.Text.Json;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fiskaltrust.Middleware.SCU.PL.Test.Launcher;

/// <summary>
/// Manual debug host for the Polish SCUs, in the shape of the German one
/// (fiskaltrust.Middleware.SCU.DE.Test.Launcher): the run is configured in the fields below rather
/// than through files or environment variables, the SCU is built through its own
/// <see cref="PosNet.ScuBootstrapper"/>, and the process stays up so requests can come from Postman
/// or curl.
///
/// The endpoints are derived from <see cref="IPLSSCD"/> itself rather than written out by hand, so
/// what you can call here is exactly the SCU contract — no invented convenience routes, and a
/// method added to the interface shows up without touching this file. The German launcher gets the
/// same property from hosting its contract through WCF and gRPC.
///
/// The queue is deliberately not involved. There is no activation state and no fiscalization check
/// in the way here — which is what makes a non-fiscal test device usable at all — but equally no
/// receipt numbering, no journal and no state mapping. This talks to the SCU, nothing else.
/// </summary>
public static class Program
{
    /// <summary>Which SCU to host: the PosNet printer driver, or the InMemory stand-in.</summary>
    private static readonly string Package = "fiskaltrust.Middleware.SCU.PL.PosNet";

    /// <summary>The SCU configuration, exactly as it would arrive from the cashbox configuration.</summary>
    private static readonly Dictionary<string, object> Configuration = new()
    {
        ["DeviceUrl"] = "tcp://192.168.178.58:6666",
        ["ConnectTimeoutMs"] = 5_000,
        // A real printer answers a trend only once the paper has actually moved.
        ["ReceiveTimeoutMs"] = 30_000,
    };

    private static readonly Guid ScuId = Guid.Parse("0d605997-9207-41b0-8fc2-206378bc904d");

    private static readonly string HostUrl = "http://localhost:1401";

    /// <summary>Property names stay as declared, so payloads match what the queue sends.</summary>
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
    };

    public static async Task Main()
    {
        var services = new ServiceCollection();
        switch (Package)
        {
            case "fiskaltrust.Middleware.SCU.PL.PosNet":
                new PosNet.ScuBootstrapper { Id = ScuId, Configuration = Configuration }.ConfigureServices(services);
                break;
            case "fiskaltrust.Middleware.SCU.PL.InMemory":
                services.AddSingleton<IPLSSCD, InMemorySCU>();
                break;
            default:
                throw new NotSupportedException($"The package '{Package}' is not supported by this launcher.");
        }

        await using var provider = services.BuildServiceProvider();
        var scu = provider.GetRequiredService<IPLSSCD>();

        var host = WebApplication.CreateBuilder();
        host.WebHost.UseUrls(HostUrl);
        var app = host.Build();

        var routes = MapContract(app, scu);
        app.MapGet("/", () => Results.Json(new { Package, Configuration, Routes = routes }, Json));

        Console.WriteLine($"""

            {Package} listening on {HostUrl}
            Configuration: {string.Join(", ", Configuration.Select(entry => $"{entry.Key}={entry.Value}"))}

            {string.Join(Environment.NewLine + "            ", routes)}

            """);

        await app.RunAsync();
    }

    /// <summary>
    /// Publishes every method of <see cref="IPLSSCD"/> as an endpoint named after it, minus the
    /// Async suffix — the same trimming the German launcher's gRPC binder does. A method without
    /// arguments becomes a GET, one taking a request object a POST carrying it as the body.
    /// </summary>
    /// <returns>A human-readable list of what was published, for the index route and the console.</returns>
    private static List<string> MapContract(WebApplication app, IPLSSCD scu)
    {
        var routes = new List<string>();
        foreach (var method in typeof(IPLSSCD).GetMethods())
        {
            var route = "/" + (method.Name.EndsWith("Async", StringComparison.Ordinal)
                ? method.Name[..^"Async".Length]
                : method.Name);
            var parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                app.MapGet(route, context => WriteResultAsync(context, method, scu, []));
                routes.Add($"GET  {route}");
                continue;
            }

            if (parameters.Length != 1)
            {
                throw new NotSupportedException(
                    $"{method.Name} takes {parameters.Length} arguments; this host only maps contract methods with none or one.");
            }

            var parameterType = parameters[0].ParameterType;
            app.MapPost(route, async context =>
            {
                var argument = await JsonSerializer.DeserializeAsync(context.Request.Body, parameterType, Json);
                await WriteResultAsync(context, method, scu, [argument]);
            });
            routes.Add($"POST {route} — {parameterType.Name}");
        }
        return routes;
    }

    private static async Task WriteResultAsync(HttpContext context, MethodInfo method, IPLSSCD scu, object?[] arguments)
    {
        try
        {
            var task = (Task) method.Invoke(scu, arguments)!;
            await task;
            var result = task.GetType().GetProperty(nameof(Task<object>.Result))?.GetValue(task);
            await context.Response.WriteAsJsonAsync(result, Json);
        }
        catch (Exception ex)
        {
            // Unwrap the reflection wrapper so the caller sees what the SCU actually threw — a
            // device error reads very differently from a wiring mistake, and in Postman the
            // response body is all you have.
            var cause = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { Error = cause.GetType().FullName, cause.Message }, Json);
        }
    }
}
