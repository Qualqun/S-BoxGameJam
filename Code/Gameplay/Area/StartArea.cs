public sealed class StartArea : Component
{
	[Property]
	public GameManager GameManager { get; set; }
	[Property] ModelRenderer platform { get; set; }
	[Property] List<Model> platforms { get; set; }
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

		platform.Model = platforms[1];
		GameManager?.PlayerEnteredStartArea( player );
	}

	private void OnObjectExit( GameObject other )
	{
		Log.Info( $"Exited zone: {other.Name} " + GameManager.GameState.State.ToString() );

		var player = other.GetComponent<PlayerBehaviour>();


		
		if ( player == null )
			return;

		if ( GameManager.GameState.State == GameStateType.Starting )
		{
			platform.Model = platforms[0];
		}


		GameManager?.PlayerLeftStartArea( player );
	}
}
