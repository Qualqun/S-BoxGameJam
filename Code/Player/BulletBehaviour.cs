using Sandbox;
using static Sandbox.Services.Stats;

public sealed class BulletBehaviour : Component
{
	public float speed { get; set; }
	public float damage { get; set; }
	public GameManager gameManager { get; set; }

	public Vector3 direction { get; set; }

	protected override void OnUpdate()
	{
		LinearDirection();
	}

	void LinearDirection()
	{
		Vector3 nextStep = WorldPosition + direction * speed * Time.Delta;
		SceneTraceResult traceResult = Scene.Trace.Sphere( 32f * WorldScale.x, WorldPosition, nextStep ).WithoutTags( "player", "bullet", "enemybullet" ).Run();

		if ( traceResult.Hit )
		{
			if ( traceResult.HasTag( "Enemy" ) )
			{
				BaseEnemyBehaviour enemy = traceResult.Collider.GetComponent<BaseEnemyBehaviour>();

				if ( enemy != null )
				{
					gameManager.EnemyTakeDamage( enemy, damage );
				}
			}

			GameObject.Destroy();

			return;
		}

		WorldPosition = nextStep;
	}
}
