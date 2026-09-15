using Sandbox;
using System.Threading.Tasks;

public class BaseVisualEnemy : Component
{
	[Property, Group( "Stats" )] protected float visualHitDuration = 0.2f;
	[Property, Group( "Refs" )] protected ModelRenderer model { get; set; }

	Task visualHitTask = null;


	protected override void OnStart()
	{
		base.OnStart();

		if ( !model.IsValid() || model.MaterialOverride is null )
		{
			return;
		}

		model.SceneObject.Batchable = false;

	}

	[Rpc.Broadcast]
	public void TakeHit()
	{
		if ( visualHitTask == null )
		{
			visualHitTask = VisualTakeHit();
		}
	}

	async Task VisualTakeHit()
	{
		float timer = 0f;

		while ( timer <= visualHitDuration )
		{
			model.Attributes.Set( "PercentFlash", (timer / visualHitDuration).Clamp( 0f, 1f ) );
			timer += Time.Delta;

			await Task.FrameEnd();
		}

		timer = visualHitDuration;

		while ( timer >= 0 )
		{
			
			model.Attributes.Set( "PercentFlash", (timer / visualHitDuration).Clamp( 0f, 1f ) );
			timer -= Time.Delta * 2f;

			await Task.FrameEnd();
		}

		model.Attributes.Set( "PercentFlash", 0f );
		visualHitTask = null;
	}
}
