namespace GraphQL.Server.Transports.ServerSentEvents

open Microsoft.AspNetCore.Http
open System.Collections.Concurrent
open System.Threading.Channels
open System.Threading

type ServerSentEventsContext(sender: HttpContext) =
  static let collection = ConcurrentDictionary<string, ServerSentEventsContext>()
  static member Collection = collection

  member val sender = sender
  member val receivedMessages = Channel.CreateUnbounded<ServerSentEventsOperationMessage>()

  member this.addReceivedMessage(message: ServerSentEventsOperationMessage) =
    this.receivedMessages.Writer.WriteAsync(message)

  member this.getAllReceivers(cancellationToken: CancellationToken) =
    this.receivedMessages.Reader.ReadAllAsync(cancellationToken)
