using System.Text.Json;
using AdventureGame.Api.Services;
using AdventureGame.Shared;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// ChatGPT via Microsoft.Extensions.AI
var openAiKey = builder.Configuration["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

var openAiClient = new OpenAIClient(openAiKey);

builder.Services.AddChatClient(openAiClient
    .GetChatClient(builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini")
    .AsIChatClient());

// DALL-E image generation
builder.Services.AddSingleton(openAiClient.GetImageClient(
    builder.Configuration["OpenAI:ImageModel"] ?? "dall-e-2"));

builder.Services.AddSingleton<GameMasterService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

// --- Non-streaming endpoints (kept for backward compatibility) ---

app.MapPost("/api/game/start", async (StartGameRequest request, GameMasterService gm, CancellationToken ct) =>
{
    var response = await gm.StartGameAsync(request.Theme, ct);
    return Results.Ok(response);
});

app.MapPost("/api/game/action", async (ChooseActionRequest request, GameMasterService gm, CancellationToken ct) =>
{
    try
    {
        var response = await gm.ChooseActionAsync(request.GameId, request.ActionId, ct);
        return Results.Ok(response);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

// --- Streaming SSE endpoints ---

app.MapPost("/api/game/start/stream", async (StartGameRequest request, GameMasterService gm, HttpContext httpContext, CancellationToken ct) =>
{
    httpContext.Response.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";

    var (gameId, messages) = gm.PrepareStartGame(request.Theme);
    var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    await foreach (var streamEvent in gm.StreamSceneAsync(gameId, messages, ct))
    {
        var eventData = JsonSerializer.Serialize(streamEvent, jsonOptions);
        await httpContext.Response.WriteAsync($"event: {streamEvent.Type}\n", ct);
        await httpContext.Response.WriteAsync($"data: {eventData}\n\n", ct);
        await httpContext.Response.Body.FlushAsync(ct);
    }
});

app.MapPost("/api/game/action/stream", async (ChooseActionRequest request, GameMasterService gm, HttpContext httpContext, CancellationToken ct) =>
{
    httpContext.Response.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";

    List<ChatMessage> messages;
    try
    {
        messages = gm.PrepareAction(request.GameId, request.ActionId);
    }
    catch (KeyNotFoundException ex)
    {
        httpContext.Response.StatusCode = 404;
        await httpContext.Response.WriteAsJsonAsync(new { error = ex.Message }, ct);
        return;
    }

    var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    await foreach (var streamEvent in gm.StreamSceneAsync(request.GameId, messages, ct))
    {
        var eventData = JsonSerializer.Serialize(streamEvent, jsonOptions);
        await httpContext.Response.WriteAsync($"event: {streamEvent.Type}\n", ct);
        await httpContext.Response.WriteAsync($"data: {eventData}\n\n", ct);
        await httpContext.Response.Body.FlushAsync(ct);
    }
});

app.Run();
