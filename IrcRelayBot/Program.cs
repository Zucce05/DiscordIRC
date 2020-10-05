using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using IrcRelayBot.classes;
using IrcRelayBot.json;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace IrcRelayBot
{
    class Program
    {
        // Create the client
        public static DiscordSocketClient client;
        // Instantiate the configuration for the bot. This is where the token is stored.
        BotConfig botConfig = new BotConfig();
        // Set Dictionary for cross-server channels
        public static ConcurrentDictionary<ulong, IrcChannel> ircDictionary = new ConcurrentDictionary<ulong, IrcChannel>();
        public static ConcurrentDictionary<ulong, IrcMessageBase> ircMessages = new ConcurrentDictionary<ulong, IrcMessageBase>();

        // Set timers
        public static Timer CleanServersTimer;

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

            // Set CleanServersTimer Interval
            CleanServersTimer = new Timer
            {
                Interval = 3600000
            };

            // Populate the configuration from the BotConfig.json file for the client to use when connecting
            BotConfiguration(ref botConfig);

            // Create event handlers and start the bot
            client.Log += Log;
            string token = botConfig.Token;

            // Use Message Received for all message handling
            client.MessageReceived += MessageReceived;
            client.MessageDeleted += MessageDeleted;
            client.MessageUpdated += MessageUpdated;
            client.ReactionAdded += ReactionAdded;

            CleanServersTimer.Elapsed += CleanServerList;
            // Connect client
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            CleanServersTimer.Enabled = true;

            // Wait for events to happen
            await Task.Delay(-1);
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (ircMessages.TryGetValue(message.Id, out IrcMessageBase ircMessage))
            {
                await ircMessage.PropogateReactions(reaction.MessageId, reaction);
            }
        }

        private async Task MessageUpdated(Cacheable<IMessage, ulong> originalMessage, SocketMessage updatedMessage, ISocketMessageChannel channel)
        {
            if (ircMessages.TryGetValue(updatedMessage.Id, out IrcMessageBase ircMessage))
            {
                await ircMessage.EditMessage(updatedMessage);
            }
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> deletedMessage, ISocketMessageChannel channel)
        {
            if (ircMessages.TryGetValue(deletedMessage.Id, out IrcMessageBase ircMessage))
            {
                await ircMessage.DeleteMessage();
            }
        }

        private async void CleanServerList(object sender, EventArgs e)
        {
            List<ulong> channelsToRemove = new List<ulong>();
            ImmutableList<SocketGuild> connectedGuilds = client.Guilds.ToImmutableList<SocketGuild>();

            foreach (KeyValuePair<ulong, IrcChannel> channel in ircDictionary)
            {
                bool isStillConnected = false;
                foreach (SocketGuild guild in connectedGuilds)
                {
                    if (channel.Value.GuildID == guild.Id)
                    {
                        isStillConnected = true;
                        break;
                    }
                }
                if (!isStillConnected)
                {
                    channelsToRemove.Add(channel.Key);
                }
            }
            try
            {
                foreach (ulong channelId in channelsToRemove)
                {
                    //ircDictionary.TryGetValue(channelId, out IrcChannel value);
                    ircDictionary.TryRemove(channelId, out IrcChannel value);
                    await Program.Log(new LogMessage(LogSeverity.Verbose, "CleanServerList", $"Guild with ID:{value.GuildID} no longer present. Removing channel from {value.Topic}"));
                }
                using (StreamWriter file = File.CreateText("json/IrcChannel.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, ircDictionary);
                }
            }
            catch (System.IO.IOException ioException)
            {
                await Program.Log(new LogMessage(LogSeverity.Verbose, "CleanServerList", $"Error: {ioException.Message}"));
            }

            // CLEAN ircMessage LIST -> NEEDS TO BE MOVED TO HANDLE SEPARATELY
            //List<ulong> messagesToRemove = new List<ulong>();
            //foreach (IrcMessageBase message in ircMessages.Values)
            //{
            //    if (message.MessageTime.AddDays(6) > DateTime.UtcNow)
            //    {
            //        messagesToRemove.Add(message.MessageId);
            //    }
            //}
            //foreach(ulong messageId in messagesToRemove)
            //{
            //    ircMessages.TryRemove(messageId, out IrcMessageBase value);
            //}
            //using (StreamWriter file = File.CreateText("json/IrcMessage.json"))
            //{
            //    JsonSerializer serializer = new JsonSerializer();
            //    serializer.Serialize(file, ircMessages);
            //}

        }

        /// <summary>
        /// Handle all incoming message events.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task MessageReceived(SocketMessage message)
        {
            await MessageCommands.MessageReceived(message, ircDictionary, ircMessages);
        }



        // TODO: Convert to text file, add my own logging to this
        /// <summary>
        /// Logging messages
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static Task Log(LogMessage msg)
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
                ircDictionary = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, IrcChannel>>(File.ReadAllText("json/IrcChannel.json"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Executable Level SetUp Exception:\n\t{e.Message}");
            }

            try
            {
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/IrcMessage.json"));
                ircMessages = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, IrcMessageBase>>(File.ReadAllText("json/IrcMessage.json"));
                if (ircMessages == null)
                {
                    ircMessages = new ConcurrentDictionary<ulong, IrcMessageBase>();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Executable Level SetUp Exception:\n\t{e.Message}");
            }
        }
    }
}
