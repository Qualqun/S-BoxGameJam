using Sandbox;

public class MeleeEnemy : BaseEnemyBehaviour
{
	[Property, Group( "Stats" )] float slowDuration { get; set; } = 1f;
	float timerSlow = 0;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if(!noTarget)
		{
			FollowPlayer();
		}
	}


}
