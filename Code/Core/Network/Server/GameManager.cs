using Sandbox;
using System.Threading;
using System.Threading.Tasks;

// Run only on the host
public sealed class GameManager : Component
{
	[Property]
	public GameState GameState { get; set; }

	[Property]
	public GameObject PlayerPrefab { get; set; }

	[Property]
	public GameObject EnemyPrefab { get; set; }

	[Property]
	public float StartDelay { get; set; } = 2f;

	[Property]
	public List<GameObject> SpawnPoints { get; set; }

	float RoundTimer { get; set; } = 0f;
	CancellationTokenSource cancellation;

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
		GameState.Server_SetGameState( GameStateType.WaitingForPlayers );
		GameState.Server_SetPlayerCount( Connection.All.Count );
		GameState.Server_SetCurrentRound( 0 );

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

		GameObject player = PlayerPrefab.Clone( WorldTransform );

		GameState.Players.Add( player );

		player.NetworkSpawn( connection );

		Log.Info( $"[GameManager] Spawned player for {connection.DisplayName}" );
	}


	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( GameState.State == GameStateType.Playing )
		{
			if ( RoundTimer <= GameState.TimePerRound )
			{
				RoundTimer += 0.02f;

				if( RoundTimer > GameState.TimePerRound )
				{
					cancellation?.Cancel();
					cancellation?.Dispose();
					cancellation = null;
				}
			}
		}
	}



	protected override void OnUpdate()
	{
		// GameManager is authoritative.
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		// example of how to change the game state from the server

		if (GameState.PlayerCount == 2)
		{
			// game should start
			if(GameState.State == GameStateType.WaitingForPlayers )
			{
				GameState.Server_SetGameState( GameStateType.Starting );
				GameState.Server_SetTimePerRound( 30f );
				RoundTimer = 0f;
				SpawnEnemy();
				Log.Info( "[GameManager] Game is starting! ." );
			}
		}

		GameState.Server_SetPlayerCount( Connection.All.Count );
	}

	#region Enemies methods

	async Task RoundSpawner( CancellationToken token )
	{
		while( !token.IsCancellationRequested )
		{
			await Task.DelaySeconds( 1f );

			SpawnEnemy();

		}
	}

	void SpawnEnemy()
	{
		int spawnPoint = Game.Random.Int( SpawnPoints.Count - 1 );
		Vector3 position = SpawnPoints[spawnPoint].WorldPosition;

		GameObject enemy = EnemyPrefab.Clone( position );
		EnemyBehaviour behaviour = enemy.GetComponent<EnemyBehaviour>();

		enemy.NetworkSpawn();
		behaviour.Players = GameState.Players;
	}


	#endregion


}
