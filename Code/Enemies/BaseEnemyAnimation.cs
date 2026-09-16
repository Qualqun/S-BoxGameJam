using Sandbox;



public class BaseEnemyAnimation : Component
{
	[Property] protected SkinnedModelRenderer model { get; set; }

	bool hit { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		model.Set( "EnemyHit", hit );

		hit = false;
	}

	[Rpc.Broadcast]
	public virtual void OnHit()
	{
		hit = true;
	}

}
