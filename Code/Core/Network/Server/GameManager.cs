using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public struct StatsPerRound
{
	[Description( "Number of enemy in one second" )]
	public float spawnRate { get; set; }
	public float enemyHp { get; set; }
	public float enemyDamage { get; set; }
}

public sealed class GameManager : Component
{
	[Property, Group( "Refs" )]
	public BoxCollider StartZone { get; set; }

	[Description( "Phases timer values" )]
	[Property, Group( "Timers" )] public float TimePerRound { get; private set; }

	[Property, Group( "Timers" )] public float TimePerWaitingRound { get; private set; }

	[Property, Group( "Timers" )] public float TimeGameOver { get; private set; }

	[Property, Group( "Timers" )] public float TimeStart { get; set; } = 5f;

	[Description( "Number of enemy in one second" )]
	[Property, Group( "Stats" )] public float BaseSpawnRate { get; set; } = 1f;

	[Property, Group( "Stats" )] public StatsPerRound StatsGrowth { get; set; }

	[Property, Group( "Refs" )] public GameState GameState { get; set; }

	[Property, Group( "Refs" )] public GameObject PlayerPrefab { get; set; }

	[Property, Group( "List Refs" )] public List<GameObject> EnemiesPrefabs { get; set; }

	[Property, Group( "List Refs" )] public List<GameObject> SpawnPoints { get; set; }
	CancellationTokenSource Cancellation;



	protected override void OnStart()
	{
		if ( !Networking.IsHost ) return;

		if ( GameState == null )
		{
			Log.Error( "[GameManager] GameState is not assigned." );
			return;
		}

		GameState.Server_SetGameState( GameStateType.WaitingForPlayers );
		GameState.Server_SetCurrentRound( 0 );
		GameState.Server_SetPhaseTimer( 0f );

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

		if ( GameState.State != GameStateType.WaitingForPlayers )
			GameState.Server_SetPhaseTimer( MathF.Max( 0f, GameState.PhaseTimer - FixedDeltaTime ) );

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
		}
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		if ( GameState.State == GameStateType.Starting )
		{
			float remaining = MathF.Max( 0f, TimeStart - GameState.PhaseTimer );
			Log.Info( $"Start countdown: {remaining:F1}s" );
		}

		if ( GameState.State == GameStateType.WaitingForNextRound )
		{
			Log.Info( $"Next round in: {GameState.PhaseTimer:F1}s / {TimePerWaitingRound:F1}s" );
		}

		if ( GameState.State == GameStateType.GameOver )
		{
			Log.Info( $"Game Over timer: {GameState.PhaseTimer:F1}s / {TimeGameOver:F1}s" );
		}
	}

	public void CheckPlayersState()
	{
		if ( !Networking.IsHost ) return;

		bool allDead = true;

		// check if all players are dead
		foreach ( var player in GameState.Players )
		{
			if ( player.State.Life > 0 )
			{
				allDead = false;
				break;
			}
		}

		// If all players are dead, show the death rewards UI and transition to GameOver
		if ( allDead && GameState.State != GameStateType.GameOver )
		{
			GameState.Server_SetGameState( GameStateType.GameOver );
			Broadcast_ShowContinuePrompt();
		}
	}

	[Rpc.Broadcast]
	public void Broadcast_ShowDeathRewards()
	{
		DeathRewards panel = Scene.GetAllComponents<DeathRewards>().FirstOrDefault();

		if ( panel != null )
			panel.Show();
		else
			Log.Warning( "DeathRewards not found in the scene!" );
	}

	[Rpc.Broadcast]
	public void Broadcast_ShowEnemyCount()
	{
		GameHUD panel = Scene.GetAllComponents<GameHUD>().FirstOrDefault();

		if ( panel != null )
			panel.ShowEnemyCount();
		else
			Log.Warning( "ShowEnemyCount not found in the scene!" );
	}

	[Rpc.Broadcast]
	public void Broadcast_HideEnemyCount()
	{
		GameHUD panel = Scene.GetAllComponents<GameHUD>().FirstOrDefault();

		if ( panel != null )
			panel.HideEnemyCount();
		else
			Log.Warning( "ShowEnemyCount not found in the scene!" );
	}

	[Rpc.Broadcast]
	public void Broadcast_ShowContinuePrompt()
	{
		ContinuePrompt panel = Scene.GetAllComponents<ContinuePrompt>().FirstOrDefault();

		if ( panel != null )
			panel.Show();
		else
			Log.Warning( "ContinuePrompt not found in the scene!" );
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

		GameState.Server_AddPlayer( playerBehaviour );
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
			return;

		GameState.Server_SetPhaseTimer( TimeStart );
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

		GameState.Server_SetPhaseTimer( 0f );
		GameState.Server_SetGameState( GameStateType.WaitingForPlayers );
		Log.Info( "[GameManager] Game starting countdown canceled." );
	}

	#endregion

	#region Gameplay loop

	public bool AllEnemiesDead()
	{
		return GameState.Enemies.All( enemy => enemy == null || !enemy.IsValid() );
	}

	public void StartNextRound()
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		if ( GameState.State != GameStateType.Playing )
			return;

		Broadcast_HideEnemyCount();

		GameState.Server_SetPhaseTimer( TimePerWaitingRound );
		GameState.Server_SetGameState( GameStateType.WaitingForNextRound );
		Log.Info( "[GameManager] Starting next round all enemies are dead..." );
	}
	void StartRoutine()
	{
		if ( GameState.PhaseTimer <= 0f )
		{
			GameState.Server_SetCurrentRound( GameState.CurrentRound + 1 );
			GameState.Server_SetPhaseTimer( TimePerRound );
			GameState.Server_SetGameState( GameStateType.Playing );
			StartRoundSpawner();
		}
	}

	private void WaitingForNextRoundRoutine()
	{
		if ( GameState.PhaseTimer <= 0f )
		{
			StopRoundSpawner();
			GameState.Server_SetPhaseTimer( TimePerRound );
			GameState.Server_SetGameState( GameStateType.Playing );
			StartRoundSpawner();
		}
	}

	private void PlayRoutine()
	{

		if ( GameState.PhaseTimer <= 0f )
		{
			StopRoundSpawner();

			Broadcast_ShowEnemyCount();

			//GameState.Server_SetPhaseTimer( TimePerWaitingRound );
			//GameState.Server_SetGameState( GameStateType.WaitingForNextRound );
		}

	}

	private void GameOverRoutine()
	{
		if ( GameState.PhaseTimer <= 0f )
		{
			StopRoundSpawner();
			GameState.Server_SetGameState( GameStateType.WaitingForNextRound );
			GameState.Server_SetPhaseTimer( TimePerWaitingRound );
			Log.Info( "[GameManager] Players should restart" );
		}
	}

	void StartRoundSpawner()
	{
		StopRoundSpawner();
		Cancellation = new CancellationTokenSource();
		_ = RoundSpawner( Cancellation.Token );
	}

	void StopRoundSpawner()
	{
		if ( Cancellation == null ) return;
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
		float spawnDelay = 1f / (BaseSpawnRate + StatsGrowth.spawnRate * GameState.CurrentRound);

		while ( !token.IsCancellationRequested )
		{
			await Task.DelaySeconds( spawnDelay );
			if ( token.IsCancellationRequested ) break;
			SpawnEnemy();
		}
	}

	void SpawnEnemy()
	{
		if ( EnemiesPrefabs == null || EnemiesPrefabs.Count == 0 ) return;
		if ( SpawnPoints == null || SpawnPoints.Count == 0 ) return;

		int spawnPoint = Game.Random.Int( SpawnPoints.Count - 1 );
		int enemyType = Game.Random.Int( EnemiesPrefabs.Count - 1 );
		Vector3 position = SpawnPoints[spawnPoint].WorldPosition;

		GameObject enemyPrefab = EnemiesPrefabs[enemyType];
		GameObject enemy = enemyPrefab.Clone( position );

		BaseEnemyBehaviour enemyBehaviour = enemy.GetComponent<BaseEnemyBehaviour>();
		enemy.NetworkSpawn();

		enemyBehaviour.gameManager = this;
		enemyBehaviour.SetPlayers( GameState.Players );
		enemyBehaviour.hp += StatsGrowth.enemyHp * GameState.CurrentRound;
		enemyBehaviour.meleDamage += StatsGrowth.enemyDamage * GameState.CurrentRound;

		GameState.Server_AddEnemy( enemy );
	}

	[Rpc.Host]
	public void EnemyTakeDamage( GameObject enemyObj, float amount )
	{
		BaseEnemyBehaviour enemy = enemyObj.GetComponent<BaseEnemyBehaviour>();

		enemy.TakeDamage( amount );
	}

	#endregion
}
