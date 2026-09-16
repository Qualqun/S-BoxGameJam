using Sandbox;



public sealed class RangeEnemyAnimation : BaseEnemyAnimation
{
	[Property] SkinnedModelRenderer model { get; set; }

	[Sync] public bool stand { get; set; } = false;
	[Sync] public bool shoot { get; set; } = false;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		model.Set( "Standing", stand );
		model.Set( "Shoot", shoot );
	}
}
