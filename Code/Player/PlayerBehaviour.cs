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

	[Property, Group( "Refs" )] public GameObject gunPoint { get; set; }

	[Property, Group( "Refs" )] PlayerUI ui { get; set; }
	[Property, Group( "Refs" )] GameObject bullet { get; set; }
	[Property, Group( "Refs" )] MPlayerController controller { get; set; }
	[Property, Group( "Refs" )] ModelRenderer model { get; set; }

	[Sync( SyncFlags.FromHost )] public GameManager gameManager { get; set; }

	float hp;
	bool canShoot = true;

	CancellationTokenSource cancellation;

	protected override void OnStart()
	{
		AddEndBehaviour( new Bounce() );

		runtimePlayerStat = basePlayerStat;
		hp = runtimePlayerStat.hp;

		ui.SetHealth( hp, runtimePlayerStat.hp );

		if ( IsProxy )
		{
			controller.Destroy();
			ui.Destroy();
		}

		base.OnStart();
	}

	BulletInfo InitBaseBullet()
	{
		Vector3 direction = gunPoint.WorldPosition - WorldPosition;
		BulletInfo newBulletInfo = runtimePlayerStat.bulletInfo;

		direction = direction.WithZ( 0 );
		newBulletInfo.direction = direction.Normal;

		return newBulletInfo;
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
		SetInvulnerability( true );

		await Task.DelaySeconds( runtimePlayerStat.timeInvulnerability );

		SetInvulnerability( false );
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
			ui.SetHealth( hp, runtimePlayerStat.hp );


			if ( hp <= 0 )
			{
				Color colorTint = model.Tint;

				colorTint = Color.Blue;
				colorTint.a = 0.5f;

				model.Tint = colorTint;
				Tags.Add( "invulnerability" );

				canShoot = false;
				isDead = true;

				cancellation?.Cancel();
				cancellation?.Dispose();
				cancellation = null;

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

	public void SetInvulnerability( bool mode )
	{
		if ( !isDead )
		{
			Color colorTint = model.Tint;

			if ( mode )
			{
				colorTint.a = 0.5f;
				model.Tint = colorTint;

				isInvulnerable = true;
				Tags.Add( "invulnerability" );
			}
			else
			{
				colorTint.a = 1f;
				model.Tint = colorTint;

				isInvulnerable = false;
				Tags.Remove( "invulnerability" );
			}
		}

	}

	#region Upgrade methods

	#region Bullet modifiers methods
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

	#region Multiplier methods

	[Rpc.Owner]
	public void AddFireRateMultiplier( float multiplier )
	{
		PlayerStats playerStats = basePlayerStat;
		StatsMultiplier statsMultiplier = playerStats.statsMultiplier;

		if ( statsMultiplier.fireRateMultiplier == null )
		{
			statsMultiplier.fireRateMultiplier = new List<float>();
		}

		statsMultiplier.fireRateMultiplier.Add( multiplier );

		playerStats.statsMultiplier = statsMultiplier;
		playerStats.fireRate *= multiplier;

		basePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddDamageMultiplier( float multiplier )
	{
		PlayerStats playerStats = basePlayerStat;
		BulletInfo bulletInfo = playerStats.bulletInfo;
		StatsMultiplier statsMultiplier = playerStats.statsMultiplier;

		if ( statsMultiplier.damageMultiplier == null )
		{
			statsMultiplier.damageMultiplier = new List<float>();
		}

		statsMultiplier.damageMultiplier.Add( multiplier );

		playerStats.statsMultiplier = statsMultiplier;
		bulletInfo.damage *= multiplier;
		playerStats.bulletInfo = bulletInfo;

		basePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddSizeMultiplier( float multiplier )
	{
		PlayerStats playerStats = basePlayerStat;
		BulletInfo bulletInfo = playerStats.bulletInfo;
		StatsMultiplier statsMultiplier = playerStats.statsMultiplier;

		if ( statsMultiplier.sizeMultiplier == null )
		{
			statsMultiplier.sizeMultiplier = new List<float>();
		}

		statsMultiplier.sizeMultiplier.Add( multiplier );

		playerStats.statsMultiplier = statsMultiplier;
		bulletInfo.size *= multiplier;
		playerStats.bulletInfo = bulletInfo;

		basePlayerStat = playerStats;
	}

	#endregion

	#region Base stats methods

	[Rpc.Owner]
	public void AddHp( float amount )
	{
		PlayerStats playerStats = basePlayerStat;

		playerStats.hp += amount;

		basePlayerStat = playerStats;
		runtimePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddMoveSpeed( float amount )
	{
		PlayerStats playerStats = basePlayerStat;

		playerStats.moveSpeed += amount;

		basePlayerStat = playerStats;
		runtimePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddFireRate( float amount )
	{
		PlayerStats playerStats = basePlayerStat;
		float trueAmount = amount;

		if ( playerStats.statsMultiplier.fireRateMultiplier != null && playerStats.statsMultiplier.fireRateMultiplier.Count > 0 )
		{
			foreach ( float multiplier in playerStats.statsMultiplier.fireRateMultiplier )
			{
				trueAmount *= multiplier;
			}
		}

		playerStats.fireRate += trueAmount;

		basePlayerStat = playerStats;
		runtimePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddTimeInvulnerability( float amount )
	{
		PlayerStats playerStats = basePlayerStat;

		playerStats.timeInvulnerability += amount;

		basePlayerStat = playerStats;
		runtimePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddBulletDamage( float amount )
	{
		PlayerStats playerStats = basePlayerStat;
		BulletInfo bulletInfo = playerStats.bulletInfo;

		float trueAmount = amount;


		if ( playerStats.statsMultiplier.damageMultiplier != null && playerStats.statsMultiplier.damageMultiplier.Count > 0 )
		{
			foreach ( float multiplier in playerStats.statsMultiplier.damageMultiplier )
			{
				trueAmount *= multiplier;
			}
		}


		bulletInfo.damage += trueAmount;
		playerStats.bulletInfo = bulletInfo;

		basePlayerStat = playerStats;
		runtimePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddBulletSize( float amount )
	{
		PlayerStats playerStats = basePlayerStat;
		BulletInfo bulletInfo = playerStats.bulletInfo;
		float trueAmount = amount;


		if ( playerStats.statsMultiplier.sizeMultiplier != null && playerStats.statsMultiplier.sizeMultiplier.Count > 0 )
		{
			foreach ( float multiplier in playerStats.statsMultiplier.sizeMultiplier )
			{
				trueAmount *= multiplier;
			}
		}

		bulletInfo.size += trueAmount;
		playerStats.bulletInfo = bulletInfo;

		basePlayerStat = playerStats;
		runtimePlayerStat = playerStats;
	}

	[Rpc.Owner]
	public void AddBulletSpeed( float amount )
	{
		PlayerStats playerStats = basePlayerStat;
		BulletInfo bulletInfo = playerStats.bulletInfo;

		bulletInfo.speed += amount;
		playerStats.bulletInfo = bulletInfo;

		basePlayerStat = playerStats;
		runtimePlayerStat = playerStats;
	}

	#endregion

	#endregion

	[Rpc.Owner]
	public void ResetPlayer()
	{
		Color colorTint = model.Tint;

		colorTint = Color.White;
		colorTint.a = 1f;

		model.Tint = colorTint;
		Tags.Remove( "invulnerability" );
		runtimePlayerStat = basePlayerStat;
		hp = runtimePlayerStat.hp;
		canShoot = true;
		isDead = false;

	}

}




