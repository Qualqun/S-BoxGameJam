using Sandbox;

public sealed class RotateMap : Component
{
	[Property] Vector3 rotate { get; set; }
	[Property] float speed { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		WorldRotation *= Rotation.FromAxis( rotate, speed * Time.Delta );
	}

}
