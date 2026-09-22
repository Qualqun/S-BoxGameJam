using Sandbox;

struct ItemPool
{
	public GameObject item { get; set; }
	public int baseAmount { get; set; }
	[Hide] public Stack<GameObject> itemStack { get; set; }

}


public class PoolManager : Component
{

	[Property] ItemPool bulletPool { get; set; }


	void AddBulletsInStack()
	{
		for ( int i = 0; i < bulletPool.baseAmount; i++ )
		{
			GameObject bullet = bulletPool.item.Clone();

			bullet.SetParent( GameObject );
			bullet.NetworkSpawn();

			bullet.Enabled = false;


			bulletPool.itemStack.Push( bullet );
		}
	}

	protected override void OnStart()
	{
		base.OnStart();

		AddBulletsInStack();
	}


	public GameObject GetBullet()
	{
		GameObject bullet;

		if ( bulletPool.itemStack.Count <= 0 )
		{
			AddBulletsInStack();
		}

		bullet = bulletPool.itemStack.Pop();
		bullet.Enabled = true;

		return bullet;
	}

	public void DisposeBullet( GameObject bulletObj )
	{
		bulletObj.Enabled = false;
		bulletPool.itemStack.Push( bulletObj );
	}
}
