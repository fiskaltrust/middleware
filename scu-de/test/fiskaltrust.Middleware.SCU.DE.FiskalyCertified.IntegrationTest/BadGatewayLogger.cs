using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.IntegrationTest
{
    public class BadGatewayLogger : ILogger<HttpClientWrapper>
    {
        private readonly List<string> _logs = new List<string>();
        public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
        public bool IsEnabled(LogLevel logLevel) => throw new NotImplementedException();
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) => _logs.Add(state.ToString());
        public List<string> GetLog() => _logs;
    }
}
