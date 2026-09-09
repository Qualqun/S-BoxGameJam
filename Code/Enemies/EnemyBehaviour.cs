using Sandbox;

public struct EnemyStats
{
	public float moveSpeed { get; set; }
	public bool isRange { get; set; }
	public float ballSpeed { get; set; }
}


public sealed class EnemyBehaviour : Component
{
	[Property, Group( "stats" )] public float hp { get; set; } = 100f;
	[Property, Group( "stats" )] public float damage { get; set; } = 25f;


	[Property, Group( "Refs" )] NavMeshAgent agent;
	[Property, Group( "Refs" )] float repathDistance { get; set; } = 64f;



	List<GameObject> players;
	GameObject target;
	float targetDist = float.MaxValue;
	bool isAttacking = false;

	protected override void OnStart()
	{
		base.OnStart();

		if(IsProxy)
		{
			Destroy();
		}
	}


	protected override void OnUpdate()
	{
		if ( players.Count > 0 )
		{
			for ( int i = 0; i < players.Count; i++ )
			{
				if ( players[i] == null )
				{
					players.RemoveAt( i );
					i--;
				}
				else
				{
					float dist = Vector3.DistanceBetween( WorldPosition, players[i].WorldPosition );

					if ( dist < targetDist )
					{
						target = players[i];
						targetDist = dist;
					}
				}
			}
		}
		else
		{
			//idle animations
		}

		if ( !isAttacking )
		{

			if ( agent.TargetPosition.HasValue )
			{
				float targetDistance = Vector3.DistanceBetween( agent.TargetPosition.Value, target.WorldPosition );

				if ( targetDistance > repathDistance )
				{
					agent.MoveTo( target.WorldPosition );
				}
			}
			else
			{
				agent.MoveTo( target.WorldPosition );
			}
		}

	}

	public void TakeDamage( float amount )
	{
		hp -= amount;

		if ( hp <= 0f )
		{
			GameObject.Destroy();
		}
	}

	public void SetPlayers( List<GameObject> allPlayers)
	{
		players = allPlayers;
	}

}
