using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.ServerSentEvents;
public static class IGraphQLBuilderExtensions
{
    public static IGraphQLBuilder AddServerSentEvents(this IGraphQLBuilder builder)
    {
        builder
            .Services
            .AddTransient(typeof(IServerSentEventsConnectionFactory<>), typeof(ServerSentEventsConnectionFactory<>))
            .AddTransient<IOperationMessageListener, LogMessagesListener>()
            .AddTransient<IOperationMessageListener, ProtocolMessageListener>();
        return builder;
    }
}