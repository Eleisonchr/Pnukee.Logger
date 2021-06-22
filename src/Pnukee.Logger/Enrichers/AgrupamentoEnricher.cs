using Serilog.Core;
using Serilog.Events;
using System;

namespace Pnukee.Logger.Enrichers
{
    public class AgrupamentoEnricher : ILogEventEnricher
    {
        public AgrupamentoEnricher()
        {
            this.ContextID = Guid.Empty;
        }
        public const string PropertyName = "ContextoID";
        public Guid ContextID { get; set; }


        /// <summary>Enrich the log event.</summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var CtxId = this.ContextID;
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, CtxId.ToString()));
        }



    }
}
