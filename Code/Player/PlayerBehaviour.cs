using Sandbox;
using System.Threading.Tasks;
public struct PlayerStats
{
	public float moveSpeed { get; set; }
	public float fireRate { get; set; }
	public float ballSpeed { get; set; }
}


public sealed class PlayerBehaviour : Component
{
	[Header( "Stats" )]
	[Property] public PlayerStats playerStats { get; set; }

	[Property, Group( "Refs" )] GameObject gunPoint { get; set; }
	[Property, Group( "Refs" )] GameObject bullet { get; set; }
	[Property, Group( "Refs" )] MPlayerController controller { get; set; }

	bool canShoot = true;

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
		{
			controller.Destroy();
		}
	}

	public void Fire()
	{
		if ( canShoot )
		{
			GameObject newBullet = bullet.Clone( gunPoint.WorldPosition );
			BulletBehaviour behaviour = newBullet.GetComponent<BulletBehaviour>();

			Vector3 direction = gunPoint.WorldPosition - WorldPosition;

			direction = direction.WithZ( 0 );

			behaviour.InitBall( direction.Normal, playerStats.ballSpeed );

			_ = StartTimer();
		}
	}

	async Task StartTimer()
	{
		canShoot = false;

		await Task.DelaySeconds( playerStats.fireRate );

		canShoot = true;
	}

}
