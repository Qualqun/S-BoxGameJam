using Sandbox;

public enum GameStateType
{
	WaitingForPlayers,
	Starting,
	Playing,
	GameOver
}

public struct RoundGrowth
{

}

public sealed class GameState : Component
{
	[Sync( SyncFlags.FromHost )]
	public GameStateType State { get; set; } = GameStateType.WaitingForPlayers;

	[Sync( SyncFlags.FromHost )]
	public int PlayerCount { get; set; }

	[Sync( SyncFlags.FromHost )]
	public int CurrentRound { get; set; }


	[Sync( SyncFlags.FromHost ), Property]
	public float TimePerRound { get; set; }


	public List<GameObject> Players { get; set; } = new List<GameObject>();
	public List<GameObject> Enemies { get; set; } = new List<GameObject>();
}
