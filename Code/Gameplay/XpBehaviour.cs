using Sandbox;

public sealed class XpBehaviour : Component, Component.ITriggerListener
{
	[Property] float xpPerBall { get; set; } = 10f;

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
		{
			Destroy();
		}
	}

	public void OnTriggerEnter( Collider other )
	{
		Log.Info( "Collider info " + other.GameObject.Name );

		if ( !other.Tags.Has( "player" ) || IsProxy ) return;
	}

}
