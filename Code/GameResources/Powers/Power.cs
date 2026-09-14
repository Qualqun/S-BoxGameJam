using Sandbox;

[GameResource( "Power", "power", "Description de la ressource", Icon = "bolt" )]
public partial class Power : GameResource
{
	[Property]
	public string Title { get; set; }

	[Property]
	public BoostType BoostType { get; set; }

	[Property]
	public Texture Image { get; set; }

	[Property, TextArea]
	public string Description { get; set; }
}
