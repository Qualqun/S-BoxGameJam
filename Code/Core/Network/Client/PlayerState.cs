public sealed class PlayerState : Component
{
	[Sync( SyncFlags.FromHost )]
	public float MaxHp { get; set; }

	[Sync( SyncFlags.FromHost )]
	public float MoveSpeed { get; set; }

	[Sync( SyncFlags.FromHost )]
	public float FireRate { get; set; }

	[Sync( SyncFlags.FromHost )]
	public float BulletDamage { get; set; }

	[Sync( SyncFlags.FromHost )]
	public float BulletSize { get; set; }

	[Sync( SyncFlags.FromHost )]
	public float BulletSpeed { get; set; }

	[Sync( SyncFlags.FromHost )]
	public float TimeInvulnerability { get; set; }
}
