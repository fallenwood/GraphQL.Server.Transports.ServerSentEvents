
namespace GraphQL.Server.Transports.ServerSentEvents

type ServerSentEventsConnection(transport: ServerSentEventsTransport, subscriptionServer: ServerSentEventsSubscriptionServer) =
  member val transport = transport
  member val server = subscriptionServer

  member this.Connect() =
    async {
      let! _ =  this.server.OnConnect() |> Async.Ignore
      let! _ = this.server.OnDisconnect() |> Async.Ignore
      this.transport.CloseAsync() |> Async.AwaitTask |> ignore
    }