using Sandbox;

public class MeleeEnemy : BaseEnemyBehaviour
{
	[Property, Group( "Stats" )] float slowDuration { get; set; } = 1f;
	[Property, Group( "Refs" )] BaseEnemyAnimation animation { get; set; }
	float timerSlow = 0;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if(!noTarget)
		{
			FollowPlayer();
		}
	}


	public override void TakeDamage( float amount )
	{
		base.TakeDamage( amount );

		animation.hit = true;
	}
}
