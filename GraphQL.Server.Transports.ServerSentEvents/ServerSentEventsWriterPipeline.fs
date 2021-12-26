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
      match token with
      | null ->
        match message.Type with
        | MessageType.GQL_DATA ->
          task {
            let! _ = context.Response.WriteAsync("event: next\ndata: ") |> Async.AwaitTask
            let! _ = documentWriter.WriteAsync(stream, message.Payload) |> Async.AwaitTask
            let! _ = context.Response.WriteAsync("\n\n") |> Async.AwaitTask
            return ()
          }
        | MessageType.GQL_COMPLETE -> 
          context.Response.WriteAsync("event: complete\n\n")
        | _ -> Task.CompletedTask
      | _ ->
          match message.Type with
          | MessageType.GQL_DATA ->
            task {
              let! _ = context.Response.WriteAsync("event: next\ndata: ") |> Async.AwaitTask
              let! _ =  documentWriter.WriteAsync(stream, {| Id = message.Id; Payload = message.Payload |}) |> Async.AwaitTask
              let! _ = context.Response.WriteAsync("\n\n") |> Async.AwaitTask
              return ()
            }
          | MessageType.GQL_COMPLETE ->
            task {
              let! _ = context.Response.WriteAsync("event: complete") |> Async.AwaitTask
              let! _ = documentWriter.WriteAsync(stream, OperationMessage(Id = message.Id)) |> Async.AwaitTask
              let! _ = context.Response.WriteAsync("\n\n") |> Async.AwaitTask
              return ()
            }
          | _ -> Task.CompletedTask

  let createMessageWriter() =
    ActionBlock<OperationMessage>(writeMessageAsync, ExecutionDataflowBlockOptions(MaxDegreeOfParallelism = 1, EnsureOrdered = true))
  
  let startBlock = createMessageWriter()

  interface IWriterPipeline with
    member _.Complete(): Task = 
      startBlock.Complete()
      Task.CompletedTask
    member _.Completion: Task = 
      startBlock.Completion
    member _.Post(message: OperationMessage): bool = 
      startBlock.Post(message)
    member _.SendAsync(message: OperationMessage): Task = 
      startBlock.SendAsync(message)
 