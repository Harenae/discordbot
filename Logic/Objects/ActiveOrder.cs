using Discord;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot_G.Logic.Objects
{
    internal class ActiveOrder : IDisposable
    {
        /// <summary>
        /// Base chance for every participant
        /// </summary>
        private const int baseChance = 5;
        /// <summary>
        /// Contain guild id where this order is publish
        /// </summary>
        private ulong serverID { get; set; }
        /// <summary>
        /// Contain channel id by where this order is publish
        /// </summary>
        private ulong channelID { get; set; }
        /// <summary>
        /// Contain message id by this order
        /// </summary>
        internal ulong messageID { get; set; }
        /// <summary>
        /// Set <see langword="true"/> when this order is complete and ready for dispose; otherwise <see langword="false"/>
        /// </summary>
        internal bool disposable { get; set; }
        /// <summary>
        /// Time when the order was register
        /// </summary>
        DateTime register { get; set; }
        /// <summary>
        /// Main message which sended at the begining 
        /// </summary>
        Discord.EmbedBuilder mainMesageEmbed { get; set; }
        /// <summary>
        /// Message which sended when someone win
        /// </summary>
        Discord.EmbedBuilder winningMessageEmbed { get; set; }
        /// <summary>
        /// Message which sended to user to DM
        /// </summary>
        Discord.EmbedBuilder? userMessage { get; set; }
        /// <summary>
        /// Dictionary with custom variables
        /// </summary>
        Dictionary<string, string>? variables { get; set; }
        /// <summary>
        /// List with participants
        /// </summary>
        internal Participants<Link> links { get; set; }
        /// <summary>
        /// Timer
        /// </summary>
        private System.Timers.Timer TimeStamp { get; set; }
        /// <summary>
        /// Regular expresion for search any parenthesis
        /// </summary>
        private readonly Regex parenthesis = new(@"\{(?>\{(?<c>)|[^{}]+|\}(?<-c>))*(?(c)(?!))\}");
        /// <summary>
        /// Regular expresion for search reserved names
        /// </summary>
        private readonly Regex reserved = new(@"(?>\{participant\})|(?>\{chance\})|(?>\{winner\})");
        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="serverID"></param>
        /// <param name="channelID"></param>
        /// <param name="register"></param>
        /// <param name="mainMesageEmbed"></param>
        /// <param name="winningMessageEmbed"></param>
        /// <param name="userMessage"></param>
        /// <param name="variables"></param>
        internal ActiveOrder(ulong serverID, ulong channelID, DateTime register,
            Discord.EmbedBuilder mainMesageEmbed, Discord.EmbedBuilder winningMessageEmbed, Discord.EmbedBuilder? userMessage, Dictionary<string, string>? variables)
        {
            this.serverID = serverID;
            this.channelID = channelID;
            this.register = register;
            this.mainMesageEmbed = mainMesageEmbed;
            this.winningMessageEmbed = winningMessageEmbed;
            this.userMessage = userMessage;
            this.variables = variables;

            if (variables is not null) mainMesageEmbed = ReplaceKeysWithValues(mainMesageEmbed);

            disposable = false;

            links = new();
            links.ValueChanged += ParticipantsOnValueChanged;

            TimeStamp = new() { AutoReset = false, Interval = mainMesageEmbed.Timestamp.Value.Subtract(DateTime.UtcNow).TotalMilliseconds };
            TimeStamp.Elapsed += TimeStamp_Elapsed;

            Publish();
        }
        /// <summary>
        /// Parse embeds for variables and replaces them with value if it founded
        /// </summary>
        /// <param name="embed"></param>
        /// <returns></returns>
        private Discord.EmbedBuilder ReplaceKeysWithValues(Discord.EmbedBuilder embed)
        {
            Func<string, IEnumerable<string>> matcher = (text) =>
            parenthesis.Matches(text).
            Where(x => !reserved.IsMatch(x.Value)).
            Select(x => x.Value);

            Func<string, string> replacer = (text) =>
            {
                foreach (var i in matcher(text))
                {
                    string key = i.Substring(1, i.Length - 2);

                    if (variables.ContainsKey(key))
                        text = Regex.Replace(text, i, variables[key]);
                    else
                        text = Regex.Replace(text, i, string.Empty);
                }
                return text;
            };

            embed.Title = replacer(embed.Title);
            embed.Description = replacer(embed.Description);

            embed.Footer.Text = replacer(embed.Footer.Text);

            if (embed.Fields.Count > 0)
                foreach (var i in embed.Fields)
                {
                    if (i.Name is not null)
                        i.Name = replacer(i.Name);
                    if (i.Value is not null)
                        i.Value = replacer(i.Value.ToString());
                }

            return embed;
        }
        /// <summary>
        /// Fired when button pressed and update list with participants
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal async Task ButtonExecuted(Discord.WebSocket.SocketMessageComponent arg)
        {
            if (links.isExist(arg.User.Id)) links.RemoveItem(links.GetLinkByUserID(arg.User.Id));
            else if (await Bot.sqlite.Users.AnyAsync(x => x.UserID == arg.User.Id))
                if (await Bot.sqlite.Links.AnyAsync(x => x.ServerID == this.serverID && x.UserID == arg.User.Id))
                    links.AddItem(await Bot.sqlite.Links.FirstAsync(x => x.ServerID == this.serverID && x.UserID == arg.User.Id));
                else
                {
                    var link = new Link() { Chance = 0, UserID = arg.User.Id, ServerID = this.serverID };

                    await Bot.sqlite.Links.AddAsync(link);
                    links.AddItem(link);
                }
            else
            {
                var user = new User() { UserID = arg.User.Id };
                var link = new Link() { Chance = 0, UserID = arg.User.Id, ServerID = this.serverID };

                await Bot.sqlite.Users.AddAsync(user);
                await Bot.sqlite.Links.AddAsync(link);
                links.AddItem(link);
            }

            await Bot.sqlite.SaveChangesAsync();
        }
        /// <summary>
        /// Fired when timer is over and choose the winner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TimeStamp_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var channel = await Program._client.GetChannelAsync(channelID) as IMessageChannel;
            
            Regex winner = new(@"(?>\{winner\})");
            Func<EmbedBuilder, IUser, EmbedBuilder> replacer = (embed, winnerUser) =>
            {
                string name = winnerUser.Mention;

                embed.Title = embed.Title is not null ? Replace(embed.Title) : null;
                embed.Description = embed.Description is not null ? Replace(embed.Description) : null;
                if (embed.Footer is not null && embed.Footer.Text is not null)
                    embed.Footer.Text = Replace(embed.Footer.Text);

                return embed;

                string Replace(string text) => winner.Replace(text, name);
            };

            IUser user;
            if (links.GetCount() == 0)
            {
                mainMesageEmbed.Timestamp = DateTime.UtcNow.AddMilliseconds(TimeStamp.Interval);
                await channel.ModifyMessageAsync(messageID, x => x.Components = new ComponentBuilder().Build());

                Publish();

                TimeStamp.Start();
                return;
            }
            else if (links.GetCount() == 1)
            {
                user = await Program._client.GetUserAsync(links.GetFirst().UserID);
                
                winningMessageEmbed = ReplaceKeysWithValues(winningMessageEmbed);
                links.Winner(user.Id);

                await channel.SendMessageAsync(null, false, replacer(winningMessageEmbed, user).Build());

                if (userMessage is not null)
                {
                    userMessage = ReplaceKeysWithValues(userMessage);
                    await user.SendMessageAsync(null, false, replacer(userMessage, user).Build());
                }
            }
            else
            {
                Random random = new();

                int count = 0;
                int dice = random.Next(0, links.GetPool() + baseChance * links.GetCount());

                foreach (var i in links.GetLinks())
                    if (dice >= count && (i.Chance + baseChance) + count > dice)
                    {
                        user = await Program._client.GetUserAsync(i.UserID);

                        winningMessageEmbed = ReplaceKeysWithValues(winningMessageEmbed);
                        links.Winner(user.Id);

                        await channel.SendMessageAsync(null, false, replacer(winningMessageEmbed, user).Build());

                        if (userMessage is not null)
                        {
                            userMessage = ReplaceKeysWithValues(userMessage);
                            await user.SendMessageAsync(null, false, replacer(userMessage, user).Build());
                        }

                        break;
                    }
                    else
                        count += (i.Chance + baseChance);
            }
            
            Bot.sqlite.Links.UpdateRange(links);
            await Bot.sqlite.SaveChangesAsync();

            disposable = true;

            await channel.ModifyMessageAsync(messageID, x => x.Components = new ComponentBuilder().Build());
        }
        /// <summary>
        /// Publish message to Discord server
        /// </summary>
        private async void Publish()
        {
            ComponentBuilder takePartButton;
            var button = new ButtonBuilder()
            {
                Label = "Take part",
                CustomId = "takePart",
                Style = ButtonStyle.Primary
            };

            takePartButton = new ComponentBuilder().WithButton(button);

            string temp = mainMesageEmbed.Description;
            var pattern = parenthesis.Matches(temp).Where(x => reserved.IsMatch(x.Value));

            if (pattern.Count() != 0)
                foreach (var i in pattern)
                    mainMesageEmbed.Description = mainMesageEmbed.Description.Replace(i.Value, string.Empty);

            var channel = await Program._client.GetChannelAsync(channelID) as IMessageChannel;
            var message = await channel.SendMessageAsync(null, false, mainMesageEmbed.Build(), components: takePartButton.Build());

            mainMesageEmbed.Description = temp;
            messageID = message.Id;

            TimeStamp.Start();
        }
        /// <summary>
        /// Returns <see langword="true"/> if this active order message ID is equal to incoming message id; otherwise <see langword="false"/>
        /// </summary>
        /// <param name="messageID"></param>
        /// <returns></returns>
        internal bool isThisByMessage(ulong messageID) => this.messageID == messageID;
        /// <summary>
        /// Returns <see langword="true"/> if this active order message ID is equal to incoming message id and ready to dispose; otherwise <see langword="false"/>
        /// </summary>
        /// <param name="messageID"></param>
        /// <returns></returns>
        internal bool isThisDisposable(ulong messageID) => this.messageID == messageID && disposable == true;
        /// <summary>
        /// Fired when any item add/remove on links
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ParticipantsOnValueChanged(object sender, EventArgs e)
        {
            Regex participant = new(@"(?>\{participant\})");
            Regex chance = new(@"(?>\{chance\})");

            if (participant.IsMatch(mainMesageEmbed.Description))
            {
                if (links.GetCount() != 0)
                {
                    IUser user;
                    int pool = links.GetPool() + baseChance * links.GetCount();
                    string temp = mainMesageEmbed.Description;

                    string data = parenthesis.Matches(temp).First(x => reserved.IsMatch(x.Value)).Value;

                    StringBuilder participants = new StringBuilder();

                    foreach (var i in links.GetLinks())
                    {
                        user = await Program._client.GetUserAsync(i.UserID);
                        participants.AppendLine(data.Substring(1, data.Length - 2)).
                            Replace(@"{participant}", user.Mention).
                            Replace(@"{chance}", Math.Round(((double)i.Chance + baseChance) / pool * 100, 1).ToString());
                    }

                    mainMesageEmbed.Description = mainMesageEmbed.Description.Replace(data, participants.ToString());

                    var channel = await Program._client.GetChannelAsync(channelID) as IMessageChannel;
                    await channel.ModifyMessageAsync(messageID, x => x.Embed = mainMesageEmbed.Build());

                    mainMesageEmbed.Description = temp;
                }
                else
                {
                    string temp = mainMesageEmbed.Description;
                    var pattern = parenthesis.Matches(temp).Where(x => reserved.IsMatch(x.Value));

                    if (pattern.Count() != 0)
                        foreach (var i in pattern)
                            mainMesageEmbed.Description = mainMesageEmbed.Description.Replace(i.Value, string.Empty);

                    var channel = await Program._client.GetChannelAsync(channelID) as IMessageChannel;
                    await channel.ModifyMessageAsync(messageID, x => x.Embed = mainMesageEmbed.Build());

                    mainMesageEmbed.Description = temp;
                }
            }
        }
        /// <summary>
        /// Release all using resources
        /// </summary>
        public void Dispose()
        {
            TimeStamp.Stop();
            TimeStamp.Elapsed -= TimeStamp_Elapsed;
            TimeStamp.Dispose();

            links.ValueChanged -= ParticipantsOnValueChanged;
            links.Dispose();
        }
        /// <summary>
        /// Release all using resources and return this active order like item
        /// </summary>
        /// <returns></returns>
        public ActiveOrder DisposeWithReturn()
        {
            TimeStamp.Stop();
            TimeStamp.Elapsed -= TimeStamp_Elapsed;
            TimeStamp.Dispose();

            links.ValueChanged -= ParticipantsOnValueChanged;
            links.Dispose();

            return this;
        }
    }
}