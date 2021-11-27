using GraphQL.Types;

namespace GraphQL.Server.Transports.ServerSentEvents.Samples
{
    public class MessageFromType : ObjectGraphType<MessageFrom>
    {
        public MessageFromType()
        {
            Field(o => o.Id);
            Field(o => o.DisplayName);
        }
    }
}
