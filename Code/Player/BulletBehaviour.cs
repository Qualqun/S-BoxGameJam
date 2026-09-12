using Sandbox;
using static Sandbox.Services.Stats;

public struct BulletInfo
{
	public float size { get; set; }
	public float speed { get; set; }
	public float damage { get; set; }
	[Hide] public Vector3 direction { get; set; }
	[Hide] public List<OnAirModifier> onAirBehaviours { get; set; }
	[Hide] public List<EndModifier> endModifiers { get; set; }
}

public sealed class BulletBehaviour : Component
{
	public BulletInfo bulletInfo { get; set; }
	public GameManager gameManager { get; set; }


	public void InitBall( BulletInfo newInfo, GameManager manager )
	{
		bulletInfo = newInfo;
		gameManager = manager;

		WorldScale = bulletInfo.size;
	}

	protected override void OnUpdate()
	{
		ModifiersBehaviour();
	}

	void SetNewDirection( Vector3 newDirection )
	{
		BulletInfo newInfo = bulletInfo;

		newInfo.direction = newDirection;

		bulletInfo = newInfo;
	}

	void ModifiersBehaviour()
	{
		SceneTraceResult traceResult;
		Vector3 nextStep;

		Vector3 direction = bulletInfo.direction;

		if ( bulletInfo.onAirBehaviours != null && bulletInfo.onAirBehaviours.Count > 0 )
		{
			Log.Info( "On air " );

			foreach ( OnAirModifier modifier in bulletInfo.onAirBehaviours )
			{
				direction = modifier.GetNewDirection( direction );
			}

			SetNewDirection( direction );
		}

		nextStep = WorldPosition + bulletInfo.direction * bulletInfo.speed * Time.Delta;
		traceResult = Scene.Trace.Sphere( 32f * WorldScale.x, WorldPosition, nextStep ).WithoutTags( "player", "bullet", "enemybullet" ).Run();


		if ( traceResult.Hit )
		{
			bool destroyBullet = true;

			if ( bulletInfo.onAirBehaviours != null && bulletInfo.onAirBehaviours.Count > 0 )
			{
				foreach ( OnAirModifier modifier in bulletInfo.onAirBehaviours )
				{
					if ( !modifier.EndBehaviour( traceResult, this ) )
					{
						destroyBullet = false;
					}
				}
			}

			if ( bulletInfo.endModifiers != null && bulletInfo.endModifiers.Count > 0 )
			{
				foreach ( EndModifier modifier in bulletInfo.endModifiers )
				{
					modifier.EndBehaviour( this );
				}
			}

			if ( traceResult.HasTag( "Enemy" ) )
			{
				BaseEnemyBehaviour enemy = traceResult.Collider.GetComponent<BaseEnemyBehaviour>();

				if ( enemy != null )
				{
					gameManager.EnemyTakeDamage( enemy, bulletInfo.damage );
				}
			}

			if ( destroyBullet )
			{
				GameObject.Destroy();
				return;
			}

		}

		WorldPosition = nextStep;

	}


}
