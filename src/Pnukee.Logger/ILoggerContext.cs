using System;

namespace Pnukee.Logger
{
    public interface ILoggerContext
    {
        string Contexto { get; set; }

        IFullLoggerProvider Contextualize(string Contexto);

        Guid AgruparLogs(string Contexto);

        void ReleaseAgrupamento(Guid AgrupamentoID, bool ResetContext);

        void ResetContexto();
    }
}