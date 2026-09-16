using Sandbox;



public sealed class RangeEnemyAnimation : BaseEnemyAnimation
{
	[Sync] public bool stand { get; set; } = false;
	bool shoot = false;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		model.Set( "Standing", stand );
		model.Set( "Shoot", shoot );

		shoot = false;
	}

	public void Shoot()
	{
		shoot = true;
	}

	

}
