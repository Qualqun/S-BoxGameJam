using Sandbox;

public struct EnemyStats
{
	public float moveSpeed { get; set; }
	public bool isCac { get; set; }
	public float ballSpeed { get; set; }
}



public sealed class EnemyBehaviour : Component
{
	[Property, Group( "Refs" )] NavMeshAgent agent;

	public List<GameObject> Players;

	protected override void OnStart()
	{
		base.OnStart();
		if ( IsProxy )
		{
			Log.Info( "[EnemyBehaviour] Destroying EnemyBehaviour on proxy." );	
		}
		else
		{
			Log.Info( "[EnemyBehaviour] Destroying EnemyBehaviour on host." );
		}

	}

	protected override void OnUpdate()
	{
		
	}
}
