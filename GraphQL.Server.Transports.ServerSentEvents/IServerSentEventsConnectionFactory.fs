namespace GraphQL.Server.Transports.ServerSentEvents

open Microsoft.AspNetCore.Http

type IServerSentEventsConnectionFactory<'T> =
  abstract member createConnection: context: HttpContext * token: string * connectionId: string -> ServerSentEventsConnection
