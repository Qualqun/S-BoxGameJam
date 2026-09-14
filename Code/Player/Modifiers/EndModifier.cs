using Sandbox;

public class EndModifier
{
	public int level = 1;

	public virtual bool EndBehaviour( SceneTraceResult traceResult, BulletBehaviour bullet, out bool updateNextStep )
	{
		updateNextStep = false;
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

	public override bool EndBehaviour( SceneTraceResult traceResult, BulletBehaviour bullet, out bool updateNextStep )
	{
		if ( !traceResult.HasTag( "enemy" ) && nbBounce > 0 )
		{
			Vector3 newDir = Vector3.Reflect( bullet.bulletInfo.direction, traceResult.Normal );

			newDir = newDir.WithZ( 0 );

			bullet.SetNewDirection( newDir );

			nbBounce--;

			updateNextStep = true;

			return false;
		}

		updateNextStep = false;
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


	public override bool EndBehaviour( SceneTraceResult traceResult, BulletBehaviour bullet, out bool updateNextStep )
	{
		GameObject collisionObj = traceResult.Collider.GameObject;
		bool alreadyEncountered = enemyEncountered.Contains( collisionObj );

		if ( traceResult.HasTag( "enemy" ) )
		{
			enemyEncountered.Add( collisionObj );
			updateNextStep = true;


			if ( !alreadyEncountered )
			{
				nbPercing--;

				return nbPercing < 0;
			}

			return false;
		}

		updateNextStep = false;
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

public class EndExplosion : EndModifier
{
	public override bool EndBehaviour( SceneTraceResult traceResult, BulletBehaviour bullet, out bool updateNextStep )
	{
		GameObject bulletObj = bullet.GameObject;
		BulletInfo bulletInfo = bullet.bulletInfo;
		int nbBullets = 8;
		int anglePerBullet = 360 / nbBullets;


		bulletInfo.endModifiers = null;
		bulletInfo.size /= 2f;
		bulletInfo.direction = Vector3.Forward;

		for ( int i = 0; i < nbBullets; i++ )
		{
			GameObject newBullet = bulletObj.Clone( bulletObj.WorldPosition );
			BulletBehaviour bulletBehaviour = newBullet.GetComponent<BulletBehaviour>();

			bulletBehaviour.InitBall( bulletInfo, bullet.gameManager );
			bulletInfo.direction = Rotation.FromYaw( anglePerBullet ) * bulletInfo.direction;
			newBullet.NetworkSpawn();
		}
		updateNextStep = false;
		return true;
	}

	public override EndExplosion Clone()
	{
		return new EndExplosion();
	}
}
