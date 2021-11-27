
namespace GraphQL.Server.Transports.ServerSentEvents

open GraphQL.Server.Transports.Subscriptions.Abstractions

type ServerSentEventsConnection(transport: ServerSentEventsTransport, subscriptionServer: SubscriptionServer) =
  member val transport = transport
  member val server = subscriptionServer

  member this.Connect() =
    async {
      let! _ =  this.server.OnConnect() |> Async.AwaitTask
      let! _ = this.server.OnDisconnect() |> Async.AwaitTask
      this.transport.CloseAsync() |> Async.AwaitTask |> ignore
    }
