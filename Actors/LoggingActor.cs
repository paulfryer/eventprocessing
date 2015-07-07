using Akka.Actor;
using Serilog;

namespace MusicIndexer.Actors
{
    public class LoggingActor : ReceiveActor
    {
        public LoggingActor()
        {
            Receive<LogMessage>(logMessage => Log.Logger.Information(logMessage.Message));
        }

        public class LogMessage
        {
            public LogMessage(string message)
            {
                Message = message;
            }

            public string Message { get; private set; }
        }
    }
}