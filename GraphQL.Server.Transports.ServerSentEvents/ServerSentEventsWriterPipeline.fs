namespace GraphQL.Server.Transports.ServerSentEvents

open GraphQL
open Microsoft.Extensions.Logging
open GraphQL.Server.Transports.Subscriptions.Abstractions
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open Microsoft.AspNetCore.Http


type ServerSentEventsWriterPipeline(context: HttpContext, documentWriter: IDocumentWriter, token: string, logger: ILogger<ServerSentEventsWriterPipeline>) =

  let writeMessageAsync(message: OperationMessage) = 
    match context.RequestAborted.IsCancellationRequested with
    | true -> Task.CompletedTask
    | false ->
      let stream = context.Response.Body
      let result =  
        match token with
        | null ->
          match message.Type with
          | MessageType.GQL_DATA ->
            async {
              let! _ = context.Response.WriteAsync("event: next\ndata: ") |> Async.AwaitTask
              let! _ = documentWriter.WriteAsync(stream, message.Payload) |> Async.AwaitTask
              context.Response.WriteAsync("\n\n") |> Async.AwaitTask |> ignore
            }
            |> Async.StartAsTask
            :> Task
          | MessageType.GQL_COMPLETE -> 
            context.Response.WriteAsync("event: complete\n\n")
            |> Async.AwaitTask
            |> Async.StartAsTask
            :> Task
          | _ -> Task.CompletedTask
        | _ ->
            match message.Type with
            | MessageType.GQL_DATA ->
              async {
                let! _ = context.Response.WriteAsync("event: next\ndata: ") |> Async.AwaitTask
                let! _ =  documentWriter.WriteAsync(stream, message.Payload) |> Async.AwaitTask
                context.Response.WriteAsync("\n\n") |> Async.AwaitTask |> ignore
              }
              |> Async.StartAsTask
              :> Task
            | MessageType.GQL_COMPLETE ->
              async {
                let! _ = context.Response.WriteAsync("event: complete") |> Async.AwaitTask
                let! _ = documentWriter.WriteAsync(stream, OperationMessage(Id = message.Id)) |> Async.AwaitTask
                context.Response.WriteAsync("\n\n") |> Async.AwaitTask |> ignore
              }
              |> Async.StartAsTask
              :> Task
            | _ -> Task.CompletedTask
      result

  let createMessageWriter() =
    ActionBlock<OperationMessage>(writeMessageAsync, ExecutionDataflowBlockOptions(MaxDegreeOfParallelism = 1, EnsureOrdered = true))
  
  member _.startBlock = createMessageWriter()

  interface IWriterPipeline with
    member this.Complete(): Task = 
      this.startBlock.Complete()
      Task.CompletedTask
    member this.Completion: Task = 
      this.startBlock.Completion
    member this.Post(message: OperationMessage): bool = 
        this.startBlock.Post(message)
    member this.SendAsync(message: OperationMessage): Task = 
        this.startBlock.SendAsync(message)
 