using System;

namespace Pnukee.Logger
{
    public interface ILoggerProvider : ILoggerContext
    {
        void Verbose(string Mensagem);
        void Debug(string Mensagem);
        void Information(string Mensagem);
        void Warning(string Mensagem);
        void Error(Exception Exeption);
        void Error(Exception Exception, string Mensagem);
        void Fatal(Exception Exception);
        void Fatal(Exception Exception, string Mensagem);
        IFullLoggerProvider Expanded { get; }
    }
}