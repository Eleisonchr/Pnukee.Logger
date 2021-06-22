using Serilog.Core;
using Serilog.Events;

namespace Pnukee.Logger.Enrichers
{
    public class AmbienteEnricher : ILogEventEnricher
    {
        private static string _Ambiente;
        public string Ambiente
        {
            get => _Ambiente;
            set => _Ambiente = value;
        }

        public const string PropertyName = "Ambiente";

        public AmbienteEnricher(string Ambiente)
        {
            _Ambiente = Ambiente;
        }
        #region Implementation of ILogEventEnricher

        /// <summary>Enrich the log event.</summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, _Ambiente));
        }


        #endregion

    }


}
