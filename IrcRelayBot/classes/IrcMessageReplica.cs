using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IrcRelayBot;

namespace IrcRelayBot.classes
{
    public class IrcMessageReplica : IrcMessageBase
    {
        public override ulong MessageId { get; set; }
        public override ulong ChannelId { get; set; }
        public override ulong ServerId { get; set; }
        public override DateTime MessageTime { get; set; }

        public ulong parentMessageId;
        public bool isWebhook { get; set; } = false;

        public IrcMessageReplica(ulong messageId, ulong channelId, ulong serverId, ulong parentMessageId)
        {
            this.MessageId = messageId;
            this.ChannelId = channelId;
            this.ServerId = serverId;
            this.parentMessageId = parentMessageId;
        }

        public IrcMessageReplica() { }

        public override async Task DeleteMessage()
        {
            // do nothing
            return;
        }

        public override async Task EditMessage(SocketMessage updatedMessage)
        {
            // do nothing
            return;
        }

        public override async Task PropogateReactions(ulong messageId, SocketReaction reaction)
        {
            if (Program.ircMessages.TryGetValue(parentMessageId, out IrcMessageBase parentMessage))
            {
                await parentMessage.PropogateReactions(messageId, reaction);
            }
        }
    }
}
