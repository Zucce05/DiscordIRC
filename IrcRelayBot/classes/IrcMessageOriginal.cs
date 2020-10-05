using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IrcRelayBot.classes
{
    public class IrcMessageOriginal : IrcMessageBase
    {
        public override ulong MessageId { get; set; }
        public override ulong ChannelId { get; set; }
        public override ulong ServerId { get; set; }
        public List<ulong> replicaIds = new List<ulong>();
        public override DateTime MessageTime { get; set; }

        public IrcMessageOriginal(ulong messageId, ulong channelId, ulong serverId, List<ulong> replicaIds)
        {
            this.MessageId = messageId;
            this.ChannelId = channelId;
            this.ServerId = serverId;
            this.replicaIds = replicaIds;
        }

        public IrcMessageOriginal() { }

        public override async Task DeleteMessage()
        {
            foreach (ulong messageId in replicaIds)
            {
                if (Program.ircMessages.TryRemove(messageId, out IrcMessageBase outMessage))
                {
                    await Program.client.GetGuild(outMessage.ServerId).GetTextChannel(outMessage.ChannelId).DeleteMessageAsync(outMessage.MessageId);
                }
            }
        }

        public override async Task EditMessage(SocketMessage updatedMessage)
        {
            foreach (ulong messageId in replicaIds)
            {
                if (Program.ircMessages.TryGetValue(messageId, out IrcMessageBase outMessage))
                {
                        IrcMessageReplica message = (IrcMessageReplica)outMessage;
                        if (message.isWebhook)
                        {
                            if (Program.ircDictionary.TryGetValue(message.ChannelId, out IrcChannel outChannel))
                            {
                                using (DiscordWebhookClient discordWebhookClient = new DiscordWebhookClient(outChannel.WebhookString))
                                {
                                    string jsonMessage = string.Empty;

                                    try
                                    {
                                        await discordWebhookClient.SendMessageAsync($"Message edited: https://discordapp.com/channels/{message.ServerId}/{message.ChannelId}/{message.MessageId} \n{updatedMessage.Content}", false, null, $"{updatedMessage.Author.Username}", $"{updatedMessage.Author.GetAvatarUrl()}");
                                    }
                                    catch (Exception e)
                                    {
                                        await Program.Log(new LogMessage(LogSeverity.Error, "Send Webhook Message", e.Message));
                                        if (e.InnerException.Message.Contains("Could not find a webhook with the supplied credentials"))
                                        {
                                            outChannel.WebhookString = string.Empty;
                                            using (StreamWriter file = File.CreateText("json/IrcChannel.json"))
                                            {
                                                JsonSerializer serializer = new JsonSerializer();
                                                serializer.Serialize(file, Program.ircDictionary);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            IUserMessage gotMessage = (IUserMessage)(await Program.client.GetGuild(message.ServerId).GetTextChannel(message.ChannelId).GetMessageAsync(message.MessageId));
                            await gotMessage.ModifyAsync(x => x.Content = $"**__{updatedMessage.Author.Username}#{updatedMessage.Author.Discriminator}:__** {updatedMessage.Content}");
                        }
                }
            }
        }

        public override async Task PropogateReactions(ulong messageId, SocketReaction reaction)
        {
            foreach (ulong replicaMessageId in replicaIds)
            {
                if (Program.ircMessages.TryGetValue(replicaMessageId, out IrcMessageBase message) && replicaMessageId != messageId)
                {
                    IUserMessage gotMessage = (IUserMessage)await Program.client.GetGuild(message.ServerId).GetTextChannel(message.ChannelId).GetMessageAsync(message.MessageId);
                    await gotMessage.AddReactionAsync(reaction.Emote);
                }
            }
        }
    }
}
