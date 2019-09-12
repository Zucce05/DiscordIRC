using Discord;
using Discord.WebSocket;
using IrcRelayBot.classes;
using IrcRelayBot.json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IrcRelayBot
{
    class Program
    {
        // Create the client
        public static DiscordSocketClient client;
        // Instantiate the configuration for the bot. This is where the token is stored.
        BotConfig botConfig = new BotConfig();
        // Set Dictionary for cross-server channels
        public static Dictionary<ulong, IrcChannel> ircDictionary = new Dictionary<ulong, IrcChannel>();

        // Entry point, immediately run everything async
        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        /// <summary>
        /// "Real" Main() so everything is async.
        /// </summary>
        /// <returns></returns>
        public async Task MainAsync()
        {
            // Instantiate the client, and add the logging
            client = new DiscordSocketClient
            (new DiscordSocketConfig
            {
                //LogLevel = LogSeverity.Debug
                //LogLevel = LogSeverity.Verbose
                LogLevel = LogSeverity.Info
                //LogLevel = LogSeverity.Warning
            });

            // Populate the configuration from the BotConfig.json file for the client to use when connecting
            BotConfiguration(ref botConfig);

            // Create event handlers and start the bot
            client.Log += Log;
            string token = botConfig.Token;

            // Use Message Received for all message handling
            client.MessageReceived += MessageReceived;

            // Connect client
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // Wait for events to happen
            await Task.Delay(-1);
        }

        /// <summary>
        /// Handle all incoming message events.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task MessageReceived(SocketMessage message)
        {
            // First condition, only pay attention if the channel is used by the bot
            if (!message.Author.IsBot && ircDictionary.ContainsKey(message.Channel.Id))
            {
                ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel channelInfo);
                string channelTopic = channelInfo.Topic;
                string messageAuthor = message.Author.Username;
                string messageServer = ((IGuildChannel)message.Channel).Guild.Name.ToString();
                IReadOnlyCollection<IAttachment> attachments = message.Attachments;
                string returnMessage = $"{messageAuthor} in {messageServer}:\n{message.Content}";
                string attachmentUrls = string.Empty;

                if (message.Attachments.Count > 0)
                {
                    foreach (Attachment a in attachments)
                    {
                        attachmentUrls += $"{a.Url.ToString()}\n";
                    }
                }

                EmbedBuilder builder = new EmbedBuilder()
                {

                    ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                    Title = $"{messageAuthor} from server {messageServer}",
                    Description = $"{message.Content}",
                    //ImageUrl = $"{attachmentUrls}",
                };

                foreach (KeyValuePair<ulong, IrcChannel> entry in ircDictionary)
                {
                    if (entry.Value.Topic == channelTopic)
                    {
                        if (message.Channel.Id != entry.Key)
                        {
                            await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync($"{returnMessage}");
                            //await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync(string.Empty, false, builder.Build());
                            //await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync(attachmentUrls);
                            if (message.Attachments.Count > 0)
                            {
                                foreach (Attachment a in attachments)
                                {
                                    //attachmentUrls += $"\n{a.Url.ToString()}";
                                    await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync(a.Url);
                                }
                            }
                        }
                    }
                }
            }
            // Second condition, check for server owner key comamnds for bot configuration
            if (message.Author.Id == ((SocketTextChannel)message.Channel).Guild.OwnerId)
            {
                if (message.Content.StartsWith("+addirc"))
                {
                    if (!ircDictionary.ContainsKey(message.Channel.Id))
                    {
                        string msg = message.Content.Substring(1).ToLower();
                        string[] substring = msg.Split(" ", 2);

                        IrcChannel newChannel = new IrcChannel();
                        newChannel.Topic = substring[1].ToLower();
                        newChannel.GuildID = ((IGuildChannel)message.Channel).GuildId;
                        ircDictionary.Add(message.Channel.Id, newChannel);
                        await message.Channel.SendMessageAsync($"Channel now a linked to Discord IRC using the topic: \"**{substring[1]}**\".");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"This channel is already linked to a Discord IRC global chat.\nUse ``+removeirc`` to remove and reassign this channel.");
                    }
                }
                if (message.Content.StartsWith("+removeirc"))
                {
                    string msg = message.Content.Substring(1).ToLower();
                    string[] substring = msg.Split(" ", 2);

                    ircDictionary.Remove(message.Channel.Id);
                    await message.Channel.SendMessageAsync($"Channel is no longer a Discord IRC linked channel.");
                }
                using (StreamWriter file = File.CreateText("json/IrcChannel.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, ircDictionary);
                }
            }
            // General command to show the bot invite link
            // TODO: Add a new category of bot command with prefix to allow for help, invite, and other commands
            if (message.Content.StartsWith("+invite"))
            {
                await message.Channel.SendMessageAsync($"You can invite the bot by using this link: https://discordapp.com/api/oauth2/authorize?client_id=581557457864884225&permissions=116736&scope=bot");
            }
            if (message.Content.StartsWith("+help"))
            {
                string helpMessage = $"```\nEveryone:\n\t+invite: Provides an invite link to invite the bot to your server.\n\t+help: Displays this help text" +
                    $"\n\t+topic: Displays the topic (keyword) for the irc linked channel" +
                    $"\nOwner only:\n\t+addirc: Adds a channel to an irc topic\n\t\tUsage: +addirc <topic>\n\t+removeirc: Removes the channel from the irc topic it's a part of\n```";
                await message.Channel.SendMessageAsync(helpMessage);
            }
            if (message.Content.StartsWith("+topic"))
            {
                if (ircDictionary.ContainsKey(message.Channel.Id))
                {
                    ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel channel);
                    await message.Channel.SendMessageAsync($"Current channel topic: {channel.Topic}");
                }
            }
        }

        // TODO: Convert to text file, add my own logging to this
        /// <summary>
        /// Logging messages
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Uses BotConfig to populate a json file, used in startup and connection of the application.
        /// </summary>
        /// <param name="bc"></param>
        public static void BotConfiguration(ref BotConfig bc)
        {
            JsonTextReader reader;
            try
            {
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/BotConfig.json"));
                bc = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("json/BotConfig.json"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Executable Level SetUp Exception:\n\t{e.Message}");
            }

            try
            {
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/IrcChannel.json"));
                ircDictionary = JsonConvert.DeserializeObject<Dictionary<ulong, IrcChannel>>(File.ReadAllText("json/IrcChannel.json"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Executable Level SetUp Exception:\n\t{e.Message}");
            }
        }
    }
}
