using Sandbox;

public class GunOutPut
{
	public virtual void OutPutBehaviour( BulletInfo baseInfo, List<BulletInfo> bullets ) { }
}

public class ShotgunOutPut : GunOutPut
{
	public override void OutPutBehaviour( BulletInfo baseInfo, List<BulletInfo> bullets )
	{
		BulletInfo Test = baseInfo;
		BulletInfo Test2 = baseInfo;

		Test2.direction = -Test.direction;

		bullets.Add( Test );
		bullets.Add( Test2 );
	}
}
