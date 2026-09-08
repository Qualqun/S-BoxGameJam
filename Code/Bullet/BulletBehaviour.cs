using Sandbox;

public sealed class BulletBehaviour: Component
{
	float speed;
	Vector3 direction;


	public void InitBall(Vector3 newDir, float newSpeed)
	{
		direction = newDir;
		speed = newSpeed;
	}

	protected override void OnUpdate()
	{


		LinearDirection();
	}

	void LinearDirection()
	{
		//Vector3
		//SceneTraceResult traceResult = Scene.Trace.Sphere( 32f * WorldScale.x, startPosition, endPosition ).Run();


		WorldPosition += direction * speed * Time.Delta;
	}
}
