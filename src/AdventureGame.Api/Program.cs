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
    builder.Configuration["OpenAI:ImageModel"] ?? "dall-e-3"));

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

app.Run();
