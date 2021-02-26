using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace PackDB.Core
{
    [ExcludeFromCodeCoverage]
    public class EmptyLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NoOpp();
        }

        private class NoOpp : IDisposable
        {
            public void Dispose()
            {
            }
        }
        
    }
}