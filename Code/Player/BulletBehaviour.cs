using Sandbox;
using System.IO.Compression;
using static Sandbox.Services.Stats;

public struct BulletInfo
{
	public float size { get; set; }
	public float speed { get; set; }
	public float damage { get; set; }
	public Vector3 direction { get; set; }
	public List<OnAirModifier> onAirModifier { get; set; }
	public List<EndModifier> endModifiers { get; set; }

	public void AddAirModifier( OnAirModifier modifier )
	{
		if ( onAirModifier == null )
		{
			onAirModifier = new List<OnAirModifier>();
		}

		foreach ( OnAirModifier mod in onAirModifier )
		{
			if ( mod.modifierType == modifier.modifierType )
			{
				mod.AddLevel();
				return;
			}
		}

		onAirModifier.Add( modifier );
	}

	public void AddEndModifiers( EndModifier modifier )
	{
		if ( endModifiers == null )
		{
			endModifiers = new List<EndModifier>();
		}

		foreach ( EndModifier mod in endModifiers )
		{

			if ( mod.modifierType == modifier.modifierType )
			{
				mod.AddLevel();
				return;
			}
		}

		endModifiers.Add( modifier );
	}


}

public sealed class BulletBehaviour : Component
{
	[Property, WideMode] TagSet noCollideTag { get; set; }
	[Property] float size { get; set; } = 32f;
	[Property] BulletSound sound { get; set; }


	public BulletInfo bulletInfo { get; set; }
	public GameManager gameManager { get; set; }
	public List<GameObject> enemyHit = new List<GameObject>();

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
		ModifiersBehaviour();
	}

	void ModifiersBehaviour()
	{
		SceneTraceResult traceResult;
		Vector3 nextStep;

		Vector3 direction = bulletInfo.direction;

		if ( bulletInfo.onAirModifier != null && bulletInfo.onAirModifier.Count > 0 )
		{
			foreach ( OnAirModifier modifier in bulletInfo.onAirModifier )
			{
				direction = modifier.GetNewDirection( direction, this );
			}

			SetNewDirection( direction );
		}

		nextStep = GetNextStep();
		traceResult = Scene.Trace.Sphere( 32f * WorldScale.x, WorldPosition, nextStep ).WithoutTags( noCollideTag ).Run();

		if ( traceResult.Hit )
		{
			bool destroyBullet = true;
			bool updateNextStep = false;

			GameObject collideObj = traceResult.Collider.GameObject;

			if ( bulletInfo.endModifiers != null && bulletInfo.endModifiers.Count > 0 )
			{
				foreach ( EndModifier modifier in bulletInfo.endModifiers )
				{
					bool isDestroyBullet = modifier.EndBehaviour( traceResult, this, out bool isUpdateNextStep );

					if ( !isDestroyBullet )
						destroyBullet = false;

					if ( isUpdateNextStep )
						updateNextStep = true;

				}
			}

			if ( traceResult.HasTag( "enemy" ) && !enemyHit.Contains( collideObj ) )
			{
				enemyHit.Add( collideObj );
				gameManager.EnemyTakeDamage( collideObj, bulletInfo.damage );
				gameManager.UiManager.HitNumbers.ShowNumber( bulletInfo.damage, traceResult.HitPosition );
			}

			if ( updateNextStep )
			{
				nextStep = GetNextStep();
			}

			if ( destroyBullet )
			{
				GameObject.Destroy();
				return;
			}
		}

		WorldPosition = nextStep;

	}

	Vector3 GetNextStep()
	{
		return WorldPosition + bulletInfo.direction * bulletInfo.speed * Time.Delta;
	}

	public void InitBall( BulletInfo baseinfo, GameManager manager )
	{
		BulletInfo newInfo = baseinfo;

		bulletInfo = newInfo;
		gameManager = manager;

		WorldScale = bulletInfo.size;
		WorldRotation = Rotation.LookAt( bulletInfo.direction );

	}

	public void SetNewDirection( Vector3 newDirection )
	{
		BulletInfo newInfo = bulletInfo;

		newInfo.direction = newDirection;

		bulletInfo = newInfo;
		WorldRotation = Rotation.LookAt( bulletInfo.direction );

	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		Gizmo.Draw.LineSphere( WorldPosition, size );
	}
}
