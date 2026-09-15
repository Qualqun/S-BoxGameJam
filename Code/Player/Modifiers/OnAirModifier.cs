using Sandbox;

public class OnAirModifier
{
	public ModifierType modifierType;
	public int level = 1;

	public virtual Vector3 GetNewDirection( Vector3 baseDirection, BulletBehaviour bullet )
	{
		return baseDirection;
	}

	public virtual void AddLevel()
	{
		level++;
	}

	public virtual OnAirModifier Clone()
	{
		return new OnAirModifier();
	}
}

public class HomingShot : OnAirModifier
{

	public new ModifierType modifierType { get; set; } = ModifierType.HomingShot;
	public override Vector3 GetNewDirection( Vector3 baseDirection, BulletBehaviour bullet )
	{
		Vector3 newDirection = baseDirection;
		Scene scene = bullet.GameObject.Scene;
		List<SceneTraceResult> allResults =
			scene.Trace.Sphere( 100f, bullet.WorldPosition, bullet.WorldPosition + Vector3.Forward ).WithAllTags( "enemy" ).RunAll().ToList();


		if ( allResults != null && allResults.Count > 0 )
		{
			Vector3 directionToEnemy;
			Vector3 targetPos = Vector3.Zero;
			Vector3 bulletPos = bullet.WorldPosition;

			float targetDist = float.MaxValue;
			float powerHoming = level * Time.Delta;

			foreach ( SceneTraceResult result in allResults )
			{
				Vector3 enemyPos = result.Collider.WorldPosition;
				float distance = Vector3.DistanceBetween( enemyPos, bulletPos );

				if ( distance < targetDist )
				{
					targetPos = enemyPos;
					targetDist = distance;
				}
			}

			directionToEnemy = (targetPos - bulletPos).WithZ( 0 ).Normal;

			newDirection = Vector3.Lerp( baseDirection, directionToEnemy, powerHoming );
			newDirection = newDirection.WithZ( 0 ).Normal;
		}


		return newDirection.Normal;
	}
}

