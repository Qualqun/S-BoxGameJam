using Sandbox;
using System.Threading.Tasks;
using static Sandbox.Services.Stats;

public class RangeEnemy : BaseEnemyBehaviour
{


	[Property, Group( "Stats" )] public float range { get; set; } = 100f;

	[Property, Group( "Stats" )] public float bulletSpeed { get; set; } = 100f;
	[Property, Group( "Stats" )] public float bulletDamage { get; set; } = 100f;
	[Property, Group( "Stats" )] public float aimTime { get; set; } = 1f;

	[Property, Group( "Stats" )] public float shootTime { get; set; } = 1f;
	[Property, Group( "Stats" )] public float reloadTime { get; set; } = 1f;

	[Property, Group( "Refs" )] public RangeVisualEnemy visual { get; set; }
	[Property, Group( "Refs" )] public GameObject bullet { get; set; }
	[Property, Group( "Refs" )] public GameObject gunPoint { get; set; }

	Task attackTask;

	protected override void OnUpdate()
	{
		if(IsProxy)
		{
			return;
		}

		base.OnUpdate();


		if ( !noTarget )
		{
			bool canShoot = targetDist < range || attackTask != null;

			if ( canShoot )
			{
				SceneTraceResult hit = Scene.Trace
					.Ray( gunPoint.WorldPosition, target.WorldPosition + Vector3.Up * 32 )
					.WithAnyTags( "enemy" ).Run();

				if ( hit.Hit )
				{
					canShoot = hit.Collider.Tags.Has( "player" );
				}

			}

			if ( canShoot )
			{
				agent.Stop();

				if ( attackTask == null )
				{
					attackTask = AttackBehaviour();
				}
			}
			else
			{
				FollowPlayer();
			}

		}
	}

	async Task AttackBehaviour()
	{

		Vector3 direction = target.WorldPosition - WorldPosition;

		GameObject newBullet;
		EnemyBulletBehaviour bulletBehaviour;

		direction = direction.WithZ( 0 );

		_ = AimRotate( direction, aimTime );

		await Task.DelaySeconds( aimTime );

		visual.UpdateLaser( gunPoint.WorldPosition, gunPoint.WorldPosition + direction  * 10000f);

		await Task.DelaySeconds( shootTime );

		visual.ResetLaser();

		newBullet = bullet.Clone( gunPoint.WorldPosition );
		bulletBehaviour = newBullet.GetComponent<EnemyBulletBehaviour>();

		

		bulletBehaviour.speed = bulletSpeed;
		bulletBehaviour.damage = bulletDamage;
		bulletBehaviour.direction = direction.Normal;
		bulletBehaviour.gameManager = gameManager;

		newBullet.NetworkSpawn();

		await Task.DelaySeconds( reloadTime );

		attackTask = null;
	}

	async Task AimRotate( Vector3 direction, float duration )
	{
		float time = 0f;

		Rotation baseRotation = WorldRotation;
		Rotation targetRotation = Rotation.LookAt( direction );

		while ( time < duration )
		{
			float amount;

			time += Time.Delta;
			amount = (time / duration).Clamp( 0f, 1f );

			WorldRotation = Rotation.Slerp( baseRotation, targetRotation, amount );

			await Task.Frame();
		}

		WorldRotation = targetRotation;

	}

	

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Green;
		Gizmo.Draw.LineSphere( WorldPosition, range );
	}
}
