using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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

	[Property, Group( "List Refs" )]
	public List<GameObject> EnemiesPrefabs { get; set; }

	[Property, Group( "List Refs" )]
	public List<GameObject> SpawnPoints { get; set; }

	List<PlayerBehaviour> Players { get; set; } = new List<PlayerBehaviour>();
	List<GameObject> Enemies { get; set; } = new List<GameObject>();

	float PhaseTimer { get; set; } = 0f;

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

		PhaseTimer = 0f;

		Log.Info( "[GameManager] Initialized." );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		const float FixedDeltaTime = 0.02f;

		// Timer shared
		PhaseTimer += FixedDeltaTime;

		// Phases of the game loop based on the current game state
		switch ( GameState.State )
		{
			case GameStateType.Starting:
				StartRoutine();
				break;

			case GameStateType.Playing:
				PlayRoutine();
				break;

			case GameStateType.WaitingForNextRound:
				WaitingForNextRoundRoutine();
				break;

			case GameStateType.GameOver:
				GameOverRoutine();
				break;

			default:
				break;
		}
	}


	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		//GameState.Server_SetPlayerCount( Connection.All.Count );

		// Debug countdown
		if ( GameState.State == GameStateType.Starting )
		{
			float remaining = MathF.Max( 0f, StartDelay - PhaseTimer );

			Log.Info( $"Start countdown: {remaining:F1}s" );
		}

		// Debug round timer
		if ( GameState.State == GameStateType.Playing )
		{
			//Log.Info( $"Round timer: {PhaseTimer:F1}s / {GameState.TimePerRound:F1}s" );
		}

		// Debug waiting timer
		if ( GameState.State == GameStateType.WaitingForNextRound )
		{
			Log.Info( $"Next round in: {PhaseTimer:F1}s / {GameState.TimePerWaitingRound:F1}s" );
		}

		if ( GameState.State == GameStateType.GameOver )
		{
			Log.Info( $"Game Over timer: {PhaseTimer:F1}s / {GameState.TimeGameOver:F1}s" );
		}
	}


	#region Game Initialization

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

		Players.Add( playerBehaviour );
		playerBehaviour.gameManager = this;

		player.NetworkSpawn( connection );

		Log.Info( $"[GameManager] Spawned player for {connection.DisplayName}" );
	}


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

		// Reset phase timer before starting.
		PhaseTimer = 0f;

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

		PhaseTimer = 0f;

		GameState.Server_SetGameState( GameStateType.WaitingForPlayers );

		Log.Info( "[GameManager] Game starting countdown canceled." );
	}

	#endregion


	#region Gameplay loop

	void StartRoutine()
	{
		if ( PhaseTimer >= StartDelay )
		{
			PhaseTimer = 0f;

			GameState.Server_SetCurrentRound(
				GameState.CurrentRound + 1
			);

			GameState.Server_SetGameState( GameStateType.Playing );

			Log.Info( "[GameManager] Game officially started!" );

			StartRoundSpawner();
		}
	}


	private void PlayRoutine()
	{
		if ( PhaseTimer >= GameState.TimePerRound )
		{
			PhaseTimer = 0f;

			StopRoundSpawner();

			GameState.Server_SetGameState(
				GameStateType.WaitingForNextRound
			);

			Log.Info( "[GameManager] Round finished." );
		}
	}


	private void WaitingForNextRoundRoutine()
	{
		if ( PhaseTimer >= GameState.TimePerWaitingRound )
		{
			PhaseTimer = 0f;

			StopRoundSpawner();

			GameState.Server_SetGameState(
				GameStateType.Playing
			);

			StartRoundSpawner();

			Log.Info( "[GameManager] Next round started." );
		}
	}
	private void GameOverRoutine()
	{
		if ( PhaseTimer >= GameState.TimeGameOver )
		{
			StopRoundSpawner();

			GameState.Server_SetGameState(
				GameStateType.WaitingForNextRound
			);

			PhaseTimer = 0f;

			Log.Info( "[GameManager] Players should restart" );
		}
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

	[Rpc.Broadcast]
	public void PlayerTakeDamage( PlayerBehaviour player, float amount )
	{
		player.TakeHit( amount );
	}


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
		if ( EnemiesPrefabs == null  || EnemiesPrefabs.Count == 0)
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
		int enemyType = Game.Random.Int( EnemiesPrefabs.Count - 1 );

		Vector3 position = SpawnPoints[spawnPoint].WorldPosition;

		GameObject enemyPrefab = EnemiesPrefabs[enemyType];
		GameObject enemy = enemyPrefab.Clone( position );

		BaseEnemyBehaviour enemyBehaviour = enemy.GetComponent<BaseEnemyBehaviour>();

		enemy.NetworkSpawn();

		enemyBehaviour.gameManager = this;
		enemyBehaviour.SetPlayers( Players );
		enemyBehaviour.hp += StatsGrowth.enemyHp * GameState.CurrentRound;
		enemyBehaviour.damage += StatsGrowth.enemyDamage * GameState.CurrentRound;

		Enemies.Add( enemy );
	}


	[Rpc.Host]
	public void EnemyTakeDamage( BaseEnemyBehaviour enemy, float amount )
	{
		enemy.TakeDamage( amount );
	}



	#endregion
}
