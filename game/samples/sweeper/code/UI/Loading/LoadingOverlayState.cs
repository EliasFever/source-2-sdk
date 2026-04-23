namespace Sweeper.UI.Loading;

/// <summary>
/// State shared between code and the loading overlay panel.
/// Lets gameplay code request the overlay to be visible slightly before engine loading begins
/// (eg, to give time for fade-in / pre-warm effects).
/// </summary>
public static class LoadingOverlayState
{
	/// <summary>
	/// When true, the loading overlay will be shown even if <see cref="Sandbox.LoadingScreen.IsVisible"/> is false.
	/// This can be cleared by the caller (eg, when a transition fails). The overlay will also clear it automatically
	/// once a real loading phase has completed and the outro finishes.
	/// </summary>
	public static bool PrewarmRequested { get; set; }

	public static void ClearPrewarm()
	{
		PrewarmRequested = false;
	}
}
