using Serilog.Core;
using Serilog.Events;
using System;

namespace Pnukee.Logger.Enrichers
{
    public class VersaoEnricher : ILogEventEnricher
    {
        public VersaoEnricher(Version Versao)
        {
            this.Versao = Versao;
        }
        #region Implementation of ILogEventEnricher

        public const string PropertyName = "Versao";
        public Version Versao { get; set; }


        /// <summary>Enrich the log event.</summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, this.Versao.ToString()));
        }

        #endregion
    }
}
