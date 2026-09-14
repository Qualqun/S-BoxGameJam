using Sandbox;
using System.IO.Compression;
using static Sandbox.Services.Stats;

public struct BulletInfo
{
	public float size { get; set; }
	public float speed { get; set; }
	public float damage { get; set; }
	public Vector3 direction { get; set; }
	public List<OnAirModifier> onAirBehaviours { get; set; }
	public List<EndModifier> endModifiers { get; set; }
}

public sealed class BulletBehaviour : Component
{
	public BulletInfo bulletInfo { get; set; }
	public GameManager gameManager { get; set; }
	public List<GameObject> enemyHit = new List<GameObject>();

	protected override void OnUpdate()
	{
		ModifiersBehaviour();
	}

	void ModifiersBehaviour()
	{

		SceneTraceResult traceResult;
		Vector3 nextStep;

		Vector3 direction = bulletInfo.direction;

		if ( bulletInfo.onAirBehaviours != null && bulletInfo.onAirBehaviours.Count > 0 )
		{

			foreach ( OnAirModifier modifier in bulletInfo.onAirBehaviours )
			{
				direction = modifier.GetNewDirection( direction, this );
			}

			SetNewDirection( direction );
		}

		nextStep = GetNextStep();
		traceResult = Scene.Trace.Sphere( 32f * WorldScale.x, WorldPosition, nextStep ).WithoutTags( "player", "bullet", "enemybullet" ).Run();

		if ( traceResult.Hit )
		{
			bool destroyBullet = true;
			bool updateNextStep = false;

			GameObject collideObj = traceResult.Collider.GameObject;

			if ( bulletInfo.endModifiers != null && bulletInfo.endModifiers.Count > 0 )
			{
				foreach ( EndModifier modifier in bulletInfo.endModifiers )
				{
					bool isDestroyBullet = modifier.EndBehaviour( traceResult, this, out bool isUpdateNextStep );

					if ( !isDestroyBullet )
						destroyBullet = false;

					if( isUpdateNextStep )
						updateNextStep = true;

				}

			}

			if ( traceResult.HasTag( "enemy" ) && !enemyHit.Contains( collideObj ) )
			{
				BaseEnemyBehaviour enemy = traceResult.Collider.GetComponent<BaseEnemyBehaviour>();

				if ( enemy != null )
				{
					enemyHit.Add( collideObj );
					gameManager.EnemyTakeDamage( enemy, bulletInfo.damage );
				}
			}

			if ( updateNextStep )
				nextStep = GetNextStep();

			if ( destroyBullet )
				GameObject.Destroy();
				return;
		}

		WorldPosition = nextStep;

	}

	Vector3 GetNextStep()
	{
		return WorldPosition + bulletInfo.direction * bulletInfo.speed * Time.Delta;
	}

	public void InitBall( BulletInfo baseinfo, GameManager manager )
	{
		BulletInfo newInfo = baseinfo;

		if ( baseinfo.onAirBehaviours != null && baseinfo.onAirBehaviours.Count > 0 )
		{
			newInfo.onAirBehaviours = new List<OnAirModifier>();

			foreach ( OnAirModifier modifier in baseinfo.onAirBehaviours )
			{
				newInfo.onAirBehaviours.Add( modifier.Clone() );
			}
		}

		if ( baseinfo.endModifiers != null && baseinfo.endModifiers.Count > 0 )
		{
			newInfo.endModifiers = new List<EndModifier>();

			foreach ( EndModifier modifier in baseinfo.endModifiers )
			{
				newInfo.endModifiers.Add( modifier.Clone() );
			}
		}

		bulletInfo = newInfo;
		gameManager = manager;

		WorldScale = bulletInfo.size;
	}

	public void SetNewDirection( Vector3 newDirection )
	{
		BulletInfo newInfo = bulletInfo;

		newInfo.direction = newDirection;

		bulletInfo = newInfo;
	}
}
