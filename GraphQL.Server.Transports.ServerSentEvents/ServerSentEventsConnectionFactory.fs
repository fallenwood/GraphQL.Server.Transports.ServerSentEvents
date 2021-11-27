namespace GraphQL.Server.Transports.ServerSentEvents

open Microsoft.Extensions.Logging
open GraphQL.Server
open System.Collections.Generic
open GraphQL
open Microsoft.AspNetCore.Http
open GraphQL.Server.Transports.Subscriptions.Abstractions
open GraphQL.Types

type ServerSentEventsConnectionFactory<'T when 'T :> ISchema>(
  logger: ILogger<ServerSentEventsConnectionFactory<'T>>,
  loggerFactory: ILoggerFactory,
  executer: IGraphQLExecuter<'T>,
  messageListeners: IEnumerable<IOperationMessageListener>,
  documentWriter: IDocumentWriter) =
  member val logger = logger
  member val loggerFactory = loggerFactory
  member val executer = executer
  member val messageListeners = messageListeners
  member val documentWriter = documentWriter

  interface IServerSentEventsConnectionFactory<'T> with
    member this.createConnection(context: HttpContext, token: string, connectionId: string) = 
      let transport = ServerSentEventsTransport(context, this.documentWriter, token, this.loggerFactory)
      let manager = SubscriptionManager(this.executer, this.loggerFactory)
      let server = SubscriptionServer(
        transport,
        manager,
        this.messageListeners,
        this.loggerFactory.CreateLogger<SubscriptionServer>())
      ServerSentEventsConnection(transport, server)