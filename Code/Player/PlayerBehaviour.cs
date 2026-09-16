using Sandbox;
using System.Threading;
using System.Threading.Tasks;

public sealed class PlayerBehaviour : Component, Component.ICollisionListener
{
	[Sync] public bool isInvulnerable { get; set; }
	[Sync] public bool isDead { get; set; } = false;
	[Property, Group( "Refs" )] public PlayerState State { get; set; }
	[Property, Group( "Refs" )] public GameObject gunPoint { get; set; }
	[Property, Group( "Refs" )] GameObject bullet { get; set; }
	[Property, Group( "Refs" )] GameObject cameraPivot { get; set; }

	[Property, Group( "Refs" )] PlayerUI ui { get; set; }
	[Property, Group( "Refs" )] MPlayerController controller { get; set; }
	[Property, Group( "Refs" )] ModelRenderer model { get; set; }

	[Sync( SyncFlags.FromHost )] public GameManager gameManager { get; set; }

	bool canShoot = true;
	CancellationTokenSource cancellation;

	protected override void OnStart()
	{
		if ( State == null ) State = Components.Get<PlayerState>();

		ui.SetHealth( State.Hp, State.MaxHp );

		if ( IsProxy )
		{
			cameraPivot.Destroy();
			controller.Destroy();
			ui.Destroy();
		}

		base.OnStart();
	}

	BulletInfo InitBaseBullet()
	{
		Vector3 direction = gunPoint.WorldPosition - WorldPosition;
		direction = direction.WithZ( 0 ).Normal;

		BulletInfo newBulletInfo = new BulletInfo
		{
			size = State.BulletSize,
			speed = State.BulletSpeed,
			damage = State.BulletDamage,
			direction = direction,
			onAirModifier = new List<OnAirModifier>(),
			endModifiers = new List<EndModifier>()
		};

		foreach ( ModifierType mod in State.ActiveModifiers )
		{
			if ( mod == ModifierType.HomingShot )
				newBulletInfo.AddAirModifier( new HomingShot() );

			if ( mod == ModifierType.Bounce )
				newBulletInfo.AddEndModifiers( new Bounce() );

			if ( mod == ModifierType.Percing )
				newBulletInfo.AddEndModifiers( new Percing() );

			if ( mod == ModifierType.Explosion )
				newBulletInfo.AddEndModifiers( new EndExplosion() );
		}

		return newBulletInfo;
	}

	async Task StartTimer( CancellationToken token )
	{
		float fireRate = (1f / State.FireRate).Clamp( 0.01f, float.MaxValue );
		canShoot = false;
		await Task.DelaySeconds( fireRate );

		if ( token.IsCancellationRequested )
			return;

		canShoot = true;
	}

	async Task TimerHit()
	{
		SetInvulnerability( true );

		await Task.DelaySeconds( State.TimeInvulnerability );

		SetInvulnerability( false );
	}

	public void OnCollisionStart( Collision collision )
	{
		GameObject otherObj = collision.Other.Collider.GameObject;

		if ( otherObj.Tags.Has( "enemy" ) )
		{
			gameManager.PlayerTakeDamageFromEnemy( this, otherObj );
		}
	}

	public void TakeHit( float amount )
	{

		if ( !isInvulnerable && !isDead )
		{
			State.TakeDamage( amount );

			// Only update the UI if this is not a proxy
			//if ( !IsProxy && ui != null )

			ui?.SetHealth( State.Hp, State.MaxHp );

			if ( State.Hp <= 0 )
			{
				SetDead();

				if (Networking.IsHost )
				{
					if ( gameManager.AreAllPlayersDeadC() )
						gameManager.GameOver();
				}
				

				cancellation?.Cancel();
				cancellation?.Dispose();
				cancellation = null;		
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
			List<GunOutPut> gunModifiers = new();

			BulletInfo baseBullet = InitBaseBullet();

			bullets.Add( baseBullet );

			foreach ( ModifierType mod in State.ActiveModifiers )
			{
				if ( mod == ModifierType.Shotgun )
				{
					GunOutPut shotgunMod = new ShotgunOutPut();
					bool levelUp = false;

					foreach ( GunOutPut modifier in gunModifiers )
					{
						if ( modifier.modifierType == shotgunMod.modifierType )
						{
							modifier.AddLevel();
							levelUp = true;
							break;
						}
					}

					if ( !levelUp )
					{
						gunModifiers.Add( shotgunMod );
					}
				}
			}

			foreach ( GunOutPut mod in gunModifiers )
			{
				List<BulletInfo> newBullets = new();
				mod.OutPutBehaviour( baseBullet, newBullets );
				bullets = newBullets;
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

	[Rpc.Broadcast]
	public void SetDead()
	{
		Color colorTint = model.Tint;
		colorTint = Color.Blue;
		colorTint.a = 0.5f;

		model.Tint = colorTint;
		Tags.Add( "invulnerability" );

		canShoot = false;
		isDead = true;
	}
	[Rpc.Broadcast]
	public void Revive()
	{
		isDead = false;
		canShoot = true;
		isInvulnerable = false;
		Tags.Remove( "invulnerability" );

		Color colorTint = model.Tint;
		colorTint = Color.White;
		colorTint.a = 1f;
		model.Tint = colorTint;

		if ( Networking.IsHost )
		{
			State.Hp = State.MaxHp;
		}

		ui?.SetHealth( State.Hp, State.MaxHp );
	}
	[Rpc.Broadcast]
	public void SetInvulnerability( bool mode )
	{
		if ( !isDead )
		{
			Color colorTint = model.Tint;

			if ( mode )
			{
				colorTint.a = 0.5f;
				isInvulnerable = true;
				Tags.Add( "invulnerability" );
			}
			else
			{
				colorTint.a = 1f;
				isInvulnerable = false;
				Tags.Remove( "invulnerability" );
			}
			model.Tint = colorTint;
		}
	}

}
