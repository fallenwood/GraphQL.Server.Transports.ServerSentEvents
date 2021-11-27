using GraphQL;
using GraphQL.Server;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.ServerSentEvents;
using GraphQL.Server.Transports.ServerSentEvents.Extensions;
using GraphQL.Server.Transports.ServerSentEvents.Samples;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services
    .AddRouting()
    .AddSingleton<IChat, Chat>()
    .AddSingleton<ChatSchema>()
    .AddSingleton<IDocumentExecuter, SubscriptionDocumentExecuter>()
    .AddGraphQL((options, provider) =>
    {
        options.EnableMetrics = true;
        var logger = provider.GetRequiredService<ILogger<Program>>();
        options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occurred", ctx.OriginalException.Message);
    })
    .AddDefaultEndpointSelectorPolicy()
    .AddSystemTextJson(deserializerSettings => { }, serializerSettings => { })
    .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
    .AddServerSentEvents()
    .AddDataLoader()
    .AddGraphTypes(typeof(ChatSchema));

builder.Services.AddControllers();

var app = builder.Build();

var s = app.Services.GetRequiredService<GraphQL.Server.IGraphQLExecuter<ChatSchema>>();

app.UseAuthorization();

app.UseStaticFiles();

app.UseGraphQLServerSentEvents<ChatSchema>();
app.UseGraphQL<ChatSchema, GraphQLHttpMiddleware<ChatSchema>>();

app.MapControllers();

app.Run();
