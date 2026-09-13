using Sandbox;

public sealed class MPlayerController : Component
{
	[Property, Group( "Refs" )] PlayerBehaviour playerBehaviour { get; set; } 
	[Property, Group( "Refs" )] CameraComponent camera { get; set; }
	[Property, Group( "Refs" )] GameObject visual { get; set; }
	[Property, Group( "Refs" )] Rigidbody rigidbody { get; set; }


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
		SceneTraceResult trace = Scene.Trace.Ray( ray.Position - gunPointOffSet, ray.Position + ray.Forward * 5000f - gunPointOffSet ).WithTag("ground").Run();

		Vector3 direction = trace.EndPosition - visual.WorldPosition;

		direction = direction.WithZ( 0f );

		visual.WorldRotation = Rotation.LookAt( direction );
	}

	void Inputs()
	{
		Vector3 velocity = new Vector3();
		float speed = playerBehaviour.runtimePlayerStat.moveSpeed;
	
		if ( Input.Down( "Forward" ) )
		{
			velocity += Vector3.Forward * speed;
		}

		if ( Input.Down( "Backward" ) )
		{
			velocity -= Vector3.Forward * speed;
		}

		if ( Input.Down( "Left" ) )
		{
			velocity -= Vector3.Right * speed;
		}

		if ( Input.Down( "Right" ) )
		{
			velocity += Vector3.Right * speed;
		}

		if(Input.Down( "Attack1" ) )
		{
			playerBehaviour.Fire();
		}

		rigidbody.Velocity = velocity;
	}
}
