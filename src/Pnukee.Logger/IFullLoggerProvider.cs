using Serilog;
using System;

namespace Pnukee.Logger
{
    public interface IFullLoggerProvider : ILogger, ILoggerContext
    {
        void Error(Exception Exception);
        void Fatal(Exception Exception);
    }
}