using System;
using GraphQL.Types;

namespace GraphQL.Server.Transports.ServerSentEvents.Samples
{
    public class ChatSchema : Schema
    {
        public ChatSchema(IChat chat, IServiceProvider provider) : base(provider)
        {
            Query = new ChatQuery(chat);
            Mutation = new ChatMutation(chat);
            Subscription = new ChatSubscriptions(chat);
        }
    }
}
