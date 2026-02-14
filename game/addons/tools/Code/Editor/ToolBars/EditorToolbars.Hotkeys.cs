using Editor.MeshEditor;

namespace Editor;

public static partial class EditorToolBars
{
	static void SetMoveMode( string id )
	{
		if ( SceneViewWidget.Current?.Tools?.CurrentTool is MeshTool meshTool )
			meshTool.MoveMode = EditorTypeLibrary.Create<MoveMode>( id );
	}

	static void SetToolMode( string id, string id2 )
	{
		var tools = SceneViewWidget.Current?.Tools;
		if ( tools == null )
		{
			EditorToolManager.SetTool( id );
			EditorToolManager.SetSubTool( id2 );
			return;
		}

		var current = tools.CurrentTool;
		if ( current != null && current.GetType().Name == id )
		{
			EditorToolManager.SetSubTool( id2 );
			tools.UpdateSubTool( EditorToolManager.CurrentSubModeName );
			return;
		}

		EditorToolManager.SetTool( id );
		tools.UpdateTool( EditorToolManager.CurrentModeName );

		EditorToolManager.SetSubTool( id2 );
		tools.UpdateSubTool( EditorToolManager.CurrentSubModeName );
	}

	//
	// Move modes
	//
	[Shortcut( "tools.position-tool", "t", typeof( SceneViewWidget ) )]
	public static void ActivatePositionMode() => SetMoveMode( "mesh.position.mode" );

	[Shortcut( "tools.rotate-tool", "r", typeof( SceneViewWidget ) )]
	public static void ActivateRotateMode() => SetMoveMode( "mesh.rotate.mode" );

	[Shortcut( "tools.scale-tool", "e", typeof( SceneViewWidget ) )]
	public static void ActivateScaleMode() => SetMoveMode( "mesh.scale.mode" );

	[Shortcut( "tools.pivot-tool", "ins", typeof( SceneViewWidget ) )]
	static public void ActivatePivotMode() => SetMoveMode( "mesh.pivot.mode" );

	[Shortcut( "tools.resize-tool", "q", typeof( SceneViewWidget ) )]
	static public void ActivateResizeMode() => SetMoveMode( "mesh.resize.mode" );
	
	//
	// Selection modes
	//
	[Shortcut( "tools.primitive-tool", "Shift+B", typeof( SceneViewWidget ) )]
	public static void ActivatePrimitiveMode() => SetToolMode( nameof( MeshTool ), nameof( PrimitiveTool ) );

	[Shortcut( "tools.vertex-tool", "1", typeof( SceneViewWidget ) )]
	public static void ActivateVertexMode() => SetToolMode( nameof( MeshTool ), nameof( VertexTool ) );

	[Shortcut( "tools.edge-tool", "2", typeof( SceneViewWidget ) )]
	public static void ActivateEdgeMode() => SetToolMode( nameof( MeshTool ), nameof( EdgeTool ) );

	[Shortcut( "tools.face-tool", "3", typeof( SceneViewWidget ) )]
	public static void ActivateFaceTool() => SetToolMode( nameof( MeshTool ), nameof( FaceTool ) );

	[Shortcut( "tools.mesh-selection", "4", typeof( SceneViewWidget ) )]
	public static void ActivateMeshTool() => SetToolMode( nameof( MeshTool ), nameof( MeshSelection ) );

	[Shortcut( "tools.object-selection", "5", typeof( SceneViewWidget ) )]
	public static void ActivateObjectTool() => SetToolMode( nameof( MeshTool ), nameof( ObjectSelection ) );
}
