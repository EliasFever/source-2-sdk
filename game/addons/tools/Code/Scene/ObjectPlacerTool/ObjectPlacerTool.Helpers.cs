namespace Editor;

public partial class ObjectPlacerTool
{
	[Shortcut( "tools.object-placer-tool", "Shift+E", typeof( SceneViewportWidget ) )]
	public static void ActivateTool()
	{
		EditorToolManager.SetTool( nameof( ObjectPlacerTool ) );
	}
}
