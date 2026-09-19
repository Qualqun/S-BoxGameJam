using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Sandbox.Game;
using static Sandbox.Sprite;
using static Sandbox.Volumes.VolumeSystem;

public sealed class PlayerBehaviour : Component, Component.ICollisionListener
{
	[Sync( SyncFlags.FromHost )] public bool isInvulnerable { get; set; }
	[Sync( SyncFlags.FromHost )] public bool isDead { get; set; } = false;
	[Sync( SyncFlags.FromHost )] public bool isPermanentlyDead { get; set; } = false;
	[Sync( SyncFlags.FromHost )] public bool mustDevilPact { get; set; } = false;
	[Sync( SyncFlags.FromHost )] public bool isInStartZone { get; set; } = false;

	[Property, Group( "Refs" )] public PlayerAnimation animation { get; set; }
	[Property, Group( "Refs" )] public PlayerState State { get; set; }
	[Property, Group( "Refs" )] public GameObject gunPoint { get; set; }

	[Property, Group( "Refs" )] GameObject bullet { get; set; }
	[Property, Group( "Refs" )] GameObject cameraPivot { get; set; }

	[Property, Group( "Refs" )] public PlayerUI ui { get; set; }

	[Property, Group( "Refs" )] GameObject muzzleFlash { get; set; }

	[Property, Group( "Refs" )] MPlayerController controller { get; set; }
	[Property, Group( "Refs" )] PlayerSound sound { get; set; }
	[Property, Group( "Refs" )] ModelRenderer model { get; set; }

	[Property, Group( "Sound" )] public SoundFile mainMusic { get; set; }
	[Sync( SyncFlags.FromHost )] public GameManager gameManager { get; set; }

	bool canShoot = true;
	CancellationTokenSource cancellation;

	protected override void OnStart()
	{
		if ( State == null ) 
			State = Components.Get<PlayerState>();

		
		if ( IsProxy )
		{
			cameraPivot.Destroy();
			controller.Destroy();
			ui.Destroy();
		}
		else
		{
			ui.GameState = gameManager.GameState;
			ui?.SetHealth( State.Hp, State.MaxHp );
			ui?.ShowArrowToWorldPosition( gameManager.StartZonePoint.WorldPosition );

			Game.Music.Play( mainMusic, fade: 1.0f, loop: true, volume: 0.05f );
		}

		base.OnStart();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if( gameManager.GameState.State == GameStateType.Playing)
		{
			ui?.HideWorldArrow();
		}
	}

	BulletInfo InitBaseBullet()
	{
		Vector3 direction = Rotation.FromYaw( model.WorldRotation.Yaw()) * Vector3.Forward;
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
		if ( IsProxy ) 
			return; 

		GameObject otherObj = collision.Other.Collider.GameObject;

		if ( otherObj.Tags.Has( "enemy" ) )
			gameManager.PlayerTakeDamageFromEnemy( this, otherObj );
	}

	public void Server_TakeHit( float amount )
	{
		if ( !Networking.IsHost ) 
			return;

		if ( isDead || isInvulnerable ) 
			return;

		State.Hp = MathF.Max( 0f, State.Hp - amount );
		Broadcast_RefreshHealth( State.Hp, State.MaxHp );

		if ( State.Hp <= 0f )
		{
			Server_Die();
		}
		else
		{
			SetInvulnerability( true );
			_ = TimerHit();          
		}
	}

	[Rpc.Broadcast]
	public void Broadcast_RefreshHealth( float hp, float maxHp )
	{
		ui?.SetHealth( hp, maxHp );
	}

	public void Server_Die()
	{
		if ( !Networking.IsHost )
			return;

		if ( isDead )
			return;

		isDead = true;
		State.Hp = 0f;

		State.LoseLife();
		State.LoseHalfLevels();

		isPermanentlyDead = State.Lives <= 0;
		mustDevilPact = !isPermanentlyDead;

		cancellation?.Cancel();
		cancellation?.Dispose();
		cancellation = null;

		Broadcast_SetDead( isPermanentlyDead );
		Broadcast_RefreshHealth( State.Hp, State.MaxHp );

		gameManager?.Server_OnPlayerDied( this );
	}

	public void Server_Revive()
	{
		if ( !Networking.IsHost )
			return;

		if ( isPermanentlyDead )
			return;

		isDead = false;
		isInvulnerable = false;
		State.Hp = State.MaxHp;

		Broadcast_Revive( State.Hp, State.MaxHp );
	}

	public void Server_FullReset( Vector3 spawnPosition )
	{
		if ( !Networking.IsHost )
			return;

		isDead = false;
		isPermanentlyDead = false;
		mustDevilPact = false;
		isInvulnerable = false;
		isInStartZone = false;

		State.Broadcast_ResetAll();
		Broadcast_FullReset( spawnPosition );
	}

	[Rpc.Broadcast]
	public void Broadcast_SetDead( bool permanent )
	{
		SetDead( permanent ); 
	}

	public void Fire()
	{
		// canShoot is local, a pending fire rate timer would hand the gun back to a ghost
		if ( canShoot && !isDead )
		{
			List<BulletInfo> bullets = new();
			List<GunOutPut> gunModifiers = new();

			BulletInfo baseBullet = InitBaseBullet();
			GameObject muzleFlash = muzzleFlash.Clone( );

			muzleFlash.WorldPosition = gunPoint.WorldPosition;
			muzleFlash.LocalRotation = gunPoint.WorldRotation;
			muzleFlash.NetworkSpawn();

			animation.speedShoot = State.FireRate;
			animation.Shoot();
			sound.Shoot();

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

	public void SetDead( bool permanent = false )
	{
		Color colorTint = permanent ? Color.Black : Color.Blue;
		colorTint.a = permanent ? 0.3f : 0.5f;

		model.Tint = colorTint;
		Tags.Add( "invulnerability" );

		canShoot = false;
	}

	[Rpc.Broadcast]
	public void Broadcast_Revive( float hp, float maxHp )
	{
		canShoot = true;
		Tags.Remove( "invulnerability" );

		Color colorTint = Color.White;
		colorTint.a = 1f;
		model.Tint = colorTint;

		ui?.SetHealth( hp, maxHp );
	}

	[Rpc.Broadcast]
	public void Broadcast_FullReset( Vector3 spawnPosition )
	{
		canShoot = true;
		Tags.Remove( "invulnerability" );

		Color colorTint = Color.White;
		colorTint.a = 1f;
		model.Tint = colorTint;

		if ( !IsProxy )
		{
			WorldPosition = spawnPosition;

			Rigidbody body = Components.Get<Rigidbody>();

			if ( body != null )
			{
				body.Velocity = Vector3.Zero;
				body.AngularVelocity = Vector3.Zero;
			}

			ui?.SetHealth( State.SpawnMaxHp, State.SpawnMaxHp );

			if ( gameManager?.StartZonePoint != null )
				ui?.ShowArrowToWorldPosition( gameManager.StartZonePoint.WorldPosition );
		}
	}

	[Rpc.Broadcast]
	public void SetInvulnerability( bool mode )
	{
		if ( !isDead )
		{
			Color colorTint = model.Tint;

			if ( mode )
			{
				colorTint.a = 0.35f;
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
