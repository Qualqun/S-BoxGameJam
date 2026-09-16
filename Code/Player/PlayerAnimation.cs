using Sandbox;

public sealed class PlayerAnimation : Component
{
	[Property] SkinnedModelRenderer model {  get; set; }

	[Sync] public Vector2 move { get; set; }
	[Sync] public float speedShoot { get; set; } = 1f;

	bool shootAnim = false;
	bool dash = false;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		model.Set( "move_x", move.x );
		model.Set( "move_y", move.y );
		model.Set( "ShootSpeed", speedShoot );

		model.Set( "Shoot", shootAnim );
		model.Set( "Dash", dash );

		shootAnim = false;
		dash = false;
	}

	[Rpc.Broadcast]
	public void Shoot()
	{
		shootAnim = true;
	}

	[Rpc.Broadcast]
	public void Dash()
	{
		dash = true;
	}

}
