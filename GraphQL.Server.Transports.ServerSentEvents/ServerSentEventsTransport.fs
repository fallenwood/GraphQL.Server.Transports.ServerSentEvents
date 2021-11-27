namespace GraphQL.Server.Transports.ServerSentEvents

open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open GraphQL.Server.Transports.Subscriptions.Abstractions
open Microsoft.Extensions.Logging
open GraphQL
open Newtonsoft.Json.Serialization
open System.Threading.Tasks

        
type ServerSentEventsTransport(context: HttpContext, documentWriter: IDocumentWriter, token: string, loggerFactory: ILoggerFactory) =
  let serializerSettings = JsonSerializerSettings(
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'",
    ContractResolver = CamelCasePropertyNamesContractResolver())

  member val context = context
  member val reader = ServerSentEventsReaderPipeline(context, token, serializerSettings, loggerFactory.CreateLogger<ServerSentEventsReaderPipeline>()) 
  member val writer = ServerSentEventsWriterPipeline(context, documentWriter, token, loggerFactory.CreateLogger<ServerSentEventsWriterPipeline>())

  interface IMessageTransport with
    member this.Reader: IReaderPipeline = 
      this.reader
    member this.Writer: IWriterPipeline = 
      this.writer

  member _.CloseAsync() = 
    Task.CompletedTask