using Serilog.Core;
using Serilog.Events;

namespace Pnukee.Logger.Enrichers
{
    public class ContextoEnricher : ILogEventEnricher
    {
        private string _Contexto;
        private const string CONTEXTO_DEFAULT = "Geral";


        public const string PropertyName = "Contexto";

        public string Contexto
        {
            get
            {
                if (string.IsNullOrEmpty(_Contexto))
                {
                    return CONTEXTO_DEFAULT;
                };
                return _Contexto;
            }
            set => _Contexto = value;
        }

        public bool EhContextoPadrao
        {
            get
            {
                return this.Contexto.Equals(CONTEXTO_DEFAULT);
            }
        }

        #region Implementation of ILogEventEnricher

        /// <summary>Enrich the log event.</summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, this.Contexto));
        }

        #endregion
    }

}
