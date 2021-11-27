namespace GraphQL.Server.Transports.ServerSentEvents

open System.Runtime.CompilerServices
open GraphQL.Server
open Microsoft.Extensions.DependencyInjection
open GraphQL.Server.Transports.Subscriptions.Abstractions
open GraphQL.Types
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open System

[<Extension>]
module IGraphQLBuilderExtensions = 
  [<Extension>]
  let AddServerSentEvents(builder : IGraphQLBuilder) =
    let serverSentEventsConnectionFactoryInterfaceType = typedefof<IServerSentEventsConnectionFactory<_>>
    let serverSentEventsConnectionFactoryType = typedefof<ServerSentEventsConnectionFactory<_>>
    builder.Services.AddTransient(serverSentEventsConnectionFactoryInterfaceType, serverSentEventsConnectionFactoryType)
      .AddTransient<IOperationMessageListener, LogMessagesListener>()
      .AddTransient<IOperationMessageListener, ProtocolMessageListener>()
    |> ignore
    builder


[<Extension>]
module GraphQLServerSentEventsApplicationBuilderExtensions =
    
  [<Extension>]
  let UseGraphQLServerSentEvents<'TSchema  when 'TSchema :> ISchema>(builder: IApplicationBuilder, [<Optional; DefaultParameterValue("/graphql")>] path: PathString) =
    let predicate(context: HttpContext) = 
      let mutable remaining = PathString(String.Empty)
      match context.Request.Path.StartsWithSegments(path, &remaining) with
      | false-> false
      | true -> String.IsNullOrEmpty(remaining)
  
    builder.UseWhen(predicate, fun b -> b.UseMiddleware<GraphQLServerSentEventsMiddleware<'TSchema>>() |> ignore)
