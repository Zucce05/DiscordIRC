using Discord;
using Discord.Webhook;
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
            bool isBotCommand = false;
            // Condition, check for server owner key comamnds for bot configuration
            if (message.Author.Id == ((SocketTextChannel)message.Channel).Guild.OwnerId)
            {
                if (message.Content.StartsWith("+addirc"))
                {
                    isBotCommand = true;
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
                    isBotCommand = true;
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

                if (message.Content.StartsWith($"+addwebhook"))
                {
                    isBotCommand = true;
                    if (ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel value))
                    {
                        string[] substring = message.Content.Split(" ", 2);
                        value.WebhookString = substring[1];
                        using (StreamWriter file = File.CreateText("json/IrcChannel.json"))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(file, ircDictionary);
                        }
                        await message.Channel.SendMessageAsync($"Webhook added");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Faild to add webhook:\n\tEither this channel is not linked to Discord IRC, or there was another error.\n\t Please verify channel is linked then try again.");
                    }
                }
                if (message.Content.StartsWith($"+removewebhook"))
                {
                    isBotCommand = true;
                    if (ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel value))
                    {
                        value.WebhookString = string.Empty;
                        using (StreamWriter file = File.CreateText("json/IrcChannel.json"))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(file, ircDictionary);
                        }
                        await message.Channel.SendMessageAsync($"Webhook removed");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Faild to remove webhook:\n\tEither this channel is not linked to Discord IRC, or there was another error.\n\t Please verify channel is linked then try again.");
                    }
                }
            }
            // General command to show the bot invite link
            // TODO: Add a new category of bot command with prefix to allow for help, invite, and other commands
            if (message.Content.StartsWith("+invite"))
            {
                isBotCommand = true;
                await message.Channel.SendMessageAsync($"You can invite the bot by using this link: https://discordapp.com/api/oauth2/authorize?client_id=581557457864884225&permissions=116736&scope=bot");
            }
            if (message.Content.StartsWith("+help"))
            {
                isBotCommand = true;
                string helpMessage = $"```\nEveryone:\n\t+invite: Provides an invite link to invite the bot to your server.\n\t+help: Displays this help text";
                helpMessage += $"\n\t+topic: Displays the topic (keyword) for the irc linked channel";
                helpMessage += $"\n\t+linked: Displays a list of all linked servers";
                helpMessage += $"\n\t+topics: Displays a list of all topics, and how many servers are joined to each";

                helpMessage += $"\nOwner only:";
                helpMessage += $"\n\t+addirc: Adds a channel to an irc topic\n\t\tUsage: +addirc <topic>";
                helpMessage+= "\n\t+removeirc: Removes the channel from the irc topic it's a part of";
                helpMessage += $"\n\t+addwebhook\n\t\tUsage: +addwebhook <webhook URL>";
                helpMessage += $"\n\t+removewebhook";

                helpMessage += "\n```";
                await message.Channel.SendMessageAsync(helpMessage);
            }
            if (message.Content.StartsWith("+topic"))
            {
                isBotCommand = true;
                if (ircDictionary.ContainsKey(message.Channel.Id))
                {
                    ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel channel);
                    await message.Channel.SendMessageAsync($"Current channel topic: {channel.Topic}");
                }
            }
            if (message.Content.StartsWith("+linked"))
            {
                isBotCommand = true;
                int total = 0;
                if (ircDictionary.ContainsKey(message.Channel.Id))
                {
                    ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel source);
                    string sendMessage = "```\nLinked Servers:\n";
                    foreach (KeyValuePair<ulong, IrcChannel> entry in ircDictionary)
                    {
                        if (entry.Value.Topic == source.Topic)
                        {
                            sendMessage += $"{client.GetGuild(entry.Value.GuildID).Name}\n";
                            total++;
                        }
                    }
                    sendMessage += $"Total: {total}\n```";
                    await message.Channel.SendMessageAsync(sendMessage);
                }
                else
                {
                    await message.Channel.SendMessageAsync("This channel currently isn't linked to anything or anywhere.");
                }
            }
            if (message.Content.StartsWith($"+topics"))
            {
                isBotCommand = true;
                Dictionary<string, int> topics = new Dictionary<string, int>();
                foreach (KeyValuePair<ulong, IrcChannel> entry in ircDictionary)
                {
                    if(topics.ContainsKey(entry.Value.Topic))
                    {
                        topics[entry.Value.Topic] = topics[entry.Value.Topic] += 1;
                    }
                    else
                    {
                        topics.Add(entry.Value.Topic, 1);
                    }
                }

                string sendMessage = $"```**Current Topics**\n";
                foreach (KeyValuePair<string, int> topicCounts in topics)
                {
                    sendMessage += $"Topic: {topicCounts.Key} | Servers Linked: {topicCounts.Value}\n";
                }
                sendMessage += "```";
                await message.Channel.SendMessageAsync(sendMessage);
            }

            // Condition, only pay attention if the channel is used by the bot
            if (!message.Author.IsBot && ircDictionary.ContainsKey(message.Channel.Id) && !isBotCommand)
            {
                ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel channelInfo);
                string channelTopic = channelInfo.Topic;
                //string messageAuthor = message.Author.Username;
                string messageServer = ((IGuildChannel)message.Channel).Guild.Name.ToString();
                IReadOnlyCollection<IAttachment> attachments = message.Attachments;
                //string returnMessage = $"{messageAuthor} in {messageServer}:\n**{message.Content}**";
                string attachmentUrls = string.Empty;

                if (message.Attachments.Count > 0)
                {
                    foreach (Attachment a in attachments)
                    {
                        attachmentUrls += $"{a.Url.ToString()}\n";
                    }
                }

                //EmbedBuilder builder = new EmbedBuilder()
                //{

                //    ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                //    Title = $"User: {messageAuthor}\nServer: {messageServer}",
                //    Description = $"{message.Content}",
                //    //ImageUrl = $"{attachmentUrls}",
                //};
                //if (message.Attachments.Count > 0)
                //{
                //    foreach (Attachment a in attachments)
                //    {
                //        attachmentUrls += $"\n{a.Url.ToString()}";
                //        //builder.AddField(string.Empty, a);
                //    }
                //}

                foreach (KeyValuePair<ulong, IrcChannel> entry in ircDictionary)
                {
                    if (entry.Value.Topic == channelTopic)
                    {
                        if (message.Channel.Id != entry.Key)
                        {
                            
                            if (entry.Value.WebhookString != string.Empty)
                            {
                                using (DiscordWebhookClient discordWebhookClient = new DiscordWebhookClient(entry.Value.WebhookString))
                                {
                                    string jsonMessage = string.Empty;

                                    try
                                    {
                                        await discordWebhookClient.SendMessageAsync($"{message.Content + "\n" + attachmentUrls}", false, null, $"{message.Author.Username}", $"{message.Author.GetAvatarUrl()}");
                                    }
                                    catch (System.InvalidOperationException e)
                                    {
                                        await Log(new LogMessage(LogSeverity.Error, "Send Webhook Message", e.Message));
                                        await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync($"**User: {message.Author.Username}\nServer: {messageServer}**\n{message.Content}\n{attachmentUrls}");
                                    }

                                }
                            }
                            else
                            {
                                //await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync($"{returnMessage}");
                                //await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync(string.Empty, false, builder.Build());
                                //await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync(message.Content, false, builder.Build());
                                await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync($"**User: {message.Author.Username}\nServer: {messageServer}**\n{message.Content}\n{attachmentUrls}");
                            }

                            //if (message.Attachments.Count > 0)
                            //{
                            //    foreach (Attachment a in attachments)
                            //    {
                            //        //attachmentUrls += $"\n{a.Url.ToString()}";
                            //        await client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync(a.Url);
                            //    }
                            //}
                        }
                    }
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
