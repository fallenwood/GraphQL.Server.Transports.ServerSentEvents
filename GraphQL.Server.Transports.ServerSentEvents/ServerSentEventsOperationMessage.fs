namespace GraphQL.Server.Transports.ServerSentEvents

open System.Runtime.Serialization

[<DataContract>]
type ServerSentEventsOperationMessage(id: string, type_: string, payload: string) =
  [<DataMember(Name = "id")>]
  member val Id = id with get, set
  [<DataMember(Name = "type")>]
  member val Type = type_ with get, set
  [<DataMember(Name = "payload")>]
  member val Payload = payload with get, set
