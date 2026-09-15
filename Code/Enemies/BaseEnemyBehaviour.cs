using Sandbox;
using System.Threading.Tasks;

public struct EnemyStats
{
	public float moveSpeed { get; set; }
	public bool isRange { get; set; }
	public float ballSpeed { get; set; }
}


public class BaseEnemyBehaviour : Component
{
	[Property, Group( "Stats" )] public float hp { get; set; } = 100f;
	[Property, Group( "Stats" )] public float meleDamage { get; set; } = 25f;

	[Property, Group( "Refs" )] protected NavMeshAgent agent { get; set; }
	[Property, Group( "Refs" )] protected BaseVisualEnemy model { get; set; }
	[Property, Group( "Refs" )] protected float repathDistance { get; set; } = 64f;


	protected List<PlayerBehaviour> players;
	public GameManager gameManager { get; set; }
	protected PlayerBehaviour target;
	protected float targetDist = float.MaxValue;
	protected bool noTarget = true;

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
		{
			Destroy();
		}
	}
	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		base.OnUpdate();

		if ( players.Count > 0 )
		{
			for ( int i = 0; i < players.Count; i++ )
			{
				if ( players[i] == null || players[i].isDead )
				{
					players.RemoveAt( i );
					i--;
				}
				else
				{
					float dist = Vector3.DistanceBetween( WorldPosition, players[i].WorldPosition );

					if ( dist < targetDist )
					{
						noTarget = false;
						target = players[i];
						targetDist = dist;
					}
				}
			}
		}
		else
		{
			noTarget = true;
		}

	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		gameManager.Enemies.Remove( GameObject );
	}

	protected void FollowPlayer()
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

	public void TakeDamage( float amount )
	{
		hp -= amount;

		model.TakeHit();

		if ( hp <= 0f )
		{
			GameObject.Destroy();
		}
	}

	public void SetPlayers( List<PlayerBehaviour> allPlayers )
	{
		players = allPlayers;
	}

}
