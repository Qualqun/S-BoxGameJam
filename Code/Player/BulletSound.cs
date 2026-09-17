using Sandbox;

public sealed class BulletSound : Component
{
	[Property, Group( "Sound" )] public SoundEvent hit { get; set; }


	[Rpc.Broadcast]
	public void Hit()
	{
		Sound.Play( hit, WorldPosition );
	}
}
