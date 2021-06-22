using Serilog.Core;
using Serilog.Events;

namespace Pnukee.Logger.Enrichers
{
    public class AplicacaoEnricher : ILogEventEnricher
    {


        public AplicacaoEnricher(string AppNome)
        {
            this.AppNome = AppNome;
        }

        public const string PropertyName = "App";
        public string AppNome { get; set; }

        #region Implementation of ILogEventEnricher

        /// <summary>Enrich the log event.</summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, this.AppNome));
        }



        #endregion
    }
}