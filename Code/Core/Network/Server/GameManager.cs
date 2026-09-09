using Sandbox;

// Run only on the host
public sealed class GameManager : Component
{
	[Property]
	public GameState GameState { get; set; }

	[Property]
	public GameObject PlayerPrefab { get; set; }

	[Property]
	public float StartDelay { get; set; } = 2f;

	protected override void OnStart()
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
		{
			Log.Error( "[GameManager] GameState is not assigned." );
			return;
		}

		// Initial game state.
		GameState.State = GameStateType.WaitingForPlayers;
		GameState.PlayerCount = Connection.All.Count;
		GameState.CurrentRoom = 0;

		Log.Info( "[GameManager] Initialized." );
	}
	public void SpawnPlayer( Connection connection )
	{
		if ( !Networking.IsHost )
			return;

		if ( PlayerPrefab == null )
		{
			Log.Error( "[GameManager] PlayerPrefab is not assigned." );
			return;
		}

		var player = PlayerPrefab.Clone( WorldTransform );

		player.NetworkSpawn( connection );

		Log.Info( $"Spawned player for {connection.DisplayName}" );
	}
	protected override void OnUpdate()
	{
		// GameManager is authoritative.
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		GameState.PlayerCount = Connection.All.Count;
	}

}
