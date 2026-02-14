namespace AdventureGame.Shared;

public record GameScene
{
    public required string Description { get; init; }
    public required List<GameAction> Actions { get; init; }
    public bool IsGameOver { get; init; }
    public string? ImageUrl { get; init; }
}

public record GameAction
{
    public required int Id { get; init; }
    public required string Text { get; init; }
}

public record StartGameRequest
{
    public string? Theme { get; init; }
}

public record ChooseActionRequest
{
    public required string GameId { get; init; }
    public required int ActionId { get; init; }
}

public record GameResponse
{
    public required string GameId { get; init; }
    public required GameScene Scene { get; init; }
}

/// <summary>
/// Represents a single Server-Sent Event for streaming game responses.
/// </summary>
public record StreamEvent
{
    /// <summary>
    /// The event type: "text", "scene", or "image".
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// For "text" events: the partial description text accumulated so far.
    /// For "scene" events: the full GameScene JSON (without ImageUrl).
    /// For "image" events: the image URL string.
    /// </summary>
    public required string Data { get; init; }

    /// <summary>
    /// For "scene" and "image" events: the game ID.
    /// </summary>
    public string? GameId { get; init; }
}
