public sealed class UIManager : Component
{
	[Property]
	public DeathRewards DeathRewards { get; set; }

	[Property]
	public ContinuePrompt ContinuePrompt { get; set; }

	[Property]
	public GameHUD GameHud { get; set; }

	[Property]
	public HitNumbers HitNumbers { get; set; }

	public void ShowDeathRewards()
	{
		DeathRewards?.Show();
		ContinuePrompt?.Hide();
	}

	public void ShowContinuePrompt()
	{
		DeathRewards?.Hide();
		ContinuePrompt?.Show();
	}

	public void HideAll()
	{
		DeathRewards?.Hide();
		ContinuePrompt?.Hide();
	}
}
