using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.BE.IntegrationTest;

// Helper class for xUnit logging integration
public class XunitLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;

    public XunitLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable BeginScope<TState>(TState state) => new NullDisposable();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            _output.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
        }
        catch
        {
            // Ignore logging errors in tests
        }
    }

    private class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}