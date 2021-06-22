using Pnukee.Logger.Enrichers;
using Serilog;
using Serilog.Configuration;
using System;

namespace Pnukee.Logger
{
    public static class LoggingExtensions
    {
        public static LoggerConfiguration WithVersao(this LoggerEnrichmentConfiguration enrich, Version Versao)
        {
            if (enrich == null)
            {
                throw new ArgumentNullException(nameof(enrich));

            }


            return enrich.With(new VersaoEnricher(Versao));
        }

        public static LoggerConfiguration WithSequencialID(this LoggerEnrichmentConfiguration enrich)
        {
            if (enrich == null)
            {
                throw new ArgumentNullException(nameof(enrich));

            }


            return enrich.With<SequentialIdEnricher>();
        }


        public static LoggerConfiguration WithAmbiente(this LoggerEnrichmentConfiguration enrich, string Ambiente)
        {
            if (enrich == null)
            {
                throw new ArgumentNullException(nameof(enrich));

            }


            return enrich.With(new AmbienteEnricher(Ambiente));
        }
        public static LoggerConfiguration WithStack(this LoggerEnrichmentConfiguration enrich)
        {
            if (enrich == null)
            {
                throw new ArgumentNullException(nameof(enrich));

            }


            return enrich.With<StackEnricher>();
        }
        public static LoggerConfiguration WithAppNome(this LoggerEnrichmentConfiguration enrich, string AppNome)
        {
            if (enrich == null)
            {
                throw new ArgumentNullException(nameof(enrich));

            }


            return enrich.With(new AplicacaoEnricher(AppNome));
        }
        public static LoggerConfiguration WithLogSession(this LoggerEnrichmentConfiguration enrich, Guid SessaoID)
        {
            if (enrich == null)
            {
                throw new ArgumentNullException(nameof(enrich));

            }


            return enrich.With(new LogSessionEnricher(SessaoID));
        }

    }
}