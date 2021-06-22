using Serilog.Core;
using Serilog.Events;

namespace Pnukee.Logger.Enrichers
{
    public class StackEnricher : ILogEventEnricher
    {

        public const string PropertyName = "Stack";

        #region Implementation of ILogEventEnricher

        /// <summary>Enrich the log event.</summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {

            var StackInfo = new StackInfo();
            StackInfo.GetStackFrame();

            var Props = new LogEventProperty[4];

            Props[0] = propertyFactory.CreateProperty("Stack.Method", StackInfo.Method);
            Props[1] = propertyFactory.CreateProperty("Stack.Class", StackInfo.Class);
            Props[2] = propertyFactory.CreateProperty("Stack.Line", StackInfo.Line);
            Props[3] = propertyFactory.CreateProperty("Stack.File", StackInfo.File);

            logEvent.AddPropertyIfAbsent(Props[0]);
            logEvent.AddPropertyIfAbsent(Props[1]);
            logEvent.AddPropertyIfAbsent(Props[2]);
            logEvent.AddPropertyIfAbsent(Props[3]);

        }


        #endregion


        private class StackInfo
        {
            public void GetStackFrame()
            {
                var Frame = new System.Diagnostics.StackFrame(9, false);
                this.Method = Frame.GetMethod().Name;
                this.Class = Frame.GetFileName();
                this.Line = Frame.GetFileLineNumber();
                this.File = Frame.GetFileName();
            }

            public string File { get; set; }

            public int Line { get; set; }

            public string Class { get; set; }

            public string Method { get; set; }
        }
    }

}
