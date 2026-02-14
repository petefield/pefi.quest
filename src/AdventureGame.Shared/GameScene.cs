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
