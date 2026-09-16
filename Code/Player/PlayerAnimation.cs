using Sandbox;

public sealed class PlayerAnimation : Component
{
	[Property] SkinnedModelRenderer model {  get; set; }

	[Sync] public Vector2 move { get; set; }
	[Sync] public float speedShoot { get; set; } = 1f;
	[Sync] public bool shoot { get; set; }
	[Sync] public bool dash { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		model.Set( "move_x", move.x );
		model.Set( "move_y", move.y );
		model.Set( "ShootSpeed", speedShoot );

		model.Set( "Shoot", shoot );
		model.Set( "Dash", dash );

		shoot = false;
		dash = false;
	}
	
}
