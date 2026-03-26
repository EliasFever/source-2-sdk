using Sandbox.Menu;

namespace Sandbox.Internal;


/// <summary>
/// Used to talk to the menu's loading screen.
/// </summary>
internal interface ILoadingInterface : IDisposable
{
	public void LoadingProgress( LoadingProgress progress );
}
