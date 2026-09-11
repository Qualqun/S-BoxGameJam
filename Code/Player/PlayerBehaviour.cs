using Sandbox;
using System.Threading;
using System.Threading.Tasks;
public struct PlayerStats
{
	public float hp { get; set; }
	public float moveSpeed { get; set; }
	public float fireRate { get; set; }
	public float ballDamage { get; set; }
	public float ballSpeed { get; set; }
	public float timeInvulnerability { get; set; }
}

public sealed class PlayerBehaviour : Component, Component.ICollisionListener
{
	[Sync] public bool isInvulnerable { get; set; }
	[Sync] public bool isDead { get; set; } = false;

	[Header( "Stats" )]
	[Property] public PlayerStats playerStats { get; set; }

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
		base.OnStart();

		hp = playerStats.hp;

		if ( IsProxy )
		{
			controller.Destroy();
		}
	}

	async Task StartTimer( CancellationToken token )
	{
		canShoot = false;

		await Task.DelaySeconds( 1f / playerStats.fireRate );

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

		await Task.DelaySeconds( playerStats.timeInvulnerability );

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

				gameManager.GameState.Server_SetGameState(GameStateType.GameOver );
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
			GameObject newBullet = bullet.Clone( gunPoint.WorldPosition );
			BulletBehaviour bulletBehaviour = newBullet.GetComponent<BulletBehaviour>();

			Vector3 direction = gunPoint.WorldPosition - WorldPosition;

			direction = direction.WithZ( 0 );

			bulletBehaviour.speed = playerStats.ballSpeed;
			bulletBehaviour.damage = playerStats.ballDamage;
			bulletBehaviour.direction = direction.Normal;
			bulletBehaviour.gameManager = gameManager;

			newBullet.NetworkSpawn();


			cancellation = new CancellationTokenSource();
			_ = StartTimer( cancellation.Token );
		}
	}

}
