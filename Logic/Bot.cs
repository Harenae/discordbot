using DiscordBot_G.Core;
using DiscordBot_G.Logic.Objects;
using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot_G.Logic
{
    internal static class Bot
    {
        /// <summary>
        /// Data base
        /// </summary>
        internal static SqliteDataBase sqlite;
        /// <summary>
        /// Http server for getting POST request
        /// </summary>
        private static HttpServer httpServer;
        /// <summary>
        /// Collection active orders
        /// </summary>
        private static List<ActiveOrder> activeOrders;
        /// <summary>
        /// Initialize base objects
        /// </summary>
        internal static void BotInitialize()
        {
            sqlite = new();
            httpServer = new();

            activeOrders = new();

            Program._client.ModalSubmitted += EventModalSubmited;
            Program._client.ButtonExecuted += _client_ButtonExecuted;
            Program._client.MessageDeleted += _client_MessageDeleted;
            Program._client.MessageUpdated += _client_MessageUpdated;
            
            httpServer.Start();
        }
        /// <summary>
        /// Fired when any message is updated and if updated message one of a part of any active order and this order have flag disposable equal <see langword="true"></see> then it will be removed
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns></returns>
        private static async Task _client_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            if (activeOrders.Any(x => x.isThisDisposable(arg1.Id)))
                activeOrders.Remove(activeOrders.Find(x => x.isThisByMessage(arg1.Id)).DisposeWithReturn());
        }
        /// <summary>
        /// Fired when any message is deleted, and if the deleted message is part of any active order, then that order will be deleted
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        private static async Task _client_MessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
        {
            if (activeOrders.Any(x => x.isThisByMessage(arg1.Id)))
                activeOrders.Remove(activeOrders.Find(x => x.isThisByMessage(arg1.Id)).DisposeWithReturn());
        }
        /// <summary>
        /// Fired when a button is clicked and updates the list of members in that order
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static async Task _client_ButtonExecuted(SocketMessageComponent arg)
        {
            if (activeOrders.Any(x => x.isThisByMessage(arg.Message.Id)))
                switch (arg.Data.CustomId)
                {
                    case "takePart":
                        await activeOrders.Find(x => x.isThisByMessage(arg.Message.Id)).ButtonExecuted(arg);
                        break;
                }

            await arg.DeferAsync();
        }
        /// <summary>
        /// Fired when someone use slash command 'report' to request report for a specified time period
        /// </summary>
        /// <param name="arg">Modal component</param>
        /// <returns></returns>
        internal static async Task EventModalSubmited(SocketModal arg)
        {
            List<SocketMessageComponentData> components = arg.Data.Components.ToList();

            DateTime from = new();
            DateTime to = new();

            string pattern = "MM-dd-yyyy";

            if (DateTime.TryParseExact(
                components.First(x => x.CustomId == "_report_from").Value,
                pattern,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out from)
                && DateTime.TryParseExact(
                    components.First(x => x.CustomId == "_report_to").Value,
                    pattern,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out to))
            {
                var orders = sqlite.Orders.Where(x =>
                    x.ServerID == arg.GuildId
                    && DateTime.Compare(from, x.Register) <= 0 && DateTime.Compare(x.Register, to) <= 0);

                if(orders.Any())
                {
                    StringBuilder reports = new();

                    foreach (var i in orders)
                        reports.AppendJoin("|", String.Format("{0:r}", i.Register), i.JSONData).Append("\n");

                    var user = await Program._client.GetUserAsync(arg.User.Id);

                    MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(reports.ToString()));

                    await user.SendFileAsync(memoryStream, "reports.txt", "Report");
                    await arg.RespondAsync("Check your DM");
                }
                else
                    await arg.RespondAsync("Nothing found for this period");                
            }
            else
                await arg.RespondAsync("Invalid format");            
        }
        /// <summary>
        /// Register new order
        /// </summary>
        /// <param name="token">Unique token</param>
        /// <param name="channelID">Channel ID</param>
        /// <param name="discordEmbed">Json string by discohook</param>
        internal static void RegisterOrder(string token, ulong channelID, DiscordEmbed discordEmbed, Dictionary<string, string>? variables)
        {
            if (discordEmbed.embeds.Count == 1 || discordEmbed.embeds.Count == 0)
                throw new Exception("Missing embeds. POST must contain at least two of these in order to post a message and successfully modify");

            var serverID = sqlite.Servers.FirstAsync(x => x.Token == token).GetAwaiter().GetResult();
            
            Func<Objects.Embed, Discord.EmbedBuilder> getbuilder = (tempEmbed) =>
            {
                Discord.EmbedBuilder builder = new()
                {
                    Color = tempEmbed.color is not null ? new Discord.Color((uint)tempEmbed.color) : null,
                    Description = tempEmbed.description is not null ? tempEmbed.description : null,
                    ImageUrl = tempEmbed.image is not null ? tempEmbed.image.url : null,
                    Url = tempEmbed.url is not null ? tempEmbed.url : null,
                    ThumbnailUrl = tempEmbed.thumbnail is not null ? tempEmbed.thumbnail.url : null,
                    Timestamp = tempEmbed.timestamp.ToUniversalTime(),
                    Title = tempEmbed.title is not null ? tempEmbed.title : null,
                    Footer = tempEmbed.footer is not null ? new Discord.EmbedFooterBuilder() 
                    { 
                        IconUrl = tempEmbed.footer.icon_url is not null ? tempEmbed.footer.icon_url : null,
                        Text = tempEmbed.footer.text is not null ? tempEmbed.footer.text : null,
                    } : null
                };

                if (tempEmbed.fields is not null)
                {
                    List<Discord.EmbedFieldBuilder> fileds = new();
                    foreach (var i in tempEmbed.fields)
                        fileds.Add(new Discord.EmbedFieldBuilder()
                        {
                            Name = i.name,
                            Value = i.value
                        });

                    builder.Fields = fileds;
                }

                return builder;
            };

            Objects.Embed mainMessage;
            Objects.Embed winningMessage;
            Objects.Embed userMessage;

            try
            {
                if (discordEmbed.embeds.Count == 2)
                {
                    mainMessage = discordEmbed.embeds[0];
                    winningMessage = discordEmbed.embeds[1];

                    int mseconds = mainMessage.timestamp.Subtract(DateTime.UtcNow).Minutes;

                    if (mainMessage.timestamp.Subtract(DateTime.UtcNow).Minutes >= 1)
                        Task.Run(() => activeOrders.Add(new ActiveOrder(
                            serverID.ServerID, channelID, DateTime.Now,
                            getbuilder(mainMessage), getbuilder(winningMessage), null, variables is not null ? variables : null)));
                    else throw new Exception("Invalid parameter TimeStamp! The difference with the current time must be more than 1 minute");
                }
                else
                {
                    mainMessage = discordEmbed.embeds[0];
                    winningMessage = discordEmbed.embeds[1];
                    userMessage = discordEmbed.embeds[2];

                    if (mainMessage.timestamp.Subtract(DateTime.UtcNow).Minutes >= 1)                    
                        Task.Run(() => activeOrders.Add(new ActiveOrder(
                            serverID.ServerID, channelID, DateTime.Now,
                            getbuilder(mainMessage), getbuilder(winningMessage), getbuilder(userMessage), variables is not null ? variables : null)));
                    else throw new Exception("Invalid parameter TimeStamp! The difference with the current time must be more than 1 minute");
                }

                sqlite.Orders.AddAsync(new Order()
                {
                    ServerID = serverID.ServerID,
                    ChanneID = channelID,
                    Register = DateTime.Now,
                    JSONData = Newtonsoft.Json.JsonConvert.SerializeObject(discordEmbed)
                }); sqlite.SaveChangesAsync();
            } catch (Exception ex)
            {
                if (ex.Message == "Invalid parameter TimeStamp! The difference with the current time must be more than 1 minute")
                    throw;
                else
                    throw new Exception("Cannot parse embeds. May embeds was corrupted. Check your embeds and try again."); 
            }
        }
        /// <summary>
        /// Register new guild
        /// </summary>
        /// <param name="serverID">Guild ID</param>
        /// <returns></returns>
        internal static async Task<string> RegisterGuild(ulong serverID)
        {
            if (await sqlite.Servers.AnyAsync(x => x.ServerID == serverID))
                sqlite.Servers.Remove(await sqlite.Servers.FirstAsync(x => x.ServerID == serverID));

            string token = Guid.NewGuid().ToString();

            await sqlite.Servers.AddAsync(new Server() { ServerID = serverID, Token = token });
            await sqlite.SaveChangesAsync();

            return token;
        }
        /// <summary>
        /// Return <see langword="true"/> if guild already registered; otherwise <see langword="false"/>
        /// </summary>
        /// <param name="serverID">Guild ID</param>
        /// <returns></returns>
        internal static bool IsRegisterGuild(ulong serverID) => sqlite.Servers.AnyAsync(x => x.ServerID == serverID).GetAwaiter().GetResult();
        /// <summary>
        /// Return <see langword="true"/> if token already exist in data base; otherwise <see langword="false"/>
        /// </summary>
        /// <param name="token">Unique token</param>
        /// <returns></returns>
        internal static bool isExist(string token) => sqlite.Servers.AnyAsync(x => x.Token == token).GetAwaiter().GetResult();
        /// <summary>
        /// Return <see langword="true"/> if channel exist; otherwise <see langword="false"/>
        /// </summary>
        /// <param name="channelID">Channel ID</param>
        /// <returns></returns>
        internal static bool isExist(string token, ulong channelID) 
        {
            ulong serverID = sqlite.Servers.FirstAsync(x => x.Token == token).GetAwaiter().GetResult().ServerID;
            var guild = Program._client.GetGuild(serverID);

            return guild.GetChannel(channelID) is not null ? true : false;
        }
        /// <summary>
        /// Try to deserialize object and return <see langword="true"/> if it can exist; otherwise <see langword="false"/>
        /// </summary>
        /// <param name="json">Input JSON string by POST</param>
        /// <param name="discordEmbed">Local embed</param>
        /// <param name="message">Exception message</param>
        /// <returns></returns>
        internal static bool isExist(string json, out DiscordEmbed? discordEmbed, out string? message)
        {
            try
            {
                discordEmbed = Newtonsoft.Json.JsonConvert.DeserializeObject<DiscordEmbed>(json);
                message = null;
                return true;
            } 
            catch (Exception ex) 
            {
                discordEmbed = null;
                message = ex.Message;
                return false;
            }
        }
    }
}
