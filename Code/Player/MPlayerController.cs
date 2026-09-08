using Sandbox;

public sealed class MPlayerController : Component
{
	[Header( "Stats" )]
	[Property] float speed { get; set; } = 100000f;

	[Property, Group( "Refs" )] CameraComponent camera { get; set; }
	[Property, Group( "Refs" )] GameObject visual { get; set; }
	[Property, Group( "Refs" )] GameObject pivotCamera { get; set; }
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
		Ray ray = camera.ScreenPixelToRay( Mouse.Position );
		SceneTraceResult trace = Scene.Trace.Ray( ray.Position, ray.Position + ray.Forward * 5000f ).WithTag("ground").Run();

		Vector3 direction;

		if ( !trace.Hit )
			return;

		direction = trace.EndPosition - visual.WorldPosition;
		direction = direction.WithZ( 0f );

		visual.WorldRotation = Rotation.LookAt( direction );
	}

	void Inputs()
	{
		Vector3 velocity = new Vector3();

		if ( Input.Down( "Forward" ) )
		{
			velocity += Vector3.Forward * speed * Time.Delta;
		}

		if ( Input.Down( "Backward" ) )
		{
			velocity -= Vector3.Forward * speed * Time.Delta;
		}

		if ( Input.Down( "Left" ) )
		{
			velocity -= Vector3.Right * speed * Time.Delta;
		}

		if ( Input.Down( "Right" ) )
		{
			velocity += Vector3.Right * speed * Time.Delta;
		}

		rigidbody.Velocity = velocity;
	}
}
