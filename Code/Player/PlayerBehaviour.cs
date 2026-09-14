using Sandbox;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public sealed class PlayerBehaviour : Component, Component.ICollisionListener
{
	[Sync] public bool isInvulnerable { get; set; }
	[Sync] public bool isDead { get; set; } = false;
	[Property, Group( "Refs" )] public PlayerState State { get; set; }
	[Property, Group( "Refs" )] public GameObject gunPoint { get; set; }
	[Property, Group( "Refs" )] PlayerUI ui { get; set; }
	[Property, Group( "Refs" )] GameObject bullet { get; set; }
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
			onAirBehaviours = new List<OnAirModifier>(),
			endModifiers = new List<EndModifier>()
		};

		foreach ( var mod in State.ActiveModifiers )
		{
			if ( mod == ModifierType.HomingShot ) 
				newBulletInfo.onAirBehaviours.Add( new HomingShot() );

			if ( mod == ModifierType.Bounce ) 
				newBulletInfo.endModifiers.Add( new Bounce() );

			if ( mod == ModifierType.Percing ) 
				newBulletInfo.endModifiers.Add( new Percing() );

			if ( mod == ModifierType.Explosion ) 
				newBulletInfo.endModifiers.Add( new EndExplosion() );
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
		if ( !Networking.IsHost )
			return;

		Collider other = collision.Other.Collider;

		if ( other.Tags.Has( "enemy" ) )
		{
			BaseEnemyBehaviour enemy = other.GameObject.GetComponent<BaseEnemyBehaviour>();

			gameManager.PlayerTakeDamage( this, enemy.damage );
		}
	}

	public void TakeHit( float amount )
	{
		if ( !isInvulnerable && !isDead )
		{
			State.TakeDamage( amount );

			// Only update the UI if this is not a proxy
			if ( !IsProxy && ui != null )
				ui.SetHealth( State.Hp, State.MaxHp );

			if ( State.Hp <= 0 )
			{
				// Only the host should handle the life decrement
				if ( Networking.IsHost )
					State.Life--;

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

				if ( State.Life > 0 )
					_ = ReviveRoutine();
				else
				{
					if ( Networking.IsHost )
						gameManager.CheckPlayersState();
				}
			}
			else
				_ = TimerHit();
		}
	}

	async Task ReviveRoutine()
	{
		await Task.DelaySeconds( 3f );

		State.Hp = State.MaxHp;

		// Only update the UI if this is not a proxy 
		if ( !IsProxy && ui != null )
		{
			ui.SetHealth( State.Hp, State.MaxHp );
		}

		Color colorTint = model.Tint;
		colorTint = Color.White;
		colorTint.a = 1f;
		model.Tint = colorTint;

		Tags.Remove( "invulnerability" );
		isDead = false;
		canShoot = true;

		_ = TimerHit();
	}

	public void Fire()
	{
		if ( canShoot )
		{
			List<BulletInfo> bullets = new();
			BulletInfo baseBullet = InitBaseBullet();
			bullets.Add( baseBullet );

			foreach ( var mod in State.ActiveModifiers )
			{
				if ( mod == ModifierType.Shotgun )
				{
					List<BulletInfo> newBullets = new();
					new ShotgunOutPut().OutPutBehaviour( baseBullet, newBullets );
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

	[Rpc.Owner]
	public void ResetPlayer()
	{
		Color colorTint = model.Tint;
		colorTint = Color.White;
		colorTint.a = 1f;
		model.Tint = colorTint;

		Tags.Remove( "invulnerability" );
		State.Reset();
		ui.SetHealth( State.Hp, State.MaxHp );
		canShoot = true;
		isDead = false;
	}
}
