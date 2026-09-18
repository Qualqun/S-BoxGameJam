using Sandbox;
using System.Collections.Generic;

public enum BoostType
{
	Hp,
	Lives,
	MoveSpeed,
	FireRate,
	TimeInvulnerability,
	BulletDamage,
	BulletSpeed,
	Shotgun,
	Uzi,
	Sniper,
	Percing,
	Bounce,
	Explosion,
	HomingShot
}

public enum ModifierType
{
	Shotgun,
	Uzi,
	Sniper,
	Percing,
	Bounce,
	Explosion,
	HomingShot
}

public sealed class PlayerState : Component
{
	[Property, Sync] public float MaxHp { get; set; } = 100f;
	[Sync( SyncFlags.FromHost )] public float Hp { get; set; } = 100f;
	[Property, Sync] public int MaxLives { get; set; } = 5;
	[Sync( SyncFlags.FromHost )] public int Lives { get; set; } = 0;
	[Property, Sync] public int Experience { get; set; } = 0;
	[Property, Sync] public bool Dead { get; set; } = false;
	[Property, Sync] public int MaxExperience { get; set; } = 100;
	[Property, Sync] public int XpMultiplier { get; set; } = 3;
	[Property, Sync] public float MoveSpeed { get; set; } = 250f;
	[Property, Sync] public float TimeInvulnerability { get; set; } = 1f;
	[Property, Sync] public float BaseFireRate { get; set; } = 4f;
	[Property, Sync] public float BaseBulletDamage { get; set; } = 20f;
	[Property, Sync] public float BaseBulletSize { get; set; } = 1f;
	[Property, Sync] public float BaseBulletSpeed { get; set; } = 700f;
	[Property, Sync] public bool RewardTaken { get; set; } = false;
	[Property, Sync] public float FireRateMultiplier { get; set; } = 1f;
	[Property, Sync] public float DamageMultiplier { get; set; } = 1f;
	[Property, Sync] public float SizeMultiplier { get; set; } = 1f;
	[Property, Sync] public NetList<ModifierType> ActiveModifiers { get; set; } = new();
	public float FireRate => BaseFireRate * FireRateMultiplier;
	public float BulletDamage => BaseBulletDamage * DamageMultiplier;
	public float BulletSize => BaseBulletSize * SizeMultiplier;
	public float BulletSpeed => BaseBulletSpeed;

	#region Spawn snapshot

	[Description( "Values the prefab was spawned with, restored when the whole game is reset." )]
	bool spawnStatsCaptured;
	float spawnMaxHp;
	int spawnMaxLives;
	int spawnExperience;
	int spawnMaxExperience;
	int spawnXpMultiplier;
	float spawnMoveSpeed;
	float spawnTimeInvulnerability;
	float spawnBaseFireRate;
	float spawnBaseBulletDamage;
	float spawnBaseBulletSize;
	float spawnBaseBulletSpeed;

	[Description( "Max hp the prefab spawns with, readable from any machine." )]
	public float SpawnMaxHp => spawnMaxHp;

	protected override void OnAwake()
	{
		base.OnAwake();

		CaptureSpawnStats();
	}

	void CaptureSpawnStats()
	{
		if ( spawnStatsCaptured )
			return;

		spawnStatsCaptured = true;

		spawnMaxHp = MaxHp;
		spawnMaxLives = MaxLives;
		spawnExperience = Experience;
		spawnMaxExperience = MaxExperience;
		spawnXpMultiplier = XpMultiplier;
		spawnMoveSpeed = MoveSpeed;
		spawnTimeInvulnerability = TimeInvulnerability;
		spawnBaseFireRate = BaseFireRate;
		spawnBaseBulletDamage = BaseBulletDamage;
		spawnBaseBulletSize = BaseBulletSize;
		spawnBaseBulletSpeed = BaseBulletSpeed;
	}

	#endregion

	private void AddModifier( ModifierType modifier )
	{
		ActiveModifiers.Add( modifier );
	}

	public void TakeDamage( float amount )
	{
		Hp -= amount;
	}

	public void LoseLife()
	{
		Lives = System.Math.Max( 0, Lives - 1 );
	}

	public void AddExperience( int amount )
	{
		Experience += amount;

		if( Experience >= MaxExperience)
		{
			Experience -= MaxExperience;
			XpMultiplier += 1;
		}
	}

	[Rpc.Host]
	public void Host_LoseLife()
	{
		LoseLife();
	}

	[Rpc.Broadcast]
	public void Broadcast_LoseHalfLevels()
	{
		if ( IsProxy )
			return;

		XpMultiplier = System.Math.Max( 0, XpMultiplier / 2 );
	}

	[Rpc.Broadcast]
	public void Broadcast_ResetAll()
	{
		CaptureSpawnStats();

		if ( Networking.IsHost )
		{
			Hp = spawnMaxHp;
			Lives = spawnMaxLives;
		}

		if ( IsProxy )
			return;

		MaxHp = spawnMaxHp;
		MaxLives = spawnMaxLives;
		Experience = spawnExperience;
		MaxExperience = spawnMaxExperience;
		XpMultiplier = spawnXpMultiplier;
		MoveSpeed = spawnMoveSpeed;
		TimeInvulnerability = spawnTimeInvulnerability;

		BaseFireRate = spawnBaseFireRate;
		BaseBulletDamage = spawnBaseBulletDamage;
		BaseBulletSize = spawnBaseBulletSize;
		BaseBulletSpeed = spawnBaseBulletSpeed;

		FireRateMultiplier = 1f;
		DamageMultiplier = 1f;
		SizeMultiplier = 1f;

		Dead = false;
		RewardTaken = false;

		ActiveModifiers.Clear();
	}

	[Rpc.Broadcast]
	public void ResetRewardTaken()
	{
		if ( IsProxy )
			return;

		RewardTaken = false;
	}

	#region Authority Methods

	[Authority]
	public void Reset()
	{
		MaxHp = 100f;
		Hp = 100f;
		MaxLives = 5;
		Lives = 5;
		Experience = 0;
		MaxExperience = 100;
		MoveSpeed = 250f;
		TimeInvulnerability = 1f;
		Dead = false;

		BaseFireRate = 4f;
		BaseBulletDamage = 20f;
		BaseBulletSize = 1f;
		BaseBulletSpeed = 700f;

		FireRateMultiplier = 1f;
		DamageMultiplier = 1f;
		SizeMultiplier = 0.5f;
		XpMultiplier = 1;

		ActiveModifiers.Clear();
	}

	[Authority]
	public void ApplyBoost( BoostType boost )
	{
		XpMultiplier = System.Math.Max( 0, XpMultiplier - 1 );

		if ( XpMultiplier <= 0 )
			RewardTaken = true;

		switch ( boost )
		{
			case BoostType.Hp:
				MaxHp += 30f;
				Hp += 30f;
				break;
			case BoostType.Lives:
				MaxLives += 1;
				Lives += 1;
				break;
			case BoostType.MoveSpeed:
				MoveSpeed += 25f;
				break;
			case BoostType.FireRate:
				BaseFireRate += 1f;
				break;
			case BoostType.TimeInvulnerability:
				TimeInvulnerability += 1f;
				break;
			case BoostType.BulletDamage:
				BaseBulletDamage += 15f;
				break;
			case BoostType.BulletSpeed:
				BaseBulletSpeed += 150f;
				break;

			case BoostType.Shotgun:
				FireRateMultiplier *= 0.8f;
				DamageMultiplier *= 0.9f;
				AddModifier( ModifierType.Shotgun );
				break;
			case BoostType.Uzi:
				FireRateMultiplier *= 2f;
				DamageMultiplier *= 0.6f;
				AddModifier( ModifierType.Uzi );
				break;
			case BoostType.Sniper:
				FireRateMultiplier *= 0.6f;
				DamageMultiplier *= 2f;
				BaseBulletSpeed += 500f;
				AddModifier( ModifierType.Sniper );
				AddModifier( ModifierType.Percing );
				break;

			case BoostType.Percing:
				AddModifier( ModifierType.Percing );
				break;
			case BoostType.Bounce:
				AddModifier( ModifierType.Bounce );
				break;
			case BoostType.Explosion:
				AddModifier( ModifierType.Explosion );
				break;
			case BoostType.HomingShot:
				AddModifier( ModifierType.HomingShot );
				break;
		}
	}

	#endregion
}
