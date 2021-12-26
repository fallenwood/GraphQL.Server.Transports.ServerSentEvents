namespace GraphQL.Server.Transports.ServerSentEvents

open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open GraphQL.Server.Transports.Subscriptions.Abstractions
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open System.IO
open System.Text
open FSharp.Control
open Newtonsoft.Json.Linq
open Microsoft.Extensions.Logging

type ServerSentEventsReaderPipeline(
  context: HttpContext,
  token: string,
  serializerSettings: JsonSerializerSettings,
  logger: ILogger<ServerSentEventsReaderPipeline>) =
  
  let readMessageFromContextAsync(target: ITargetBlock<ServerSentEventsOperationMessage>, message: ServerSentEventsOperationMessage) =
      target.SendAsync(message)
      |> Async.AwaitIAsyncResult
      |> Async.Ignore
  
  let readMessage(target: ITargetBlock<ServerSentEventsOperationMessage>) =
    logger.LogTrace("Will read message from target")
    let sseContext =
      match token with
      | null ->
        logger.LogTrace("Token is null, cannot find sse context")
        None
      | token -> 
        match ServerSentEventsContext.Collection.TryRemove token with 
        | (false, _) ->
          logger.LogTrace("Sse context not found")
          None
        | (true, context) ->
          logger.LogTrace("SSe context Found")
          Some context

    match sseContext with
    | None ->
      logger.LogTrace("Sse Context Not found, reading from http context")
      use memoryStream = new MemoryStream()
      context.Request.Body.CopyToAsync(memoryStream)
      |> Async.AwaitTask
      |> ignore
      let message = Encoding.UTF8.GetString(memoryStream.ToArray())
      logger.LogTrace("Read Message {}", message)
      readMessageFromContextAsync(target, ServerSentEventsOperationMessage(id=null, type_=MessageType.GQL_START, payload=message))
      |> ignore
    | Some sseContext -> 
      logger.LogTrace("Sse Context found, recursiving")
      // Why currying not work?
      // let action = readMessageFromContextAsync target
      let action = fun e -> readMessageFromContextAsync(target, e)
      let rec f() = 
        async {
          logger.LogTrace("will call getReceiver {}", context.Connection.Id)
          let readerContexts = sseContext.getAllReceivers(context.RequestAborted)

          let! _ =
            AsyncSeq.ofAsyncEnum(readerContexts)
            |> AsyncSeq.iterAsync action
          f()
        }
        |> Async.Start
      f()

  let createMessageReader() = 
    let source = BufferBlock<ServerSentEventsOperationMessage>(
      ExecutionDataflowBlockOptions(EnsureOrdered=true, BoundedCapacity=1, MaxDegreeOfParallelism=1))
    
    Task.Run(fun () -> readMessage(source) |> ignore)
    |> ignore
  
    source :> ISourceBlock<ServerSentEventsOperationMessage>

  let createReaderJsonTransformer() = 
    let transfomer(input: ServerSentEventsOperationMessage) : OperationMessage =
      logger.LogTrace("Transfoming")
      let payload = JObject.FromObject(JsonConvert.DeserializeObject(input.Payload, serializerSettings))
      let operationIdOrNull =
        match payload.TryGetValue("extensions") with
        | (true, extensions) ->
          match extensions with
          | :? JObject as extensionsObject -> 
            match extensionsObject.TryGetValue("operationId") with
            | (false, _) ->
              logger.LogTrace("OperationId not found in extensions")
              null
            | (true, operationIdToken) -> operationIdToken.Value<string>()
          | _ -> null
        | (false, _) -> null
      let operationId = 
        match operationIdOrNull with
        | null ->
          // TODO: ?? operator?
          match input.Id with
          | null -> context.Connection.Id
          | token -> token
        | token -> token
      OperationMessage(Type=MessageType.GQL_START, Payload=payload, Id=operationId)
    TransformBlock<ServerSentEventsOperationMessage, OperationMessage>(transfomer, ExecutionDataflowBlockOptions(EnsureOrdered=true))

  let _startBlock = createMessageReader()
  let _endBlock = createReaderJsonTransformer()

  do
    _startBlock.LinkTo(_endBlock, DataflowLinkOptions(PropagateCompletion=true)) |> ignore
    
  member _.startBlock = _startBlock
  member _.endBlock = _endBlock

  interface IReaderPipeline with
    member this.Complete(): Task = 
      this.startBlock.Complete()
      Task.CompletedTask

    member this.Completion: Task = 
      this.endBlock.Completion

    member this.LinkTo(target: ITargetBlock<OperationMessage>): unit =
      logger.LogTrace("Linking Reader endBlock")
      this.endBlock.LinkTo(target, DataflowLinkOptions(PropagateCompletion=true))
      |> ignore
