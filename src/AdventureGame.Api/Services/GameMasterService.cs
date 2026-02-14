using System.Collections.Concurrent;
using System.Text.Json;
using AdventureGame.Shared;
using Microsoft.Extensions.AI;
using OpenAI.Images;

namespace AdventureGame.Api.Services;

public class GameMasterService
{
    private readonly IChatClient _chatClient;
    private readonly ImageClient _imageClient;
    private readonly ILogger<GameMasterService> _logger;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _games = new();

    private const string SystemPrompt = """
        You are a Game Master for a text-based adventure game.
        
        Your job is to create vivid, engaging scenes and present the player with choices.
        
        Rules:
        - Each scene should have a rich description (2-4 sentences)
        - Provide exactly 4 or 5 actions for the player to choose from
        - Actions should be varied: some safe, some risky, some creative
        - Maintain narrative continuity based on the conversation history
        - If the player dies or achieves a major victory, set isGameOver to true
        - Keep the tone fun and adventurous
        - Include a short imagePrompt (1 sentence) that describes the visual scene for image generation
        
        You MUST respond with valid JSON in this exact format, and nothing else:
        {
            "description": "The scene description here...",
            "imagePrompt": "A short visual description of the scene for image generation",
            "actions": [
                { "id": 1, "text": "Action description" },
                { "id": 2, "text": "Action description" },
                { "id": 3, "text": "Action description" },
                { "id": 4, "text": "Action description" }
            ],
            "isGameOver": false
        }
        """;

    public GameMasterService(IChatClient chatClient, ImageClient imageClient, ILogger<GameMasterService> logger)
    {
        _chatClient = chatClient;
        _imageClient = imageClient;
        _logger = logger;
    }

    public async Task<GameResponse> StartGameAsync(string? theme, CancellationToken ct = default)
    {
        var gameId = Guid.NewGuid().ToString("N");

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, string.IsNullOrWhiteSpace(theme)
                ? "Start a new adventure game. Set the scene and give me my first choices."
                : $"Start a new adventure game with the theme: {theme}. Set the scene and give me my first choices.")
        };

        var scene = await GetSceneFromAI(messages, ct);
        _games[gameId] = messages;

        return new GameResponse { GameId = gameId, Scene = scene };
    }

    public async Task<GameResponse> ChooseActionAsync(string gameId, int actionId, CancellationToken ct = default)
    {
        if (!_games.TryGetValue(gameId, out var messages))
            throw new KeyNotFoundException($"Game '{gameId}' not found.");

        var lastAssistantMsg = messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
        var actionText = $"I choose action {actionId}";

        if (lastAssistantMsg?.Text is not null)
        {
            try
            {
                var lastScene = JsonSerializer.Deserialize<AiSceneResponse>(lastAssistantMsg.Text, JsonOptions);
                var action = lastScene?.Actions?.FirstOrDefault(a => a.Id == actionId);
                if (action is not null)
                    actionText = $"I choose: {action.Text}";
            }
            catch
            {
                // Fall back to generic action text
            }
        }

        messages.Add(new ChatMessage(ChatRole.User, actionText));

        var scene = await GetSceneFromAI(messages, ct);

        return new GameResponse { GameId = gameId, Scene = scene };
    }

    private async Task<GameScene> GetSceneFromAI(List<ChatMessage> messages, CancellationToken ct)
    {
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);

        var assistantMessage = response.Messages.Last();
        messages.Add(assistantMessage);

        var json = assistantMessage.Text ?? throw new InvalidOperationException("Empty response from AI.");

        // Strip markdown code fences if present
        json = json.Trim();
        if (json.StartsWith("```"))
        {
            var firstNewline = json.IndexOf('\n');
            if (firstNewline >= 0)
                json = json[(firstNewline + 1)..];
            if (json.EndsWith("```"))
                json = json[..^3];
            json = json.Trim();
        }

        var aiResponse = JsonSerializer.Deserialize<AiSceneResponse>(json, JsonOptions)
                         ?? throw new InvalidOperationException("Failed to parse AI response.");

        // Generate image from the scene prompt
        string? imageUrl = null;
        if (!string.IsNullOrWhiteSpace(aiResponse.ImagePrompt))
        {
            imageUrl = await GenerateImageAsync(aiResponse.ImagePrompt, ct);
        }

        return new GameScene
        {
            Description = aiResponse.Description,
            Actions = aiResponse.Actions,
            IsGameOver = aiResponse.IsGameOver,
            ImageUrl = imageUrl
        };
    }

    private async Task<string?> GenerateImageAsync(string prompt, CancellationToken ct)
    {
        try
        {
            var options = new OpenAI.Images.ImageGenerationOptions
            {
                Size = GeneratedImageSize.W1792xH1024,
                Quality = GeneratedImageQuality.Standard,
                ResponseFormat = GeneratedImageFormat.Uri
            };

            var result = await _imageClient.GenerateImageAsync(
                $"Fantasy adventure game scene, digital art style: {prompt}",
                options,
                ct);

            return result.Value.ImageUri?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate image for scene");
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Internal model matching the AI JSON response (includes imagePrompt).
    /// </summary>
    private record AiSceneResponse
    {
        public required string Description { get; init; }
        public string? ImagePrompt { get; init; }
        public required List<GameAction> Actions { get; init; }
        public bool IsGameOver { get; init; }
    }
}
