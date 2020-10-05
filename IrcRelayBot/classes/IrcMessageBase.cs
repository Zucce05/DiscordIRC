using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcRelayBot.classes
{
    public abstract class IrcMessageBase
    {
        abstract public ulong MessageId { get; set; }
        abstract public ulong ChannelId { get; set; }
        abstract public ulong ServerId { get; set; }
        abstract public DateTime MessageTime { get; set; }

        abstract public Task PropogateReactions(ulong messageId, SocketReaction reaction);
        abstract public Task DeleteMessage();
        abstract public Task EditMessage(SocketMessage updatedMessage);

    }
}
