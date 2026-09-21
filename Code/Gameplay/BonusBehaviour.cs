using Sandbox;
using System;
using System.Diagnostics.Metrics;


public enum BonusType
{
	XP,
	LVL_UP,
	LIFE_UP,
}

public struct BonusInfo
{
	public BonusType type { get; set; }
	[Range( 0, 100 )] public int percent { get; set; }
	public Material matAssociate { get; set; }


}

public sealed class BonusBehaviour : Component, Component.ITriggerListener
{
	[Property] float speed { get; set; } = 20f;
	[Property] float levitateAmount { get; set; } = 20f;
	[Property] float levitateSpeed { get; set; } = 2f;
	[Property] BonusInfo[] bonusRate { get; set; }

	[Property, Group( "Refs" )] ModelRenderer modelRenderer { get; set; }

	BonusInfo actualBonus { get; set; }

	public int amountExperience { get; set; } = 1;
	public float offset { get; set; } = 32f;
	public Vector3 center { get; set; }
	public float angle { get; set; }

	float timerLevitate;

	protected override void OnStart()
	{
		base.OnStart();

		timerLevitate = Game.Random.Float( -1f, 1f );

		if ( IsProxy )
		{
			Destroy();
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		angle += speed * Time.Delta;
		timerLevitate += Time.Delta;

		WorldPosition = center + Rotation.FromYaw( angle ) * (Vector3.Forward * offset) + MathF.Sin( timerLevitate * levitateSpeed ) * levitateAmount * Vector3.Up;
	}

	public void OnTriggerEnter( Collider other )
	{
		PlayerBehaviour player;

		if ( !other.Tags.Has( "player" ) ) return;

		player = other.GetComponent<PlayerBehaviour>();

		if ( !player.isDead )
		{
			Connection owner = player.GameObject.Network.Owner;

			if ( actualBonus.type != BonusType.LIFE_UP )
			{
				using ( Rpc.FilterInclude( c => c == owner ) )
				{
					player.Broadcast_AddExperience( actualBonus.type == BonusType.XP ? amountExperience : 100 );
				}
			}
			else
			{
				player.State.WinLife();

				using ( Rpc.FilterInclude( c => c == owner ) )
				{
					player.Broadcast_ShowLifeUP( );
				}
			}




			GameObject.Destroy();
		}
	}

	public void InitBonus()
	{
		int random = Game.Random.Int( 0, 100 );

		foreach ( BonusInfo bonus in bonusRate )
		{
			if ( random <= bonus.percent )
			{
				actualBonus = bonus;

				modelRenderer.SetMaterial( bonus.matAssociate );

				if ( bonus.type == BonusType.LVL_UP )
					Log.Info( "lelv up" );
			}
			else
			{
				random -= bonus.percent;
			}
		}

	}

}
