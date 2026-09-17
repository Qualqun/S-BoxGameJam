using Sandbox;
using System.Threading.Tasks;
using static Sandbox.Services.Stats;
using static Sandbox.Sprite;

public class RangeEnemy : BaseEnemyBehaviour
{

	[Property, Group( "Stats" )] public float range { get; set; } = 100f;
	[Property, Group( "Stats" )] public float bulletSpeed { get; set; } = 180f;
	[Property, Group( "Stats" )] public float bulletDamage { get; set; } = 6f;
	[Property, Group( "Stats" )] public float aimTime { get; set; } = 0.4f;
	[Property, Group( "Stats" )] public float shootTime { get; set; } = 1.2f;
	[Property, Group( "Stats" )] public float reloadTime { get; set; } = 1.4f;

	[Property, Group( "Growth stats" )] public float bulletDamagePerRound { get; set; } = 6f;

	[Property, Group( "Refs" )] RangeEnemyAnimation animation { get; set; }

	[Property, Group( "Refs" )] public RangeVisualEnemy visual { get; set; }
	[Property, Group( "Refs" )] public EnemyBulletBehaviour bullet { get; set; }
	[Property, Group( "Refs" )] public GameObject gunPoint { get; set; }

	Task attackTask;

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		base.OnUpdate();


		if ( !noTarget )
		{
			bool canShoot = targetDist < range && attackTask == null;

			if ( canShoot )
			{
				SceneTraceResult hit = Scene.Trace
					.Sphere( bullet.size, gunPoint.WorldPosition, target.WorldPosition )
					.WithAnyTags( "enemy" ).Run();

				if ( hit.Hit && !hit.StartedSolid )
				{
					canShoot = hit.Collider.Tags.Has( "player" );
				}
			}

			animation.stand = canShoot || attackTask != null;

			if ( canShoot || attackTask != null )
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

		visual.UpdateLaser( gunPoint.WorldPosition, gunPoint.WorldPosition + direction * 10000f );

		await Task.DelaySeconds( shootTime / 4 * 3 );

		visual.StartBlink();

		await Task.DelaySeconds( shootTime / 4 );

		visual.ResetLaser();
		animation.Shoot();

		newBullet = bullet.GameObject.Clone( gunPoint.WorldPosition );
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

	public override void TakeDamage( float amount )
	{
		base.TakeDamage( amount );

		animation.OnHit();
	}

	public override void InitStats( int roundNb )
	{
		base.InitStats( roundNb );
		bulletDamage += bulletDamagePerRound * roundNb;
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Green;
		Gizmo.Draw.LineSphere( WorldPosition, range );
	}
}
