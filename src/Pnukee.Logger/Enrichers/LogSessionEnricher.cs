using Serilog.Core;
using Serilog.Events;
using System;

namespace Pnukee.Logger.Enrichers
{
    public class LogSessionEnricher : ILogEventEnricher
    {
        public LogSessionEnricher(Guid SessaoID)
        {
            this.SessaoID = SessaoID;
        }
        public const string PropertyName = "LogSessaoID";
        public Guid SessaoID { get; set; }


        /// <summary>Enrich the log event.</summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, SessaoID.ToString()));
        }



    }
}
