GraphQL.Server.Transports.ServerSentEvents
---

## Introduction
Experimental transport implmentation for [GraphQL dotnet server](https://github.com/graphql-dotnet/server "") 5.0.2+, with [graphql-sse](https://github.com/enisdenjo/graphql-sse "") as client.

## How to run example
1. Make sure dotnet 6 and nodejs are installed
2. Inside `GraphQL.Server.Transports.ServerSentEvents.Samples/ClientApp`, run `npm install`, `npm run build`
3. Inside `GraphQL.Server.Transports.ServerSentEvents.Samples`, run `dotnet run`
4. Visit http://loccalhost:5000/index.html, click `subscribe` button
5. With some http client like `postman`, send a GraphQL mutation like
```graphql
// Query
mutation AddMessage($message: MessageInputType!) {
  addMessage(message: $message) {
    from {
      id
      displayName
    }
    content
  }
}

// Variables
{
  "message": {
    "content": "Message",
    "fromId": "1"
  }
}
```

*Note: The subscription query is*
```graphql
subscription MessageAdded {
  messageAdded {
    from { id displayName }
    content
  }
}
```

## TODOs
 - [x] Fix single connection
 - [ ] Unit tests
 - [x] Github actions
 - [ ] Test with more cases
 - [ ] Beautify example
 - [ ] Publish to Nuget

## License
MIT