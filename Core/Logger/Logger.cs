using Discord;

namespace DiscordBot_G.Core.Logger
{
    public abstract class Logger : ILogger
    {
        public string _guid;
        public Logger() => _guid = Guid.NewGuid().ToString()[^4..];

        public abstract Task Log(LogMessage message);
    }
}