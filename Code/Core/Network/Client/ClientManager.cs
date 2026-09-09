using Sandbox;

public class ClientManager : Component, Component.INetworkListener
{
	[Property]
	public GameManager GameManager { get; set; }

	public bool AcceptConnection( Connection connection, ref string reason )
	{
		// Fake lobby, we must change the lobby behaviour (invite ?, UI ???????)
		if ( Connection.All.Count >= 4 )
		{
			reason = "[ClientManager] Game is full.";
			return false;
		}

		return true;
	}

	// When the connection is established, but the client is not ready yet
	public void OnConnected( Connection connection )
	{
		Log.Info( $"[ClientManager] New player connected: {connection.DisplayName}" );
	}

	// When the connection is fully established and the client is ready 
	public void OnActive( Connection connection )
	{
		if ( !Networking.IsHost )
			return;

		Log.Info( $"[ClientManager] New player active: {connection.DisplayName} | Is local : {connection == Connection.Local} | Is host {connection == Connection.Host}" );

		GameManager?.SpawnPlayer( connection );

	}

	// When a player is disconnected
	public void OnDisconnected( Connection connection )
	{
		Log.Info( $"[ClientManager] Player disconnected: {connection.DisplayName}" );
	}

	// When a client became a host (in case it was a simple client)
	public void OnBecameHost( Connection previousHost )
	{
		Log.Info( "[ClientManager] Became host." );
	}
}
