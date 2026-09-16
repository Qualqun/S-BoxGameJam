using Sandbox;
using System.Threading;
using System.Threading.Tasks;

public sealed class RangeVisualEnemy : BaseVisualEnemy
{
	[Property, Group( "Stats" )] float blinkSpeed { get; set; } = 0.01f;

	[Property, Group( "Refs" )] LineRenderer laserInfo { get; set; }

	CancellationTokenSource cancellation;

	protected override void OnStart()
	{
		Gradient laserColor = laserInfo.Color;
		Color[] color = new Color[1];

		color[0] = Color.Red;
		laserColor = Gradient.FromColors( color );

		laserInfo.Color = laserColor;

		base.OnStart();
	}


	[Rpc.Broadcast]
	public void UpdateLaser( Vector3 start, Vector3 end )
	{
		laserInfo.VectorPoints = new List<Vector3>
		{
			start,
			end
		};
	}

	[Rpc.Broadcast]
	public void StartBlink()
	{
		cancellation = new CancellationTokenSource();
		_ = LaserBlink( cancellation.Token );
	}

	[Rpc.Broadcast]
	public void ResetLaser()
	{
		cancellation.Cancel();
		cancellation.Dispose();
		cancellation = null;

		laserInfo.VectorPoints.Clear();
	}

	async Task LaserBlink( CancellationToken token )
	{
		float timer = 0;
		Gradient laserColor = laserInfo.Color;

		while ( !token.IsCancellationRequested )
		{
			timer += Time.Delta;

			if ( timer > blinkSpeed )
			{
				Color[] color = new Color[1];
				bool isRed = laserColor.Colors[0].Value == Color.Red;

				timer = 0f;
				color[0] = isRed ? Color.Yellow : Color.Red;

				laserColor = Gradient.FromColors( color );
				laserInfo.Color = laserColor;
			}

			await Task.FrameEnd();
		}
	}
}
