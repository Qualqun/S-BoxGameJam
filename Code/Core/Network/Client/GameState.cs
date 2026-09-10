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
	public GameStateType State { get; private set; } = GameStateType.WaitingForPlayers;
																																		
	[Sync( SyncFlags.FromHost )]
	public int PlayerCount { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public int PlayerReadyCount { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public int CurrentRound { get; private set; }

	[Sync( SyncFlags.FromHost ), Property]
	public float TimePerRound { get; private set; }

	public List<GameObject> Players { get; private set; } = new List<GameObject>();
	public List<GameObject> Enemies { get; set; } = new List<GameObject>();


	#region Server Setters

	public void Server_SetGameState( GameStateType state )
	{
		if ( !Networking.IsHost )
			return;

		State = state;
	}

	public void Server_SetPlayerCount( int count )
	{
		if ( !Networking.IsHost )
			return;
		PlayerCount = count;
	}

	public void Server_SetCurrentRound( int round )
	{
		if ( !Networking.IsHost )
			return;
		CurrentRound = round;
	}

	public void Server_SetTimePerRound( float time )
	{
		if ( !Networking.IsHost )
			return;
		TimePerRound = time;
	}

	public void Server_AddPlayer( GameObject player )
	{
		if ( !Networking.IsHost )
			return;
		Players.Add( player );
	}

	public void Server_RemovePlayer( GameObject player )
	{
		if ( !Networking.IsHost )
			return;
		Players.Remove( player );
	}

	public void Server_AddEnemy( GameObject enemy )
	{
		if ( !Networking.IsHost )
			return;
		Enemies.Add( enemy );
	}

	public void Server_RemoveEnemy( GameObject enemy )
	{
		if ( !Networking.IsHost )
			return;
		Enemies.Remove( enemy );
	}

	public void Server_SetPlayerReadyCount( int count )
	{
		if ( !Networking.IsHost )
			return;
		PlayerReadyCount = count;
	}

	#endregion

}
