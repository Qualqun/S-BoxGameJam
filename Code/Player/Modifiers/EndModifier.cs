using Sandbox;

public class EndModifier
{
	public virtual void EndBehaviour( BulletBehaviour bullet ) { }
}

public class EndExplosion : EndModifier
{
	public override void EndBehaviour( BulletBehaviour bullet ) 
	{
		GameObject bulletObj = bullet.GameObject;

		GameObject newBullet = bulletObj.Clone( bulletObj.WorldPosition );
		BulletBehaviour bulletBehaviour = newBullet.GetComponent<BulletBehaviour>();

		BulletInfo bulletInfo = bullet.bulletInfo;

		bulletInfo.direction *= -1;
		bulletInfo.size /= 2f;

		bulletInfo.onAirBehaviours = null;
		bulletInfo.endModifiers = null;

		bulletBehaviour.InitBall( bulletInfo, bullet.gameManager );

		newBullet.NetworkSpawn();

	}
}
