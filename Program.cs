using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DiscordBot_G.Core;
using DiscordBot_G.Logic;
using DiscordBot_G.Core.Logger;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace DiscordBot_G
{
    internal class Program
    {
        /// <summary>
        /// Discord client for entire program
        /// </summary>
        internal static DiscordSocketClient _client;
        /// <summary>
        /// Discord bot token
        /// </summary>
        private const string Token = "MTA0NjA2NTMyODUwNzA2MDMwNA.G4dwLm.sUfyWzO7ADVjcDs2gQuZFbsmYhqVQOvfobbmpY";
        /// <summary>
        /// Run asynchronous task
        /// </summary>
        /// <returns></returns>
        static Task Main() => new Program().MainAsync();
        /// <summary>
        /// Return <see langword="true"></see> if current operation system is Linux; otherwise <see langword="false"></see>
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
        /// <summary>
        /// Host property initialization.
        /// </summary>
        /// <returns></returns>
        private async Task MainAsync()
        {
            if (!IsLinux)
            {
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    StringBuilder error = new();
                    error.AppendLine("This program requires administrator permission for running HTTP Listener and get POST requests.").
                        AppendLine("Please check your permission access and try again.").
                        AppendLine("Press any key to exit.");

                    Console.WriteLine(error.ToString());
                    Console.ReadKey();

                    Environment.Exit(0);
                }
            }

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
            services
            .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = Discord.GatewayIntents.AllUnprivileged,
                LogGatewayIntentWarnings = false,
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Debug,
                UseInteractionSnowflakeDate = false
            }))
            .AddTransient<ConsoleLogger>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>()
            .AddSingleton(x => new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug,
                DefaultRunMode = Discord.Commands.RunMode.Async
            }))
            .AddSingleton<PrefixHandler>())
            .Build();

            await RunAsync(host);
        }
        /// <summary>
        /// Initialization of properties, modules and events of the bot. Bot launch.
        /// </summary>
        /// <param name="host">Host properties</param>
        /// <returns></returns>
        private async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var commands = provider.GetRequiredService<InteractionService>();
            _client = provider.GetRequiredService<DiscordSocketClient>();

            await provider.GetRequiredService<InteractionHandler>().InitializeAsync();

            _client.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);
            commands.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);

            _client.Ready += async () => await commands.RegisterCommandsGloballyAsync(true);            

            await _client.LoginAsync(TokenType.Bot, Token);
            await _client.StartAsync();

            try
            {
                Bot.BotInitialize();
            } catch (Exception ex) { Console.WriteLine(string.Join("\n", ex.Message, "Press any key to exit.")); Console.ReadKey(); Environment.Exit(0); }
            await Task.Delay(-1);
        }
    }
}