using System;

namespace Pnukee.Logger
{
    public class LogSimples : ILoggerProvider
    {
        private readonly IFullLoggerProvider _Source;

        protected internal LogSimples(IFullLoggerProvider Source)
        {
            _Source = Source;
        }
        #region Implementation of ILoggerContext

        public string Contexto { get; set; }
        public IFullLoggerProvider Contextualize(string Contexto)
        {
            return _Source.Contextualize(Contexto);
        }

        public Guid AgruparLogs(string Contexto)
        {
            return _Source.AgruparLogs(Contexto);
        }

        public void ReleaseAgrupamento(Guid AgrupamentoID, bool ResetContext)
        {
            _Source.ReleaseAgrupamento(AgrupamentoID, ResetContext);
        }

        public void ResetContexto()
        {
            _Source.ResetContexto();
        }

        #endregion

        #region Implementation of ILoggerProvider

        public void Verbose(string Mensagem)
        {
            _Source.Verbose(Mensagem);
        }

        public void Debug(string Mensagem)
        {
            _Source.Debug(Mensagem);
        }

        public void Information(string Mensagem)
        {
            _Source.Information(Mensagem);
        }

        public void Warning(string Mensagem)
        {
            _Source.Warning(Mensagem);
        }

        public void Error(Exception Exeption)
        {
            _Source.Error(Exeption);
        }

        public void Error(Exception Exception, string Mensagem)
        {
            _Source.Error(Exception, Mensagem);
        }

        public void Fatal(Exception Exception)
        {
            _Source.Fatal(Exception);
        }

        public void Fatal(Exception Exception, string Mensagem)
        {
            _Source.Fatal(Exception, Mensagem);
        }

        public IFullLoggerProvider Expanded => _Source;

        #endregion
    }
}
