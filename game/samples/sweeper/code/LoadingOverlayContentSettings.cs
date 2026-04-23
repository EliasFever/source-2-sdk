namespace Sandbox;

public class LoadingOverlayContentSettings : ConfigData
{
	[Hide]
	public override int Version => 1;

	/// <summary>
	/// Optional Chapter number/title KV for the loading overlay.
	/// Keys should be numeric strings ("1", "2", etc) if you want numeric ordering.
	/// </summary>
	public Dictionary<string, string> Chapter { get; set; } = new();

	/// <summary>
	/// Optional chapter description shown on the loading screen.
	/// </summary>
	public string ChapterDesc { get; set; }

	/// <summary>
	/// Optional list of tips to display during loading.
	/// </summary>
	public List<string> Tips { get; set; } = new();
}

