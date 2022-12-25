using Discord;

namespace DiscordBot_G.Core.Logger
{
    public class ConsoleLogger : Logger
    {
        // Using Task.Run() in case there are any long running actions, to prevent blocking gateway
        public override async Task Log(LogMessage message) => Task.Run(() => LogToConsoleAsync(this, message));
        
        private async Task LogToConsoleAsync<T>(T logger, LogMessage message) where T : ILogger => Console.WriteLine($"guid:{_guid} : " + message);
    }
}