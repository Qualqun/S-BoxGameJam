using Sandbox;

public class OnAirModifier
{
	public virtual Vector3 GetNewDirection( Vector3 baseDirection )
	{
		return baseDirection;
	}

	public virtual bool EndBehaviour( SceneTraceResult traceResult, BulletBehaviour bullet)
	{
		return true;
	}

}


public class HomingShot : OnAirModifier
{
	public override Vector3 GetNewDirection( Vector3 baseDirection )
	{
		Vector3 newDirection = baseDirection + Vector3.Left;

		return newDirection.Normal;
	}
}
