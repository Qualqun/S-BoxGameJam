using Sandbox;

public sealed class RangeVisualEnemy : BaseVisualEnemy
{
	[Property, Group( "Refs" )] LineRenderer laserInfo { get; set; }

	[Rpc.Broadcast]
	public void UpdateLaser( Vector3 start, Vector3 end )
	{
		laserInfo.VectorPoints = new List<Vector3>
		{
			start,
			end
		};
	}

	[Rpc.Broadcast]
	public void ResetLaser()
	{
		laserInfo.VectorPoints.Clear();
	}
}
