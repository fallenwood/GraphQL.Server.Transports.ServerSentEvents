GraphQL.Server.Transports.ServerSentEvents
---

## Introduction
Experimental transport implmentation for [GraphQL dotnet server](https://github.com/graphql-dotnet/server "") 5.0.2+

## How to run
1. Make sure dotnet 6 and nodejs are installed
2. Inside `GraphQL.Server.Transports.ServerSentEvents.Samples/ClientApp`, run `npm install`, `npm run build`
3. Inside `GraphQL.Server.Transports.ServerSentEvents.Samples`, run `dotnet run`
4. Visit http://loccalhost:5000/index.html, click `subscribe` button
5. With some http client like `postman`, send a GraphQL mutation like
```
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

## TODOs
 - [x] Fix single connection
 - [ ] Unit tests
 - [ ] Github actions
 - [ ] Test with more cases
 - [ ] Beautify example

## License
MIT