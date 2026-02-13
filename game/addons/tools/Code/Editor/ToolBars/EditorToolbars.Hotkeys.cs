using Editor.MeshEditor;

namespace Editor;

public static partial class EditorToolBars
{
	[Shortcut( "tools.position-tool", "t", typeof( SceneViewWidget ) )]
	public static void ActivatePositionMode() => SetMoveMode( "mesh.position.mode" );

	[Shortcut( "tools.rotate-tool", "r", typeof( SceneViewWidget ) )]
	public static void ActivateRotateMode() => SetMoveMode( "mesh.rotate.mode" );

	[Shortcut( "tools.scale-tool", "e", typeof( SceneViewWidget ) )]
	public static void ActivateScaleMode() => SetMoveMode( "mesh.scale.mode" );

	[Shortcut( "tools.pivot-tool", "ins", typeof( SceneViewWidget ) )]
	static public void ActivatePivotMode() => SetMoveMode( "mesh.pivot.mode" );

	[Shortcut( "tools.resize-tool", "y", typeof( SceneViewWidget ) )]
	static public void ActivateResizeMode() => SetMoveMode( "mesh.resize.mode" );

	static void SetMoveMode( string id )
	{
		if ( SceneViewWidget.Current?.Tools?.CurrentTool is MeshTool meshTool )
			meshTool.MoveMode = EditorTypeLibrary.Create<MoveMode>( id );
	}

	static void SetToolMode( string id, string id2 )
	{
		EditorToolManager.CurrentModeName = id;
		EditorToolManager.CurrentSubModeName = id2;
	}

	[Shortcut( "tools.vertex-tool", "1", typeof( SceneViewWidget ) )]
	public static void ActivateVertexMode() => SetToolMode( nameof( MeshTool ), nameof( VertexTool ) );

	[Shortcut( identifier: "tools.edge-tool", "2", typeof( SceneViewWidget ) )]
	public static void ActivateEdgeMode() => SetToolMode( nameof( MeshTool ), nameof( EdgeTool ) );

	[Shortcut( identifier: "tools.face-tool", "3", typeof( SceneViewWidget ) )]
	public static void ActivateFaceTool() => SetToolMode( nameof( MeshTool ), nameof( FaceTool ) );

	[Shortcut( identifier: "tools.mesh-selection", "4", typeof( SceneViewWidget ) )]
	public static void ActivateMeshTool() => SetToolMode( nameof( MeshTool ), nameof( MeshSelection ) );

	[Shortcut( identifier: "tools.object-selection", "5", typeof( SceneViewWidget ) )]
	public static void ActivateObjectTool() => SetToolMode( nameof( MeshTool ), nameof( ObjectSelection ) );
}
