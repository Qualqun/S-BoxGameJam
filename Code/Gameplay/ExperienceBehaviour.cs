using Sandbox;
using System;

public sealed class ExperienceBehaviour : Component, Component.ITriggerListener
{
	[Property] float speed { get; set; } = 20f;
	[Property] float levitateAmount { get; set; } = 20f;
	[Property] float levitateSpeed { get; set; } = 2f;

	public int AmountExperience { get; set; } = 1;
	public float offset { get; set; } = 32f;
	public Vector3 center { get; set; }
	public float angle { get; set; }

	float timerLevitate;

	public void OnTriggerEnter( Collider other )
	{
		PlayerBehaviour player;

		if ( !other.Tags.Has( "player" ) || IsProxy ) return;

		player = other.GetComponent<PlayerBehaviour>();

		player.State.AddExperience( AmountExperience );


		GameObject.Destroy();
	}

	protected override void OnStart()
	{
		base.OnStart();

		timerLevitate = Game.Random.Float( -1f, 1f );
	}


	protected override void OnUpdate()
	{
		base.OnUpdate();


		angle += speed * Time.Delta;
		timerLevitate += Time.Delta;

		WorldPosition = center + Rotation.FromYaw( angle ) * (Vector3.Forward * offset) + MathF.Sin( timerLevitate * levitateSpeed ) * levitateAmount * Vector3.Up;
	}



}
