using Discord;

namespace DiscordBot_G.Core.Logger
{
    public interface ILogger
    {
        public Task Log(LogMessage message);
    }
}