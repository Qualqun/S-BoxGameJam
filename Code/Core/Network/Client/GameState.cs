using Sandbox;

public enum GameStateType
{
	WaitingForPlayers,
	Starting,
	Playing,
	GameOver
}

public sealed class GameState : Component
{
	[Sync( SyncFlags.FromHost )]
	public GameStateType State { get; set; } = GameStateType.WaitingForPlayers;

	[Sync( SyncFlags.FromHost )]
	public int PlayerCount { get; set; }

	[Sync( SyncFlags.FromHost )]
	public int CurrentRoom { get; set; }
}
