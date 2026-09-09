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
		GameState.State = GameStateType.WaitingForPlayers;
		GameState.PlayerCount = Connection.All.Count;
		GameState.CurrentRound = 0;

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

		Log.Info( $"Spawned player for {connection.DisplayName}" );

		//temp need to be launch when the round start
		//cancellation = new CancellationTokenSource();
		//_ = RoundSpawner( cancellation.Token );
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

		GameState.PlayerCount = Connection.All.Count;
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
