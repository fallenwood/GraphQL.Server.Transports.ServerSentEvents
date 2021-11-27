using System;

namespace GraphQL.Server.Transports.ServerSentEvents.Samples
{
    public class ReceivedMessage
    {
        public string FromId { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
