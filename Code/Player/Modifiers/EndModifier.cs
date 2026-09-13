using Sandbox;

public class EndModifier
{
	public int level = 1;

	public virtual bool EndBehaviour( SceneTraceResult traceResult, BulletBehaviour bullet )
	{
		return true;
	}

	public virtual EndModifier Clone()
	{
		return new EndModifier();
	}
}

public class Bounce : EndModifier
{
	int nbBounce = 1;

	public override bool EndBehaviour( SceneTraceResult traceResult, BulletBehaviour bullet )
	{
		if ( !traceResult.HasTag( "enemy" ) && nbBounce > 0 )
		{
			Vector3 newDir = Vector3.Reflect( bullet.bulletInfo.direction, traceResult.HitPosition.Normal );

			newDir = newDir.WithZ( 0 );

			bullet.SetNewDirection( newDir );

			nbBounce--;

			return false;
		}

		return true;
	}


	public override EndModifier Clone()
	{
		Bounce bounceClone = new Bounce();

		bounceClone.level = level;
		bounceClone.nbBounce = level;

		return bounceClone;
	}

}

public class Percing : EndModifier
{
	int nbPercing = 1;
	List<GameObject> enemyEncountered = new List<GameObject>();


	public override bool EndBehaviour( SceneTraceResult traceResult, BulletBehaviour bullet )
	{
		GameObject collisionObj = traceResult.Collider.GameObject;
		bool alreadyEncountered = enemyEncountered.Contains( collisionObj );

		if ( traceResult.HasTag( "enemy" ) )
		{
			enemyEncountered.Add( collisionObj );

			if( !alreadyEncountered )
			{
				nbPercing--;

				return nbPercing < 0;
			}

			return false;
		}

		return true;
	}


	public override EndModifier Clone()
	{
		Percing bounceClone = new Percing();

		bounceClone.level = level;
		bounceClone.nbPercing = level;

		return bounceClone;
	}

}



//public class EndExplosion : EndModifier
//{
//	public override void EndBehaviour( BulletBehaviour bullet ) 
//	{
//		GameObject bulletObj = bullet.GameObject;

//		GameObject newBullet = bulletObj.Clone( bulletObj.WorldPosition );
//		BulletBehaviour bulletBehaviour = newBullet.GetComponent<BulletBehaviour>();

//		BulletInfo bulletInfo = bullet.bulletInfo;

//		bulletInfo.direction *= -1;
//		bulletInfo.size /= 2f;

//		bulletInfo.onAirBehaviours = null;
//		bulletInfo.endModifiers = null;

//		bulletBehaviour.InitBall( bulletInfo, bullet.gameManager );

//		newBullet.NetworkSpawn();

//	}
//}
