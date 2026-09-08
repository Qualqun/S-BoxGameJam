using Sandbox;
using static Sandbox.Gizmo;

public sealed class PlayerBehaviour : Component
{
	[Property, Group( "Refs" )] MPlayerController controller { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
		{
			controller.Destroy();
		}
	}
}
