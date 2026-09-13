using Sandbox;

public class GunOutPut
{
	public virtual void OutPutBehaviour( BulletInfo baseInfo, List<BulletInfo> bullets ) { }
}


public class ShotgunOutPut : GunOutPut
{
	public override void OutPutBehaviour( BulletInfo baseInfo, List<BulletInfo> bullets )
	{
		int nbBullets = 3;
		int degRange = 120;
		float degPerBullet = (float) degRange / nbBullets;

		BulletInfo bulletInfo = baseInfo;

		bulletInfo.direction = Rotation.FromYaw( -(degPerBullet * (nbBullets / 2 + 1)) ) * bulletInfo.direction;

		for ( int i = 0; i < nbBullets; i++ )
		{
			bulletInfo.direction = Rotation.FromYaw( degPerBullet ) * bulletInfo.direction;
			bullets.Add( bulletInfo );
		}

	}
}
