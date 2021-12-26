namespace GraphQL.Server.Transports.ServerSentEvents

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System
open System.Net
open System.IO
open System.Linq
open System.Text
open GraphQL.Server.Transports.Subscriptions
open Microsoft.Extensions.DependencyInjection
open GraphQL.Types
open System.Threading.Tasks

type GraphQLServerSentEventsMiddleware<'T when 'T :> ISchema>(next: RequestDelegate, logger: ILogger<GraphQLServerSentEventsMiddleware<'T>>) =
  let GraphQLEventStreamTokenHeader = "x-graphql-event-stream-token"
  let EventStreamContentType = "text/event-stream"

  member _.HandleSenderRequest(context: HttpContext, serverSentEventsContext: ServerSentEventsContext, type_: string, operationId: string) =
    logger.LogTrace("Handling Sender Request {}", context.Request.PathBase)
    async {
      use memoryStream = new MemoryStream()
      let! _ = context.Request.Body.CopyToAsync(memoryStream) |> Async.AwaitTask
      let message = Encoding.UTF8.GetString(memoryStream.ToArray())
      
      // TODO: Async.AwaitValueTask is not ready - https://github.com/fsharp/fslang-design/blob/main/RFCs/FS-1021-value-task-interop.md
      let! _ = serverSentEventsContext.addReceivedMessage(ServerSentEventsOperationMessage(operationId, type_, message)).AsTask() |> Async.AwaitTask
      context.Response.StatusCode <- (int)HttpStatusCode.Accepted
     }

  member _.HandleReceiverRequest(context: HttpContext, token: string) = 
    logger.LogTrace("Handling Receiver Request {}", context.Request.PathBase)
    async {
      context.Response.ContentType <- EventStreamContentType
      let! _ = context.Response.Body.FlushAsync() |> Async.AwaitTask
      let connectionFactory = context.RequestServices.GetRequiredService<IServerSentEventsConnectionFactory<'T>>()
      let connection = connectionFactory.createConnection(context, token, context.Connection.Id)
      let! _ = connection.Connect()
      context.RequestAborted.WaitHandle.WaitOne() |> ignore
    }
  
  member this.InvokeAsync(context: HttpContext) : Task =
    let request = context.Request
    let response = context.Response

    let invoke() =     
      logger.LogTrace("Invoking next {}", context.Request.PathBase)
      next.Invoke(context) |> Async.AwaitTask

    let accepts =
      match request.Headers.TryGetValue("Accept") with
      | (false, _) -> Array.Empty<string>()
      | (true, _1) -> _1.ToArray()

    let created() = 
      logger.LogTrace("Handling Created {}", context.Request.PathBase)
      async {
        let token = Guid.NewGuid().ToString()
        response.StatusCode <- (int)HttpStatusCode.Created
        let! _ = response.WriteAsync(token) |> Async.AwaitTask
        response.Body.FlushAsync() |> Async.AwaitTask |> ignore
      }

    let handleGet(token) = 
      logger.LogTrace("Handling Get {} {}", context.Request.PathBase, token)
      async {
        let sseContext = ServerSentEventsContext(context)
        ServerSentEventsContext.Collection.TryAdd(token, sseContext) |> ignore
        let! _ = this.HandleReceiverRequest(context, token)
        ServerSentEventsContext.Collection.TryRemove(token) |> ignore
      }
    
    let task =
      match request.Headers.TryGetValue(GraphQLEventStreamTokenHeader) with
      | (false, _) ->
        match HttpMethods.IsPut(context.Request.Method) with
        | true->
          created()
        | _ -> 
          match accepts.Contains(EventStreamContentType) with
          | false -> invoke()
          | true -> this.HandleReceiverRequest(context, null)
      | (true, tokens) ->
        let token = tokens.FirstOrDefault()
        match String.IsNullOrEmpty(token) with
        | true -> invoke()
        | false ->
          if HttpMethods.IsGet(request.Method) then
            match accepts.Contains(EventStreamContentType) with
            | false -> invoke()
            | true -> handleGet(token)
          else if HttpMethods.IsPost(request.Method) then
            match ServerSentEventsContext.Collection.TryGetValue(token) with
            | (false, _) -> invoke()
            | (true, serverSentEventsContext) -> this.HandleSenderRequest(context, serverSentEventsContext, Abstractions.MessageType.GQL_START, null)
          else if HttpMethods.IsDelete(request.Method) then
            match ServerSentEventsContext.Collection.TryGetValue(token) with
            | (false, _) -> invoke()
            | (true, serverSentEventsContext) ->
              match request.Query.TryGetValue("operationId") with
              | (false, _) -> invoke()
              | (true, operationIds) -> this.HandleSenderRequest(context, serverSentEventsContext, Abstractions.MessageType.GQL_STOP, operationIds.FirstOrDefault())
          else
            invoke()
    Async.StartAsTask task
