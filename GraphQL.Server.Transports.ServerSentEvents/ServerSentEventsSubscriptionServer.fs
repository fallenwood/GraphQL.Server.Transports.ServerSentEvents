namespace GraphQL.Server.Transports.ServerSentEvents

open GraphQL.Server.Transports.Subscriptions.Abstractions
open Microsoft.AspNetCore.Http
open System.Collections.Generic
open Microsoft.Extensions.Logging
open System.Threading.Tasks.Dataflow
open GraphQL.Server.Transports.AspNetCore

type ServerSentEventsSubscriptionServer(
  httpContext: HttpContext,
  transport: IMessageTransport,
  subscriptionManager: ISubscriptionManager,
  messageListeners: IEnumerable<IOperationMessageListener>,
  logger: ILogger<ServerSentEventsSubscriptionServer>) =
  
  let onBeforeHandleAsync(context: MessageHandlingContext) =
      messageListeners
      |> Seq.map (fun listener -> listener.BeforeHandleAsync(context) |> Async.AwaitTask)
      |> Async.Sequential
      |> Async.Ignore

  let onHandleAsync(context: MessageHandlingContext) =
      messageListeners
      |> Seq.map (fun listener -> listener.HandleAsync(context) |> Async.AwaitTask)
      |> Async.Sequential
      |> Async.Ignore
  
  let onAfterHandleAsync(context: MessageHandlingContext) =
    messageListeners
    |> Seq.map (fun listener -> listener.AfterHandleAsync(context) |> Async.AwaitTask)
    |> Async.Sequential
    |> Async.Ignore

  interface IServerOperations with
    member _.Subscriptions: ISubscriptionManager = 
      subscriptionManager
    member _.Terminate(): System.Threading.Tasks.Task =
      task {
        let! _ = 
          subscriptionManager
          |> Seq.map (fun subscription -> subscriptionManager.UnsubscribeAsync(subscription.Id) |> Async.AwaitTask)
          |> Async.Sequential
        return transport.Reader.Complete()
      }
    member _.TransportReader: IReaderPipeline = 
      transport.Reader
    member _.TransportWriter: IWriterPipeline = 
      transport.Writer

  member this.OnConnect() =
    logger.LogDebug("Connected")
    let (handler: ActionBlock<OperationMessage>) = this.linkToTransportReader()

    async {
      let! _ = handler.Completion |> Async.AwaitTask
      let! _ = transport.Writer.Complete() |> Async.AwaitTask
      let! _ = transport.Writer.Completion |> Async.AwaitTask
      return ()
    }

  member this.OnDisconnect() = 
    logger.LogDebug("Disconnected")
    let me = this :> IServerOperations
    me.Terminate()
    |> Async.AwaitTask
  
  member this.linkToTransportReader() = 
    logger.LogDebug("Creating reader pipeline")
    let handler = ActionBlock<OperationMessage>(this.handleMessageAsync, ExecutionDataflowBlockOptions(EnsureOrdered = true, BoundedCapacity = 1))
    transport.Reader.LinkTo(handler)
    logger.LogDebug("Reader pipeline created")
    handler

  member this.buildMessageHandlingContext(message: OperationMessage) =
    let context = new MessageHandlingContext(this, message)
    async {
      let! userContext = async {
        match httpContext.RequestServices.GetService(typedefof<IUserContextBuilder>) with
          | :? IUserContextBuilder as builder -> 
            let! userContext = builder.BuildUserContext(httpContext) |> Async.AwaitTask
            return userContext
          | _ -> return Dictionary<string, obj>()
      }

      // TODO: functional?
      for e in userContext do
        context.Properties.[e.Key] <- e.Value

      context.Properties.["httpContext"] <- httpContext
      logger.LogInformation("Built User Context {}", context.Count)
      return context
    }
  
  member this.handleMessageAsync(message: OperationMessage) =
    task {
      logger.LogDebug("Handling Operation Message")
      let! context = this.buildMessageHandlingContext(message)
      let! _ = onBeforeHandleAsync(context)

      match context.Terminated with
      | true -> ()
      | false ->
        let! _ = onHandleAsync(context)
        let! _ = onAfterHandleAsync(context)
        ()
    }