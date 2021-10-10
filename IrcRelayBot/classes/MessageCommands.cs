using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IrcRelayBot.classes
{
    public static class MessageCommands
    {
        public static async Task<Task> MessageReceived(SocketMessage message, ConcurrentDictionary<ulong, IrcChannel> ircDictionary, ConcurrentDictionary<ulong, IrcMessageBase> ircMessages)
        {
            bool isBotCommand = false;
            string[] commandString = message.Content.ToLower().Split(" ");

            if (message.Content.StartsWith("+irc"))
            {
                isBotCommand = true;

                if(message.Author.Id == 233826304699531264)
                {
                    // Add bot administration commands (remove topic, block server, etc)
                }
                if(Program.client.GetGuild(((IGuildChannel)message.Channel).GuildId).GetUser(message.Author.Id).GuildPermissions.Administrator)
                {
                    // Add admin management (set channel, remove channel, set webhook, remove webhook, etc.)
                    switch (commandString[1])
                    {
                        case "link":
                            switch (commandString[2])
                            {
                                case "join":
                                    string[] linkString = message.Content.Split("~");
                                    isBotCommand = true;
                                    if (!ircDictionary.ContainsKey(message.Channel.Id))
                                    {
                                        // string msg = message.Content.Substring(1).ToLower();
                                        // string[] substring = msg.Split(" ", 2);

                                        IrcChannel newChannel = new IrcChannel();
                                        newChannel.Topic = linkString[1].ToLower();
                                        newChannel.GuildID = ((IGuildChannel)message.Channel).GuildId;
                                        ircDictionary.TryAdd(message.Channel.Id, newChannel);
                                        using (StreamWriter file = File.CreateText("json/IrcChannel.json"))
                                        {
                                            JsonSerializer serializer = new JsonSerializer();
                                            serializer.Serialize(file, ircDictionary);
                                        }
                                        await message.Channel.SendMessageAsync($"Channel now a linked to Discord IRC using the topic: \"**{linkString[1]}**\".");
                                    }
                                    else
                                    {
                                        await message.Channel.SendMessageAsync($"This channel is already linked to a Discord IRC global chat.\nUse ``+removeirc`` to remove and reassign this channel.");
                                    }
                                    break;
                                case "leave":
                                    {
                                        isBotCommand = true;
                                        string msg = message.Content.Substring(1).ToLower();
                                        string[] substring = msg.Split(" ", 2);

                                        ircDictionary.TryRemove(message.Channel.Id, out IrcChannel value);
                                        await message.Channel.SendMessageAsync($"Channel is no longer a Discord IRC linked channel.");
                                    }
                                    using (StreamWriter file = File.CreateText("json/IrcChannel.json"))
                                    {
                                        JsonSerializer serializer = new JsonSerializer();
                                        serializer.Serialize(file, ircDictionary);
                                    }
                                    // await message.Channel.SendMessageAsync("link leave success");
                                    break;
                            }
                            break;
                        case "webhook":
                            switch (commandString[2])
                            {
                                case "add":
                                    isBotCommand = true;
                                    string[] linkString = message.Content.Split("~");
                                    if (ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel addValue))
                                    {
                                        // string[] substring = message.Content.Split(" ", 2);
                                        addValue.WebhookString = linkString[1];
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
                                    // await message.Channel.SendMessageAsync("websocket add success");
                                    break;
                                case "remove":
                                    isBotCommand = true;
                                    if (ircDictionary.TryGetValue(message.Channel.Id, out IrcChannel removeValue))
                                    {
                                        removeValue.WebhookString = string.Empty;
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
                                    // await message.Channel.SendMessageAsync("websocket remove success");
                                    break;
                            }
                            break;
                        case "broadcast":
                            switch (commandString[2])
                            {
                                case "create":
                                    await message.Channel.SendMessageAsync("broadcast create success");
                                    break;
                                case "destroy":
                                    await message.Channel.SendMessageAsync("broadcast destroy success");
                                    break;
                            }
                            break;
                        case "subscribe":
                            switch (commandString[2])
                            {
                                case "join":
                                    await message.Channel.SendMessageAsync("subscribe join success");
                                    break;
                                case "leave":
                                    await message.Channel.SendMessageAsync("subscribe leave success");
                                    break;
                            }
                            break;
                        case "user":
                            break;
                        case "server":
                            break;
                    }

                }
            }

            // Condition, check for server owner key comamnds for bot configuration
            if (message.Author.Id == ((SocketTextChannel)message.Channel).Guild.OwnerId)
            {

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
                await message.Channel.SendMessageAsync($"You can invite the bot by using this link: <https://discordapp.com/api/oauth2/authorize?client_id=581557457864884225&permissions=116736&scope=bot>");
            }
            if (message.Content.StartsWith("+help"))
            {
                isBotCommand = true;
                string helpMessage = $"```\nEveryone:\n\t+invite: Provides an invite link to invite the bot to your server.\n\t+help: Displays this help text";
                helpMessage += $"\n\t+topic: Displays the topic (keyword) for the irc linked channel";
                helpMessage += $"\n\t+linked: Displays a list of all linked servers";
                helpMessage += $"\n\t+topics: Displays a list of all topics, and how many servers are joined to each";

                helpMessage += $"\nOwner/Admin only:";
                helpMessage += $"\n\t+addirc: Adds a channel to an irc topic\n\t\tUsage: +addirc <topic>";
                helpMessage += "\n\t+removeirc: Removes the channel from the irc topic it's a part of";
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
                            sendMessage += $"{Program.client.GetGuild(entry.Value.GuildID).Name}\n";
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
                    if (topics.ContainsKey(entry.Value.Topic))
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

            bool oldEnough = false;
            if (((SocketTextChannel)message.Channel).Guild.GetUser(message.Author.Id).JoinedAt.Value.UtcDateTime < DateTime.UtcNow.AddDays(-2))
            {
                oldEnough = true;
            }




            // Condition, only pay attention if the channel is used by the bot
            if (!message.Author.IsBot && ircDictionary.ContainsKey(message.Channel.Id) && !isBotCommand && oldEnough)
            {
                IrcMessageOriginal originalMessage = new IrcMessageOriginal
                {
                    MessageId = message.Id,
                    ChannelId = message.Channel.Id,
                    ServerId = ((IGuildChannel)message.Channel).GuildId,
                    MessageTime = message.Timestamp.UtcDateTime
                };

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

                foreach (KeyValuePair<ulong, IrcChannel> entry in ircDictionary)
                {
                    if (entry.Value.Topic == channelTopic)
                    {
                        bool webhookWorked = false;
                        if (message.Channel.Id != entry.Key)
                        {
                            try
                            {
                                if (entry.Value.WebhookString != string.Empty)
                                {
                                    using (DiscordWebhookClient discordWebhookClient = new DiscordWebhookClient(entry.Value.WebhookString))
                                    {
                                        string jsonMessage = string.Empty;

                                        try
                                        {
                                            ulong sentMessageId = await discordWebhookClient.SendMessageAsync($"{message.Content + "\n" + attachmentUrls}", false, null, $"{message.Author.Username}", $"{message.Author.GetAvatarUrl()}");
                                            webhookWorked = true;
                                            originalMessage.replicaIds.Add(sentMessageId);
                                            IrcMessageReplica replicatedMessage = new IrcMessageReplica()
                                            {
                                                MessageId = sentMessageId,
                                                ChannelId = entry.Key,
                                                ServerId = entry.Value.GuildID,
                                                parentMessageId = message.Id,
                                                MessageTime = DateTime.UtcNow,
                                                isWebhook = true
                                            };
                                            ircMessages.TryAdd(sentMessageId, replicatedMessage);

                                        }
                                        catch (Exception e)
                                        {
                                            await Program.Log(new LogMessage(LogSeverity.Error, "Send Webhook Message", e.Message));
                                            if (e.InnerException.Message.Contains("Could not find a webhook with the supplied credentials"))
                                            {
                                                entry.Value.WebhookString = string.Empty;
                                                using (StreamWriter file = File.CreateText("json/IrcChannel.json"))
                                                {
                                                    JsonSerializer serializer = new JsonSerializer();
                                                    serializer.Serialize(file, ircDictionary);
                                                }
                                                await Program.client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync($"Webhook credentials invalid - Try adding the webhook again, or creating a new one to add.");
                                            }
                                            //await Program.client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync($"**User: {message.Author.Username}\nServer: {messageServer}**\n{message.Content}\n{attachmentUrls}");
                                        }

                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                await Program.Log(new LogMessage(LogSeverity.Error, "Send Webhook Message", e.Message));
                                
                            }
                            
                            if (!webhookWorked)
                            {
                                RestUserMessage sentMessage = await Program.client.GetGuild(entry.Value.GuildID).GetTextChannel((entry.Key)).SendMessageAsync($"**__{message.Author.Username}#{message.Author.Discriminator}:__** {message.Content}\n{attachmentUrls}");
                                originalMessage.replicaIds.Add(sentMessage.Id);
                                IrcMessageReplica replicatedMessage = new IrcMessageReplica()
                                {
                                    MessageId = sentMessage.Id,
                                    ChannelId = entry.Key,
                                    ServerId = entry.Value.GuildID,
                                    parentMessageId = message.Id,
                                    MessageTime = sentMessage.Timestamp.UtcDateTime
                                };
                                ircMessages.TryAdd(sentMessage.Id, replicatedMessage);
                            }


                        }
                    }
                }
                ircMessages.TryAdd(message.Id, originalMessage);
                using (StreamWriter file = File.CreateText("json/IrcMessage.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, ircMessages);
                }
            }

            return Task.CompletedTask;
        }
    }
}
