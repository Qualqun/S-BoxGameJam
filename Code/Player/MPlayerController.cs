using Sandbox;
using System.Threading.Tasks;

public sealed class MPlayerController : Component
{
	[Property, Group( "Stats" )] float dashDuration;
	[Property, Group( "Stats" )] float cooldown;
	[Property, Group( "Stats" )] float dashSpeed;


	[Property, Group( "Refs" )] PlayerBehaviour playerBehaviour { get; set; }
	[Property, Group( "Refs" )] CameraComponent camera { get; set; }
	[Property, Group( "Refs" )] GameObject visual { get; set; }
	[Property, Group( "Refs" )] Rigidbody rigidbody { get; set; }

	Task dashTask;
	bool dashEnd = true;

	protected override void OnStart()
	{
		base.OnStart();

		Mouse.Visibility = MouseVisibility.Visible;
		Mouse.CursorType = "crosshair";
	}

	protected override void OnUpdate()
	{
		PlayerRotation();
		Inputs();
	}

	void PlayerRotation()
	{
		Vector3 gunPointOffSet = Vector3.Up * playerBehaviour.gunPoint.WorldPosition.z;
		Ray ray = camera.ScreenPixelToRay( Mouse.Position );
		SceneTraceResult trace = Scene.Trace.Ray( ray.Position - gunPointOffSet, ray.Position + ray.Forward * 5000f - gunPointOffSet ).WithTag( "ground" ).Run();

		Vector3 direction = trace.EndPosition - visual.WorldPosition;

		direction = direction.WithZ( 0f );

		visual.WorldRotation = Rotation.LookAt( direction );
	}

	void Inputs()
	{
		Vector3 velocity = Vector3.Zero;
		float speed = playerBehaviour.runtimePlayerStat.moveSpeed;

		if ( Input.Down( "Forward" ) )
		{
			velocity += Vector3.Forward;
		}

		if ( Input.Down( "Backward" ) )
		{
			velocity -= Vector3.Forward;
		}

		if ( Input.Down( "Left" ) )
		{
			velocity -= Vector3.Right;
		}

		if ( Input.Down( "Right" ) )
		{
			velocity += Vector3.Right;
		}

		if ( Input.Down( "Attack1" ) )
		{
			playerBehaviour.Fire();
		}

		if ( Input.Down( "Jump" ) && velocity != Vector3.Zero && dashTask == null && !playerBehaviour.isDead )
		{
			dashTask = Dash( velocity );
		}

		if ( dashEnd )
		{
			rigidbody.Velocity = velocity * speed;
		}
	}


	async Task Dash( Vector3 velocity )
	{
		rigidbody.Velocity = velocity * dashSpeed;
		dashEnd = false;
		playerBehaviour.SetInvulnerability( true );

		await Task.DelaySeconds( dashDuration );

		rigidbody.Velocity = Vector3.Zero;
		dashEnd = true;
		playerBehaviour.SetInvulnerability( false );

		await Task.DelaySeconds( cooldown );

		dashTask = null;
	}
}
