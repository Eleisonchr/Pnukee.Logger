using Serilog.Events;
using System;

namespace Pnukee.Logger
{
    public class LogConfigs
    {
        public LogConfigs(string AppNome, Version AppVersao, string Ambiente)
        {
            this.Versao = AppVersao;
            this.AppNome = AppNome;
            this.Ambiente = Ambiente;
        }
        public ElasticSearchConfigs ElasticSearch { get; set; }
        public string Ambiente { get; set; }
        public string AppNome { get; set; }
        public LogEventLevel Level { get; set; }
        public Version Versao { get; set; }
        public Guid AppRunID { get; set; }
    }
}