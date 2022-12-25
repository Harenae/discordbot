using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using DiscordBot_G.Logic;
using DiscordBot_G.Core.Logger;
using Discord.WebSocket;

namespace DiscordBot_G.Modules
{
    public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands {get; set;}
        private static Logger _loger;
        public InteractionModule(ConsoleLogger loger) => _loger = loger;
        [SlashCommand("ping", "Receive a ping message")]
        public async Task Ping()
        {
            await RespondAsync(Context.User.Mention);
        }
        [SlashCommand("registration", "Registration your guild")]
        public async Task GetToken()
        {
            if (!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                await RespondAsync("Error! This command can only be used by an administrator");
                return;
            }
            else if (Bot.IsRegisterGuild(Context.Guild.Id))
            {
                await RespondAsync("Error! Can't register a second time\nIf you have lost your token, use the \"newtoken\" command.");
                return;
            }

            string token = Bot.RegisterGuild(Context.Guild.Id).GetAwaiter().GetResult();

            await Context.User.SendMessageAsync($"Your unique Token - {token}\nUse this token for your POST request");
            await RespondAsync("Check your DM");
        }
        [SlashCommand("newtoken", "Generate new token")]
        public async Task NewToken()
        {
            if (!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                await RespondAsync("Error! This command can only be used by an administrator");
                return;
            }

            string token = Bot.RegisterGuild(Context.Guild.Id).GetAwaiter().GetResult();

            await Context.User.SendMessageAsync($"Your unique Token - {token}\nUse this token for your POST request");
            await RespondAsync("Check your DM");
        }
        [SlashCommand("report","Get report")]
        public async Task Report()
        {
            if ((Context.User as SocketGuildUser).GuildPermissions.ManageMessages)
            {
                var modal = new ModalBuilder()
                    .WithTitle("Report")
                    .WithCustomId("_report")
                    .AddTextInput("From", "_report_from", placeholder:"MM-dd-yyyy | Example: 03-25-2022")
                    .AddTextInput("To", "_report_to", placeholder: "MM-dd-yyyy | Example: 03-28-2022");

                await RespondWithModalAsync(modal.Build());
            }
            else
            {
                await RespondAsync("You do not have permission to manage messages in this channel.");
            }
        }
    }
}
