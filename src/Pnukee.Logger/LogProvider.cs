using Pnukee.Logger.Enrichers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Collections.Generic;

namespace Pnukee.Logger
{
    public class LogProvider : IFullLoggerProvider
    {
        private readonly Serilog.Core.Logger _Log;
        private readonly ContextoEnricher _ContextoEnricher;
        private bool _ContextoLocked;
        private Guid _CurrentAgrupamento = Guid.Empty;
        private readonly AgrupamentoEnricher _AgrupamentoEnricher;
        private LogSimples _SimpleLogger;

        public LogProvider(LogConfigs Configs)
        {
            var LogConfiger = new LoggerConfiguration();
            LogConfiger = LogConfiger.Enrich.FromLogContext();
            //LogConfiger

            if (Configs.ElasticSearch.Enabled)
            {
                var Elk = new ElasticsearchSinkOptions(new Uri(Configs.ElasticSearch.Endpoint))
                {
                    MinimumLogEventLevel = Configs.ElasticSearch.NivelDeLog,
                    IndexFormat = Configs.ElasticSearch.IndexName.Remove(Configs.ElasticSearch.IndexName.Length - 2) + "-{0:yyyy.MM.dd}",
                    AutoRegisterTemplate = true,
                    ModifyConnectionSettings = x => x.BasicAuthentication(Configs.ElasticSearch.Username, Configs.ElasticSearch.Senha)
                };

                LogConfiger.WriteTo.Elasticsearch(Elk);

            }

            //if (Configs.IncluirHttpContext)
            //{
            //    LogConfiger = LogConfiger.Enrich.WithRequest();
            //    LogConfiger = LogConfiger.Enrich.WithResponse();
            //}

            LogConfiger = LogConfiger.Enrich.WithSequencialID();
            LogConfiger = LogConfiger.Enrich.WithVersao(Configs.Versao);
            LogConfiger = LogConfiger.Enrich.WithAmbiente(Configs.Ambiente);
            LogConfiger = LogConfiger.Enrich.WithStack();
            LogConfiger = LogConfiger.Enrich.WithAppNome(Configs.AppNome);
            LogConfiger = LogConfiger.Enrich.WithLogSession(Configs.AppRunID.Equals(Guid.Empty) ? Guid.NewGuid() : Configs.AppRunID);

            _ContextoEnricher = new ContextoEnricher();
            _AgrupamentoEnricher = new AgrupamentoEnricher();

            LogConfiger.Enrich.With(_ContextoEnricher);
            LogConfiger.Enrich.With(_AgrupamentoEnricher);

            switch (Configs.Level)
            {
                case LogEventLevel.Verbose:
                    LogConfiger.MinimumLevel.Verbose();
                    break;
                case LogEventLevel.Debug:
                    LogConfiger.MinimumLevel.Debug();
                    break;
                case LogEventLevel.Information:
                    LogConfiger.MinimumLevel.Information();
                    break;
                case LogEventLevel.Warning:
                    LogConfiger.MinimumLevel.Warning();
                    break;
                case LogEventLevel.Error:
                    LogConfiger.MinimumLevel.Error();
                    break;
                case LogEventLevel.Fatal:
                    LogConfiger.MinimumLevel.Fatal();
                    break;
                default:
                    throw new NotSupportedException("Este Level não está implementado.");

            }

            _Log = LogConfiger.CreateLogger();
        }

        public string Contexto
        {
            get => _ContextoEnricher.Contexto;
            set
            {
                _ContextoEnricher.Contexto = value;
                _ContextoLocked = (value != null);
            }

        }

        public ILoggerProvider ObterLoggerSimples()
        {
            if (_SimpleLogger == null)
            {
                _SimpleLogger = new LogSimples(this);
            }

            return _SimpleLogger;

        }

        public IFullLoggerProvider Contextualize(string Contexto)
        {
            _ContextoEnricher.Contexto = Contexto;
            return this;
        }

        public Guid AgruparLogs(string Contexto)
        {
            if (!_CurrentAgrupamento.Equals(Guid.Empty))
            {
                Warning("Tentativa de definir um agrupamento de log a um agrupamento já existente.");
                return Guid.Empty;
            }
            if (!_ContextoEnricher.EhContextoPadrao)
            {
                throw new InvalidOperationException("Não é possível definir um agrupameto, pois o contexto do log já foi previamente definido.");
            }
            this.Contexto = Contexto;
            _CurrentAgrupamento = Guid.NewGuid();
            _AgrupamentoEnricher.ContextID = _CurrentAgrupamento;
            return _CurrentAgrupamento;

        }

        public void ReleaseAgrupamento(Guid AgrupamentoID, bool ResetContext)
        {
            if (!_CurrentAgrupamento.Equals(AgrupamentoID))
            {
                this.Warning("Solicitado um cancelamento de agrupamento com ID inválido. O agrupamento não foi cancelado");
                return;
            }

            _CurrentAgrupamento = Guid.Empty;
            _AgrupamentoEnricher.ContextID = _CurrentAgrupamento;
            if (ResetContext)
            {
                this.ResetContexto();
            }
        }

        public void ResetContexto()
        {
            _ContextoLocked = false;
            ProcessarResetDeContexto();
        }



        private void ProcessarResetDeContexto()
        {
            if (!_ContextoLocked)
            {
                _ContextoEnricher.Contexto = null;
            }
        }

        #region Implementation of ILogger


        /// <summary>
        /// Create a logger that enriches log events via the provided enrichers.
        /// </summary>
        /// <param name="Enricher">Enricher that applies in the context.</param>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public ILogger ForContext(ILogEventEnricher Enricher)
        {
            return _Log.ForContext(Enricher);
        }

        /// <summary>
        /// Create a logger that enriches log events via the provided enrichers.
        /// </summary>
        /// <param name="Enrichers">Enrichers that apply in the context.</param>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public ILogger ForContext(IEnumerable<ILogEventEnricher> Enrichers)
        {
            return _Log.ForContext(Enrichers);
        }

        /// <summary>
        /// Create a logger that enriches log events with the specified property.
        /// </summary>
        /// <param name="PropertyName">The name of the property. Must be non-empty.</param>
        /// <param name="Value">The property value.</param>
        /// <param name="DestructureObjects">If true, the value will be serialized as a structured
        /// object if possible; if false, the object will be recorded as a scalar or simple array.</param>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public ILogger ForContext(string PropertyName, object Value, bool DestructureObjects = false)
        {
            return _Log.ForContext(PropertyName, Value, DestructureObjects);
        }

        /// <summary>
        /// Create a logger that marks log events as being from the specified
        /// source type.
        /// </summary>
        /// <typeparam name="TSource">Type generating log messages in the context.</typeparam>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public ILogger ForContext<TSource>()
        {
            return _Log.ForContext<TSource>();
        }

        /// <summary>
        /// Create a logger that marks log events as being from the specified
        /// source type.
        /// </summary>
        /// <param name="Source">Type generating log messages in the context.</param>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public ILogger ForContext(Type Source)
        {
            return _Log.ForContext(Source);
        }

        /// <summary>Write an event to the log.</summary>
        /// <param name="LogEvent">The event to write.</param>
        public void Write(LogEvent LogEvent)
        {
            _Log.Write(LogEvent);
            ProcessarResetDeContexto();
        }



        /// <summary>Write a log event with the specified level.</summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        public void Write(LogEventLevel Level, string MessageTemplate)
        {
            _Log.Write(Level, MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>Write a log event with the specified level.</summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        public void Write<T>(LogEventLevel Level, string MessageTemplate, T PropertyValue)
        {
            _Log.Write<T>(Level, MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>Write a log event with the specified level.</summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        public void Write<T0, T1>(LogEventLevel Level, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Write<T0, T1>(Level, MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>Write a log event with the specified level.</summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        public void Write<T0, T1, T2>(LogEventLevel Level, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Write<T0, T1, T2>(Level, MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>Write a log event with the specified level.</summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="MessageTemplate"></param>
        /// <param name="PropertyValues"></param>
        public void Write(LogEventLevel Level, string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Write(Level, MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the specified level and associated exception.
        /// </summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        public void Write(LogEventLevel Level, Exception Exception, string MessageTemplate)
        {
            _Log.Write(Level, Exception, MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the specified level and associated exception.
        /// </summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        public void Write<T>(LogEventLevel Level, Exception Exception, string MessageTemplate, T PropertyValue)
        {
            _Log.Write<T>(Level, Exception, MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the specified level and associated exception.
        /// </summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        public void Write<T0, T1>(LogEventLevel Level, Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Write<T0, T1>(Level, Exception, MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the specified level and associated exception.
        /// </summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        public void Write<T0, T1, T2>(LogEventLevel Level, Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Write<T0, T1, T2>(Level, Exception, MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the specified level and associated exception.
        /// </summary>
        /// <param name="Level">The level of the event.</param>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        public void Write(LogEventLevel Level, Exception Exception, string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Write(Level, Exception, MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Determine if events at the specified level will be passed through
        /// to the log sinks.
        /// </summary>
        /// <param name="Level">Level to check.</param>
        /// <returns>True if the level is enabled; otherwise, false.</returns>
        public bool IsEnabled(LogEventLevel Level)
        {
            return _Log.IsEnabled(Level);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Verbose("Staring into space, wondering if we're alone.");
        /// </example>
        public void Verbose(string MessageTemplate)
        {
            _Log.Verbose(MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose("Staring into space, wondering if we're alone.");
        /// </example>
        public void Verbose<T>(string MessageTemplate, T PropertyValue)
        {
            _Log.Verbose<T>(MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose("Staring into space, wondering if we're alone.");
        /// </example>
        public void Verbose<T0, T1>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Verbose<T0, T1>(MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose("Staring into space, wondering if we're alone.");
        /// </example>
        public void Verbose<T0, T1, T2>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Verbose<T0, T1, T2>(MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level and associated exception.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose("Staring into space, wondering if we're alone.");
        /// </example>
        public void Verbose(string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Verbose(MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
        /// </example>
        public void Verbose(Exception Exception, string MessageTemplate)
        {
            _Log.Verbose(Exception, MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
        /// </example>
        public void Verbose<T>(Exception Exception, string MessageTemplate, T PropertyValue)
        {
            _Log.Verbose<T>(Exception, MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
        /// </example>
        public void Verbose<T0, T1>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Verbose<T0, T1>(Exception, MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
        /// </example>
        public void Verbose<T0, T1, T2>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Verbose<T0, T1, T2>(Exception, MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Verbose" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
        /// </example>
        public void Verbose(Exception Exception, string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Verbose(Exception, MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
        /// </example>
        public void Debug(string MessageTemplate)
        {
            _Log.Debug(MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
        /// </example>
        public void Debug<T>(string MessageTemplate, T PropertyValue)
        {
            _Log.Debug<T>(MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
        /// </example>
        public void Debug<T0, T1>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Debug<T0, T1>(MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="propertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
        /// </example>
        public void Debug<T0, T1, T2>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 propertyValue2)
        {
            _Log.Debug<T0, T1, T2>(MessageTemplate, PropertyValue0, PropertyValue1, propertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level and associated exception.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
        /// </example>
        public void Debug(string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Debug(MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>Log.Debug(ex, "Swallowing a mundane exception.");</example>
        public void Debug(Exception Exception, string MessageTemplate)
        {
            _Log.Debug(Exception, MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>Log.Debug(ex, "Swallowing a mundane exception.");</example>
        public void Debug<T>(Exception Exception, string MessageTemplate, T PropertyValue)
        {
            _Log.Debug<T>(Exception, MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>Log.Debug(ex, "Swallowing a mundane exception.");</example>
        public void Debug<T0, T1>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Debug<T0, T1>(Exception, MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        /// <example>Log.Debug(ex, "Swallowing a mundane exception.");</example>
        public void Debug<T0, T1, T2>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Debug<T0, T1, T2>(Exception, MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>Log.Debug(ex, "Swallowing a mundane exception.");</example>
        public void Debug(Exception Exception, string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Debug(Exception, MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information(string MessageTemplate)
        {
            _Log.Information(MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information<T>(string MessageTemplate, T PropertyValue)
        {
            _Log.Information<T>(MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information<T0, T1>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Information<T0, T1>(MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information<T0, T1, T2>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Information<T0, T1, T2>(MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level and associated exception.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information(string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Information(MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information(Exception Exception, string MessageTemplate)
        {
            _Log.Information(Exception, MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information<T>(Exception Exception, string MessageTemplate, T PropertyValue)
        {
            _Log.Information<T>(Exception, MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information<T0, T1>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Information<T0, T1>(Exception, MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information<T0, T1, T2>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Information<T0, T1, T2>(Exception, MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public void Information(Exception Exception, string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Information(Exception, MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning(string MessageTemplate)
        {
            _Log.Warning(MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="propertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning<T>(string MessageTemplate, T propertyValue)
        {
            _Log.Warning<T>(MessageTemplate, propertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning<T0, T1>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Warning<T0, T1>(MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="propertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning<T0, T1, T2>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 propertyValue2)
        {
            _Log.Warning<T0, T1, T2>(MessageTemplate, PropertyValue0, PropertyValue1, propertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level and associated exception.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning(string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Warning(MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning(Exception Exception, string MessageTemplate)
        {
            _Log.Warning(Exception, MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning<T>(Exception Exception, string MessageTemplate, T PropertyValue)
        {
            _Log.Warning<T>(Exception, MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning<T0, T1>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Warning<T0, T1>(Exception, MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning<T0, T1, T2>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Warning<T0, T1, T2>(Exception, MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public void Warning(Exception Exception, string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Warning(Exception, MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error(string MessageTemplate)
        {
            _Log.Error(MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error<T>(string MessageTemplate, T PropertyValue)
        {
            _Log.Error<T>(MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error<T0, T1>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Error<T0, T1>(MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error<T0, T1, T2>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Error<T0, T1, T2>(MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level and associated exception.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error(string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Error(MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error(Exception Exception, string MessageTemplate)
        {
            _Log.Error(Exception, MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error<T>(Exception Exception, string MessageTemplate, T PropertyValue)
        {
            _Log.Error<T>(Exception, MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error<T0, T1>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Error<T0, T1>(Exception, MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue2">Object positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error<T0, T1, T2>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 PropertyValue2)
        {
            _Log.Error<T0, T1, T2>(Exception, MessageTemplate, PropertyValue0, PropertyValue1, PropertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public void Error(Exception Exception, string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Error(Exception, MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <example>
        /// Log.Error(ex);
        /// </example>
        public void Error(Exception Exception)
        {
            this.Error(Exception, Exception.Message);
        }

        public void Fatal(Exception Exception)
        {
            this.Fatal(Exception, Exception.Message);
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>Log.Fatal("Process terminating.");</example>
        public void Fatal(string MessageTemplate)
        {
            _Log.Fatal(MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>Log.Fatal("Process terminating.");</example>
        public void Fatal<T>(string MessageTemplate, T PropertyValue)
        {
            _Log.Fatal<T>(MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>Log.Fatal("Process terminating.");</example>
        public void Fatal<T0, T1>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Fatal<T0, T1>(MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="propertyValue2">Object positionally formatted into the message template.</param>
        /// <example>Log.Fatal("Process terminating.");</example>
        public void Fatal<T0, T1, T2>(string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 propertyValue2)
        {
            _Log.Fatal<T0, T1, T2>(MessageTemplate, PropertyValue0, PropertyValue1, propertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level and associated exception.
        /// </summary>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>Log.Fatal("Process terminating.");</example>
        public void Fatal(string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Fatal(MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <example>Log.Fatal(ex, "Process terminating.");</example>
        public void Fatal(Exception Exception, string MessageTemplate)
        {
            _Log.Fatal(Exception, MessageTemplate);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue">Object positionally formatted into the message template.</param>
        /// <example>Log.Fatal(ex, "Process terminating.");</example>
        public void Fatal<T>(Exception Exception, string MessageTemplate, T PropertyValue)
        {
            _Log.Fatal<T>(Exception, MessageTemplate, PropertyValue);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <example>Log.Fatal(ex, "Process terminating.");</example>
        public void Fatal<T0, T1>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1)
        {
            _Log.Fatal<T0, T1>(Exception, MessageTemplate, PropertyValue0, PropertyValue1);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="PropertyValue1">Object positionally formatted into the message template.</param>
        /// <param name="propertyValue2">Object positionally formatted into the message template.</param>
        /// <example>Log.Fatal(ex, "Process terminating.");</example>
        public void Fatal<T0, T1, T2>(Exception Exception, string MessageTemplate, T0 PropertyValue0, T1 PropertyValue1, T2 propertyValue2)
        {
            _Log.Fatal<T0, T1, T2>(Exception, MessageTemplate, PropertyValue0, PropertyValue1, propertyValue2);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level and associated exception.
        /// </summary>
        /// <param name="Exception">Exception related to the event.</param>
        /// <param name="MessageTemplate">Message template describing the event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <example>Log.Fatal(ex, "Process terminating.");</example>
        public void Fatal(Exception Exception, string MessageTemplate, params object[] PropertyValues)
        {
            _Log.Fatal(Exception, MessageTemplate, PropertyValues);
            ProcessarResetDeContexto();
        }

        /// <summary>
        /// Uses configured scalar conversion and destructuring rules to bind a set of properties to a
        /// message template. Returns false if the template or values are invalid (<c>ILogger</c>
        /// methods never throw exceptions).
        /// </summary>
        /// <param name="MessageTemplate">Message template describing an event.</param>
        /// <param name="PropertyValues">Objects positionally formatted into the message template.</param>
        /// <param name="ParsedTemplate">The internal representation of the template, which may be used to
        /// render the <paramref name="BoundProperties" /> as text.</param>
        /// <param name="BoundProperties">Captured properties from the template and <paramref name="PropertyValues" />.</param>
        /// <example>
        /// MessageTemplate template;
        /// IEnumerable&lt;LogEventProperty&gt; properties&gt;;
        /// if (Log.BindMessageTemplate("Hello, {Name}!", new[] { "World" }, out template, out properties)
        /// {
        ///     var propsByName = properties.ToDictionary(p =&gt; p.Name, p =&gt; p.Value);
        ///     Console.WriteLine(template.Render(propsByName, null));
        ///     // -&gt; "Hello, World!"
        /// }
        /// </example>
        public bool BindMessageTemplate(string MessageTemplate, object[] PropertyValues, out MessageTemplate ParsedTemplate, out IEnumerable<LogEventProperty> BoundProperties)
        {
            return _Log.BindMessageTemplate(MessageTemplate, PropertyValues, out ParsedTemplate, out BoundProperties);
        }

        /// <summary>
        /// Uses configured scalar conversion and destructuring rules to bind a property value to its captured
        /// representation.
        /// </summary>
        /// <param name="PropertyName">The name of the property. Must be non-empty.</param>
        /// <param name="Value">The property value.</param>
        /// <param name="DestructureObjects">If true, the value will be serialized as a structured
        /// object if possible; if false, the object will be recorded as a scalar or simple array.</param>
        /// <param name="Property">The resulting property.</param>
        /// <returns>True if the property could be bound, otherwise false (<summary>ILogger</summary>
        /// methods never throw exceptions).</returns>
        public bool BindProperty(string PropertyName, object Value, bool DestructureObjects, out LogEventProperty Property)
        {
            return _Log.BindProperty(PropertyName, Value, DestructureObjects, out Property);
        }

        #endregion
    }
}
