using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Network;

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
	[Property, Group( "Stats" )] public float BaseSpawnRate { get; set; } = 0.1f;
	[Property, Group( "Stats" )] public float RoundSpawnRate { get; set; } = 0.35f;

	[Property, Group( "Refs" )] public GameState GameState { get; set; }

	[Property, Group( "Refs" )] public GameObject PlayerPrefab { get; set; }
	[Property, Group( "Refs" )] public GameObject StartZonePoint { get; set; }
	[Property, Group( "Refs" )] public UIManager UiManager { get; set; }

	[Property, Group( "List Refs" )] public List<GameObject> EnemiesPrefabs { get; set; }

	[Property, Group( "List Refs" )] public List<GameObject> SpawnPoints { get; set; }
	CancellationTokenSource Cancellation;

	protected override void OnStart()
	{
		LobbyConfig config = new();

		config.MaxPlayers = 4;

		Networking.CreateLobby( config );

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

		if ( GameState.State != GameStateType.WaitingForPlayers && GameState.State != GameStateType.GameOver )
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
	}

	public bool AreAllPlayersDeadC()
	{
		if ( !Networking.IsHost )
			return false;

		var allPlayers = Scene.GetAllComponents<PlayerBehaviour>().ToList();
		Log.Info( $"[AreAllPlayersDeadC] Checking {allPlayers.Count} players in scene..." );

		if ( allPlayers.Count == 0 ) return false;

		bool allDead = true;

		foreach ( var player in allPlayers )
		{
			if ( player != null && player.IsValid() && !player.isDead )
			{
				allDead = false;
				break;
			}
		}

		return allDead;
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
		Log.Info( $"[GameManager] Start spawn player for {connection.DisplayName}" );

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

		if ( player.isInStartZone )
			return;

		player.isInStartZone = true;
		GameState.Server_SetPlayerReadyCount( GameState.PlayerReadyCount + 1 );

		if ( GameState.State == GameStateType.WaitingForPlayers )
		{
			if ( GameState.Players.Count > 0 && GameState.PlayerReadyCount >= GameState.Players.Count )
			{
				GameState.Server_SetPhaseTimer( TimeStart );
				GameState.Server_SetGameState( GameStateType.Starting );
				Log.Info( "[GameManager] Game starting countdown..." );
			}
		}
	}

	public void PlayerLeftStartArea( PlayerBehaviour player )
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		if ( !player.isInStartZone )
			return;

		player.isInStartZone = false;
		GameState.Server_SetPlayerReadyCount( GameState.PlayerReadyCount - 1 );

		if ( GameState.State == GameStateType.Starting )
		{
			if ( GameState.PlayerReadyCount < GameState.Players.Count )
			{
				GameState.Server_SetPhaseTimer( 0f );
				GameState.Server_SetGameState( GameStateType.WaitingForPlayers );
				Log.Info( "[GameManager] Game starting countdown canceled." );
			}
		}
	}

	#endregion

	#region Gameplay loop

	public bool AllEnemiesDead()
	{
		return GameState.Enemies.All( enemy => enemy == null || !enemy.IsValid() );
	}


	public void GameOver()
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		if ( GameState.State != GameStateType.Playing )
			return;

		StopRoundSpawner();
		RemoveAllEnemies();
		Broadcast_ShowContinuePrompt();

		var allPlayers = Scene.GetAllComponents<PlayerBehaviour>().ToList();
		foreach ( var player in allPlayers )
		{
			if ( player != null && player.IsValid() && player.State != null )
			{
				player.State.RewardTaken = false;
			}
		}

		GameState.Server_SetPhaseTimer( 0f );
		GameState.Server_SetGameState( GameStateType.GameOver );
	}
	public void StartNextRound( bool isGameOver = false )
	{
		if ( !Networking.IsHost )
			return;

		if ( GameState == null )
			return;

		if ( GameState.State != GameStateType.Playing && GameState.State != GameStateType.GameOver )
			return;

		Broadcast_HideEnemyCount();

		GameState.Server_SetPhaseTimer( TimePerWaitingRound );
		GameState.Server_SetGameState( GameStateType.WaitingForNextRound );

		if ( isGameOver )
		{
			Log.Info( "[GameManager] Restart after game over..." );
		}
		else
		{
			Log.Info( "[GameManager] Starting next round all enemies are dead..." );
		}
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

	public bool AreAllPlayersTakedBoost()
	{
		if ( !Networking.IsHost )
			return false;

		var allPlayers = Scene.GetAllComponents<PlayerBehaviour>().ToList();
		int readyCount = 0;
		int validPlayers = 0;

		foreach ( var player in allPlayers )
		{
			if ( player == null || !player.IsValid() )
				continue;

			validPlayers++;

			if ( player.State != null && player.State.RewardTaken )
			{
				readyCount++;
			}
		}

		if ( validPlayers == 0 )
			return false;

		return readyCount == validPlayers;
	}

	private void GameOverRoutine()
	{
		if ( AreAllPlayersTakedBoost() )
		{
			var allPlayers = Scene.GetAllComponents<PlayerBehaviour>().ToList();
			foreach ( var player in allPlayers )
			{
				if ( player != null && player.IsValid() )
				{
					if ( player.State != null )
					{
						player.State.RewardTaken = false;
					}
					player.Revive();
				}
			}
			StartNextRound( true );
		}
	}

	public void RemoveAllEnemies()
	{
		foreach ( var enemy in GameState.Enemies )
		{
			if ( enemy != null && enemy.IsValid() )
			{
				enemy.Destroy();
			}
		}
		GameState.Server_ClearEnemies();
	}

	void StartRoundSpawner()
	{
		StopRoundSpawner();
		Cancellation = new CancellationTokenSource();
		_ = RoundSpawner( Cancellation.Token );
	}

	public void StopRoundSpawner()
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
		player.Server_TakeHit( amount );
	}

	[Rpc.Host]
	public void PlayerTakeDamageFromEnemy( PlayerBehaviour player, GameObject enemyObj )
	{
		if ( player == null || !player.IsValid() )
			return;

		if ( enemyObj == null || !enemyObj.IsValid() )
			return;

		BaseEnemyBehaviour enemy = enemyObj.GetComponent<BaseEnemyBehaviour>();

		if ( enemy == null )
			return;

		player.Server_TakeHit( enemy.meleDamage );
	}

	#region Enemies methods

	async Task RoundSpawner( CancellationToken token )
	{
		float spawnDelay = 1f / (BaseSpawnRate + RoundSpawnRate * GameState.CurrentRound);

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
		enemyBehaviour.InitStats( GameState.CurrentRound );

		GameState.Server_AddEnemy( enemy );
	}

	[Rpc.Host]
	public void EnemyTakeDamage( GameObject enemyObj, float amount )
	{
		BaseEnemyBehaviour enemy = enemyObj.GetComponent<BaseEnemyBehaviour>();

		if ( enemy == null )
			return;

		enemy.TakeDamage( amount );
	}

	#endregion
}
