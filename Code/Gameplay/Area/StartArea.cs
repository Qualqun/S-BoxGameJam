public sealed class StartArea : Component
{
	[Property]
	public GameManager GameManager { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		var collider = Components.Get<BoxCollider>();

		collider.OnObjectTriggerEnter += OnObjectEnter;
		collider.OnObjectTriggerExit += OnObjectExit;
	}

	private void OnObjectEnter( GameObject other )
	{
		Log.Info( $"Entered zone: {other.Name}" );

		var player = other.GetComponent<PlayerBehaviour>();

		if ( player == null )
			return;

		Log.Info( "Player entered!" );

		GameManager?.PlayerEnteredStartArea( player );
	}

	private void OnObjectExit( GameObject other )
	{
		Log.Info( $"Exited zone: {other.Name}" );

		var player = other.GetComponent<PlayerBehaviour>();

		if ( player == null )
			return;

		GameManager?.PlayerLeftStartArea( player );
	}
}
