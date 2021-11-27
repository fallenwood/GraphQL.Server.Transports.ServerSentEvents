namespace GraphQL.Server.Transports.ServerSentEvents

open FSharp.Control
open GraphQL.NewtonsoftJson
open GraphQL.Server
open GraphQL.Server.Transports.Subscriptions.Abstractions
open GraphQL.Subscription
open Microsoft.Extensions.Logging
open System.Collections.Concurrent
open System.Linq
open System.Threading.Tasks

type ServerSentEventsSubscriptionManager(executer: IGraphQLExecuter, loggerFactory: ILoggerFactory) =
  
  // let logger = loggerFactory.CreateLogger<ServerSentEventsSubscriptionManager>()
  let subscriptions = ConcurrentDictionary<string, Subscription>()

  let execute(id: string, payload: OperationMessagePayload, context: MessageHandlingContext) =
    let writer = context.Writer

    async {
      let variable =
        match payload.Variables with
        | null -> null
        | str -> str.ToInputs()

      let! result = executer.ExecuteAsync(payload.OperationName, payload.Query, variable, context, null) |> Async.AwaitTask
      let hasError =
        match result.Errors with 
        | null -> false
        | errors -> errors.Any()

      return
        match hasError with 
        | true ->
          writer.SendAsync(OperationMessage(Type = MessageType.GQL_ERROR, Id = id, Payload = result))
          |> Async.AwaitTask
          |> ignore
          None
        | false -> 
          match result with
          | :? SubscriptionExecutionResult as subscriptionExecutionResult ->
              match subscriptionExecutionResult.Streams.Values.SingleOrDefault() with 
              | null ->
                writer.SendAsync(OperationMessage(Type = MessageType.GQL_ERROR, Id = id, Payload = result))
                |> Async.AwaitTask
                |> ignore
                None
              | _ ->
                let remove = fun _ -> subscriptions.TryRemove(id) |> ignore
                let subscription = Subscription(id, payload, subscriptionExecutionResult, writer, remove, loggerFactory.CreateLogger<Subscription>())
                Some subscription
          | _ ->
            writer.SendAsync(OperationMessage(Type = MessageType.GQL_DATA, Id = id, Payload = result))
            |> Async.AwaitTask
            |> ignore
            writer.SendAsync(OperationMessage(Type = MessageType.GQL_COMPLETE, Id = id))
            |> Async.AwaitTask
            |> ignore
            None }

  interface ISubscriptionManager with
    member _.GetEnumerator(): System.Collections.Generic.IEnumerator<Subscription> = 
      subscriptions.Values.GetEnumerator()
    member _.GetEnumerator(): System.Collections.IEnumerator = 
      subscriptions.Values.GetEnumerator()

    member _.SubscribeOrExecuteAsync(id: string, payload: OperationMessagePayload, context: MessageHandlingContext): Task = 
      async {
        let! subscription = execute(id, payload, context)
        return
          match subscription with 
          | None -> ()
          | Some sub -> 
            subscriptions.TryAdd(id, sub) 
            |> ignore }
      |> Async.StartAsTask 
      :> Task
      
    member _.UnsubscribeAsync(id: string): Task = 
      match subscriptions.TryRemove(id) with
      | (false, _) -> Task.CompletedTask
      | (true, removed) ->
        removed.UnsubscribeAsync()
        |> Async.AwaitTask
        |> Async.StartAsTask
        :> Task
