using Sandbox;
using System.Threading;
using System.Threading.Tasks;

public struct StatsPerRound
{
	[Description( "Number of enemy in one second" )]
	public float spawnRate { get; set; }
	public float enemyHp { get; set; }
	public float enemyDamage { get; set; }
}


// Run only on the host
public sealed class GameManager : Component
{

	[Property, Group( "Stats" )] public float StartDelay { get; set; } = 2f;

	[Description( "Number of enemy in one second" )]
	[Property, Group( "Stats" )] public float BaseSpawnRate { get; set; } = 1f;
	[Property, Group( "Stats" )] public StatsPerRound StatsGrowth { get; set; }


	[Property, Group( "Refs" )] public GameState GameState { get; set; }
	[Property, Group( "Refs" )] public GameObject PlayerPrefab { get; set; }

	[Property, Group( "Refs" )] public GameObject EnemyPrefab { get; set; }
	[Property, Group( "Refs" )] public List<GameObject> SpawnPoints { get; set; }


	List<GameObject> Players { get; set; } = new List<GameObject>();
	List<GameObject> Enemies { get; set; } = new List<GameObject>();

	float RoundTimer { get; set; } = 0f;
	CancellationTokenSource Cancellation;

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
		PlayerBehaviour playerBehaviour = player.GetComponent<PlayerBehaviour>();

		Players.Add( player );
		playerBehaviour.gameManager = this;

		player.NetworkSpawn( connection );


		Log.Info( $"Spawned player for {connection.DisplayName}" );

		//Temp need to be launch when the round start
		Cancellation = new CancellationTokenSource();
		_ = RoundSpawner( Cancellation.Token );
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

				if ( RoundTimer > GameState.TimePerRound )
				{
					Cancellation?.Cancel();
					Cancellation?.Dispose();
					Cancellation = null;
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

		if ( GameState.PlayerCount == 2 )
		{
			// game should start
			if ( GameState.State == GameStateType.WaitingForPlayers )
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
		float spawnDelay = 1f / (BaseSpawnRate + StatsGrowth.spawnRate * GameState.CurrentRound);

		while ( !token.IsCancellationRequested )
		{
			await Task.DelaySeconds( spawnDelay );

			SpawnEnemy();
		}
	}

	void SpawnEnemy()
	{
		int spawnPoint = Game.Random.Int( SpawnPoints.Count - 1 );
		Vector3 position = SpawnPoints[spawnPoint].WorldPosition;

		GameObject enemy = EnemyPrefab.Clone( position );
		EnemyBehaviour enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();

		enemy.NetworkSpawn();

		enemyBehaviour.SetPlayers( Players );
		enemyBehaviour.hp += StatsGrowth.enemyHp * GameState.CurrentRound;
		enemyBehaviour.damage += StatsGrowth.enemyDamage * GameState.CurrentRound;

		Enemies.Add( enemy );
	}

	[Rpc.Host]
	public void EnemyTakeDamage( EnemyBehaviour enemy, float amount )
	{
		enemy.TakeDamage( amount );
	}



	#endregion


}
