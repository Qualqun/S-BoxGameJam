using Sandbox;
using System;
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
	[Property, Group( "Refs" )]
	public BoxCollider StartZone { get; set; }

	[Property, Group( "Stats" )]
	public float StartDelay { get; set; } = 5f;

	[Description( "Number of enemy in one second" )]
	[Property, Group( "Stats" )]
	public float BaseSpawnRate { get; set; } = 1f;

	[Property, Group( "Stats" )]
	public StatsPerRound StatsGrowth { get; set; }

	[Property, Group( "Refs" )]
	public GameState GameState { get; set; }

	[Property, Group( "Refs" )]
	public GameObject PlayerPrefab { get; set; }

	[Property, Group( "Refs" )]
	public GameObject EnemyPrefab { get; set; }

	[Property, Group( "Refs" )]
	public List<GameObject> SpawnPoints { get; set; }


	List<GameObject> Players { get; set; } = new List<GameObject>();
	List<GameObject> Enemies { get; set; } = new List<GameObject>();

	float StartTimer { get; set; } = 0f;
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

		StartTimer = 0f;
		RoundTimer = 0f;

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

		Log.Info( $"[GameManager] Spawned player for {connection.DisplayName}" );
	}


	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( GameState == null )
			return;

		const float FixedDeltaTime = 0.02f;

		if ( GameState.State == GameStateType.Starting )
		{
			StartTimer += FixedDeltaTime;

			if ( StartTimer >= StartDelay )
			{
				StartTimer = 0f;

				GameState.Server_SetGameState( GameStateType.Playing );

				Log.Info( "[GameManager] Game officially started!" );

				StartRoundSpawner();
			}
		}


		if ( GameState.State == GameStateType.Playing )
		{
			RoundTimer += FixedDeltaTime;

			if ( RoundTimer >= GameState.TimePerRound )
			{
				RoundTimer = 0f;

				StopRoundSpawner();

				Log.Info( "[GameManager] Round finished." );

				// TODO:
				// Increment round
				// Start next round
			}
		}
	}


	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		GameState.Server_SetPlayerCount( Connection.All.Count );

		// Debug countdown
		if ( GameState.State == GameStateType.Starting )
		{
			float remaining = MathF.Max( 0f, StartDelay - StartTimer );

			Log.Info( $"Start countdown: {remaining:F1}s" );
		}

		// Debug round timer
		if ( GameState.State == GameStateType.Playing )
		{
			Log.Info( $"Round timer: {RoundTimer:F1}s / {GameState.TimePerRound:F1}s" );
		}
	}


	#region Game Initialization

	public void PlayerEnteredStartArea( PlayerBehaviour player )
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		if ( GameState.State != GameStateType.WaitingForPlayers )
		{
			Log.Info( "[GameManager] Game is already in progress. Cannot start a new game." );
			return;
		}

		//if ( GameState.PlayerReadyCount != GameState.PlayerCount )
		//{
		//	Log.Info( "[GameManager] Not enough players to start the game." );
		//	return;
		//}

		// Reset timer before starting.
		StartTimer = 0f;

		GameState.Server_SetGameState( GameStateType.Starting );

		Log.Info( "[GameManager] Game starting countdown..." );
	}

	public void PlayerLeftStartArea( PlayerBehaviour player )
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		if ( GameState.State != GameStateType.Starting )
			return;

		StartTimer = 0f;


		GameState.Server_SetGameState( GameStateType.WaitingForPlayers );
		Log.Info( "[GameManager] Game starting countdown canceled." );
	}


	void StartRoundSpawner()
	{
		// Stop a previous spawner if one exists.
		StopRoundSpawner();

		Cancellation = new CancellationTokenSource();

		_ = RoundSpawner( Cancellation.Token );
	}


	void StopRoundSpawner()
	{
		if ( Cancellation == null )
			return;

		Cancellation.Cancel();
		Cancellation.Dispose();
		Cancellation = null;
	}

	#endregion


	#region Enemies methods

	async Task RoundSpawner( CancellationToken token )
	{
		float spawnDelay =
			1f / (BaseSpawnRate + StatsGrowth.spawnRate * GameState.CurrentRound);

		while ( !token.IsCancellationRequested )
		{
			await Task.DelaySeconds( spawnDelay );

			if ( token.IsCancellationRequested )
				break;

			SpawnEnemy();
		}
	}


	void SpawnEnemy()
	{
		if ( EnemyPrefab == null )
		{
			Log.Error( "[GameManager] EnemyPrefab is not assigned." );
			return;
		}

		if ( SpawnPoints == null || SpawnPoints.Count == 0 )
		{
			Log.Error( "[GameManager] No spawn points assigned." );
			return;
		}

		int spawnPoint = Game.Random.Int( SpawnPoints.Count - 1 );
		Vector3 position = SpawnPoints[spawnPoint].WorldPosition;

		GameObject enemy = EnemyPrefab.Clone( position );
		EnemyBehaviour enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();

		enemy.NetworkSpawn();

		enemyBehaviour.SetPlayers( Players );

		enemyBehaviour.hp +=
			StatsGrowth.enemyHp * GameState.CurrentRound;

		enemyBehaviour.damage +=
			StatsGrowth.enemyDamage * GameState.CurrentRound;

		Enemies.Add( enemy );
	}


	[Rpc.Host]
	public void EnemyTakeDamage( EnemyBehaviour enemy, float amount )
	{
		enemy.TakeDamage( amount );
	}

	#endregion
}
