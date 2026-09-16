using Sandbox;



public class BaseEnemyAnimation : Component
{
	[Property] SkinnedModelRenderer model { get; set; }

	[Sync] public bool hit { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		model.Set( "EnemyHit", hit );

		hit = false;
	}
}
