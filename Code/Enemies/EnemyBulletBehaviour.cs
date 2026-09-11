using Sandbox;
using static Sandbox.Services.Stats;

public sealed class EnemyBulletBehaviour : Component
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
		SceneTraceResult traceResult = Scene.Trace.Sphere( 32f * WorldScale.x, WorldPosition, nextStep ).WithoutTags( "enemy", "enemybullet", "bullet" ).Run();

		if ( traceResult.Hit )
		{
			if ( traceResult.HasTag( "player" ) )
			{
				PlayerBehaviour player = traceResult.Collider.GetComponent<PlayerBehaviour>();

				if ( player != null )
				{
					Log.Info( "Hittt" );

					using ( Rpc.FilterInclude( c => c == player.GameObject.Network.Owner ) )
					{
						Log.Info( "Hittt" );
						gameManager.PlayerTakeDamage( player, damage );
					}
				}
			}

			GameObject.Destroy();

			return;
		}

		WorldPosition = nextStep;
	}
}
