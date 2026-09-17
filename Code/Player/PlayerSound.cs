using Sandbox;

public sealed class PlayerSound : Component
{
	[Property, Group( "Sound" )] public SoundEvent shoot { get; set; }


	[Rpc.Broadcast]
	public void Shoot()
	{
		GameObject.PlaySound( shoot );
	}
}
