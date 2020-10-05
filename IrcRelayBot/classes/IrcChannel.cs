using System;
using System.Collections.Generic;
using System.Text;

namespace IrcRelayBot.classes
{
    public class IrcChannel
    {
        public ulong GuildID { get; set; }
        public string WebhookString { get; set; } = string.Empty;
        public string Topic { get; set; }
        public int FailedMessageCounter { get; set; } = 0;
    }
}
