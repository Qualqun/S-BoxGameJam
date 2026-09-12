using Sandbox;
using System.Threading;
using System.Threading.Tasks;


public struct StatsMultiplier
{
	public List<float> fireRateMultiplier { get; set; }
	public List<float> damageMultiplier { get; set; }
	public List<float> sizeMultiplier { get; set; }
}


public struct PlayerStats
{
	public float hp { get; set; }
	public float moveSpeed { get; set; }
	public float fireRate { get; set; }
	public float timeInvulnerability { get; set; }
	public StatsMultiplier statsMultiplier { get; set; }
	public BulletInfo bulletInfo { get; set; }
	[Hide] public List<GunOutPut> gunOutPuts { get; set; }
}



public sealed class PlayerBehaviour : Component, Component.ICollisionListener
{
	[Sync] public bool isInvulnerable { get; set; }
	[Sync] public bool isDead { get; set; } = false;

	[Header( "Stats" )]
	[Property] public PlayerStats basePlayerStat { get; set; }
	public PlayerStats runtimePlayerStat { get; set; }

	[Property, Group( "Refs" )] GameObject gunPoint { get; set; }
	[Property, Group( "Refs" )] GameObject bullet { get; set; }
	[Property, Group( "Refs" )] MPlayerController controller { get; set; }
	[Property, Group( "Refs" )] ModelRenderer model { get; set; }

	[Sync( SyncFlags.FromHost )] public GameManager gameManager { get; set; }



	float hp;
	bool canShoot = true;

	CancellationTokenSource cancellation;

	protected override void OnStart()
	{

		runtimePlayerStat = basePlayerStat;

		if ( IsProxy )
		{
			controller.Destroy();
		}

		base.OnStart();

	}

	async Task StartTimer( CancellationToken token )
	{
		float fireRate = (1f / runtimePlayerStat.fireRate).Clamp( 0.01f, float.MaxValue );

		canShoot = false;

		await Task.DelaySeconds( fireRate );

		if ( token.IsCancellationRequested )
		{
			return;
		}

		canShoot = true;
	}

	async Task TimerHit()
	{
		Color colorTint = model.Tint;

		colorTint.a = 0.5f;
		model.Tint = colorTint;

		isInvulnerable = true;
		Tags.Add( "invulnerability" );

		await Task.DelaySeconds( runtimePlayerStat.timeInvulnerability );

		colorTint.a = 1f;
		model.Tint = colorTint;

		isInvulnerable = false;
		Tags.Remove( "invulnerability" );
	}

	public void OnCollisionStart( Collision collision )
	{
		Collider other = collision.Other.Collider;

		if ( other.Tags.Has( "enemy" ) )
		{
			BaseEnemyBehaviour enemy = other.GameObject.GetComponent<BaseEnemyBehaviour>();

			TakeHit( enemy.damage );
		}


	}
	public void TakeHit( float amount )
	{
		if ( !isInvulnerable )
		{
			hp -= amount;

			if ( hp <= 0 )
			{
				Color colorTint = model.Tint;

				colorTint = Color.Blue;
				colorTint.a = 0.5f;

				model.Tint = colorTint;

				canShoot = false;

				cancellation?.Cancel();
				cancellation?.Dispose();
				cancellation = null;

				Tags.Add( "invulnerability" );

				gameManager.GameState.Server_SetGameState( GameStateType.GameOver );
			}
			else
			{
				_ = TimerHit();
			}
		}
	}
	public void Fire()
	{
		if ( canShoot )
		{
			List<BulletInfo> bullets = new();
			BulletInfo baseBullet = InitBaseBullet();

			bullets.Add( baseBullet );

			if ( runtimePlayerStat.gunOutPuts != null && runtimePlayerStat.gunOutPuts.Count > 0 )
			{
				foreach ( GunOutPut modifier in runtimePlayerStat.gunOutPuts )
				{
					List<BulletInfo> newBullets = new();

					foreach ( BulletInfo bulletInfo in bullets )
					{
						modifier.OutPutBehaviour( bulletInfo, newBullets );
					}

					bullets = newBullets;
				}

			}


			foreach ( BulletInfo bulletInfo in bullets )
			{
				GameObject newBullet = bullet.Clone( gunPoint.WorldPosition );
				BulletBehaviour bulletBehaviour = newBullet.GetComponent<BulletBehaviour>();

				bulletBehaviour.InitBall( bulletInfo, gameManager );

				newBullet.NetworkSpawn();
			}


			cancellation = new CancellationTokenSource();
			_ = StartTimer( cancellation.Token );
		}
	}


	BulletInfo InitBaseBullet()
	{
		Vector3 direction = gunPoint.WorldPosition - WorldPosition;
		BulletInfo newBulletInfo = runtimePlayerStat.bulletInfo;

		direction = direction.WithZ( 0 );
		newBulletInfo.direction = direction.Normal;

		return newBulletInfo;
	}

	#region Upgrade methods


	[Rpc.Owner]
	public void AddGunOutPut( GunOutPut newModifier )
	{
		PlayerStats playerStats = basePlayerStat;

		if ( playerStats.gunOutPuts == null )
		{
			playerStats.gunOutPuts = new List<GunOutPut>();
		}

		playerStats.gunOutPuts.Add( newModifier );
		basePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddOnAirBehaviour( OnAirModifier newModifier )
	{
		PlayerStats playerStats = basePlayerStat;
		BulletInfo bulletInfo = playerStats.bulletInfo;

		if ( bulletInfo.onAirBehaviours == null )
		{
			bulletInfo.onAirBehaviours = new List<OnAirModifier>();
		}

		bulletInfo.onAirBehaviours.Add( newModifier );
		playerStats.bulletInfo = bulletInfo;
		basePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddEndBehaviour( EndModifier newModifier )
	{
		PlayerStats playerStats = basePlayerStat;
		BulletInfo bulletInfo = playerStats.bulletInfo;

		if ( bulletInfo.endModifiers == null )
		{
			bulletInfo.endModifiers = new List<EndModifier>();
		}

		bulletInfo.endModifiers.Add( newModifier );
		playerStats.bulletInfo = bulletInfo;
		basePlayerStat = playerStats;
	}

	#endregion

}
