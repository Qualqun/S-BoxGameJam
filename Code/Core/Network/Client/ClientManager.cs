using Sandbox;

public class ClientManager : Component, Component.INetworkListener
{
	[Property]
	public GameManager GameManager { get; set; }

	public bool AcceptConnection( Connection connection, ref string reason )
	{
		if ( Connection.All.Count >= 4 )
		{
			reason = "Game is full.";
			return false;
		}

		return true;
	}

	public void OnConnected( Connection connection )
	{
		Log.Info( $"Player connected: {connection.DisplayName}" );
	}

	public void OnActive( Connection connection )
	{
		if ( !Networking.IsHost )
			return;

		Log.Info( $"Player active: {connection.DisplayName}" );

		GameManager?.SpawnPlayer( connection );
	}

	public void OnDisconnected( Connection connection )
	{
		Log.Info( $"Player disconnected: {connection.DisplayName}" );
	}

	public void OnBecameHost( Connection previousHost )
	{
		Log.Info( "Became host." );
	}
}
