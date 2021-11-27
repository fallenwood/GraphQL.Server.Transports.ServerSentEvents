using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.ServerSentEvents.Extensions;

public static class GraphQLServerSentEventsApplicationBuilderExtensions
{
    public static IApplicationBuilder UseGraphQLServerSentEvents<TSchema>(
        this IApplicationBuilder builder,
        string path = "/graphql")
        where TSchema : ISchema
        => builder.UseGraphQLServerSentEvents<TSchema>(new PathString(path));

    public static IApplicationBuilder UseGraphQLServerSentEvents<TSchema>(
        this IApplicationBuilder builder,
        PathString path)
        where TSchema : ISchema
    {
        return builder.UseWhen(
            context => context.Request.Path.StartsWithSegments(path, out var remaining) && string.IsNullOrEmpty(remaining),
            b => b.UseMiddleware<GraphQLServerSentEventsMiddleware<TSchema>>());
    }
}
