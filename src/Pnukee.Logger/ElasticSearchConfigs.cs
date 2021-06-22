using Serilog.Events;
using System;
using System.Text.RegularExpressions;

namespace Pnukee.Logger
{
    public class ElasticSearchConfigs
    {
        private bool _Enabled;

        public ElasticSearchConfigs(in string IndexName)
        {
            this.NivelDeLog = LogEventLevel.Debug;
            this._Enabled = true;




            this.IndexName = this.FormatarIndex(IndexName);
        }

        private string FormatarIndex(in string IndexName)
        {

            var Index = IndexName.ToLowerInvariant();

            var Regex = new Regex(@"([a-z0-9]{1,})+(\-\*?)?$", RegexOptions.Singleline);
            if (!Regex.IsMatch(Index))
            {
                throw new InvalidOperationException("Formato Inválido. O index das configurações do elasticsearh devem seguir o formato 'letrasenumeros[-*]'");
            }

            return Regex.Replace(Index, "$1-*");

        }

        public string Endpoint { get; set; }
        public string Username { get; set; }
        public string Senha { get; set; }
        public LogEventLevel NivelDeLog { get; set; }

        public bool Enabled
        {
            get
            {
                return this._Enabled && (!string.IsNullOrEmpty(this.Endpoint) && !string.IsNullOrEmpty(this.Username) && !string.IsNullOrEmpty(this.Senha));
            }
            set => _Enabled = value;
        }

        public string IndexName { get; set; }
    }
}