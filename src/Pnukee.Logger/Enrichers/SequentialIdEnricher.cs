using Serilog.Core;
using Serilog.Events;
using System;
using System.Threading;

namespace Pnukee.Logger.Enrichers
{
    public class SequentialIdEnricher : ILogEventEnricher
    {
        private long _currentId = 0;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            var incrementedId = Interlocked.Increment(ref _currentId);
            logEvent.AddPropertyIfAbsent(new LogEventProperty("SequentialID", new ScalarValue(incrementedId)));
        }
    }
}
