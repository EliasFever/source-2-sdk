
namespace Editor.MeshEditor;

/// <summary>
/// Mesh tools mode for creating and editing meshes.
/// </summary>
[EditorTool( "tools.mesh-tool" )]
[Title( "Mapping" )]
[Icon( "hardware" )]
[Alias( "mesh" )]
public partial class MeshTool : EditorTool
{
	public Material ActiveMaterial
	{
		get => MeshActiveMaterialState.Instance.ActiveMaterial;
		set => MeshActiveMaterialState.Instance.ActiveMaterial = value;
	}

	public MoveMode MoveMode { get; set; }

	public void SetMoveMode<T>() where T : MoveMode
	{
		if ( MoveMode?.GetType() == typeof( T ) ) return;
		MoveMode = EditorTypeLibrary.Create<MoveMode>( typeof( T ) );
	}

	// legacy compat version of the above
	public void SetMoveMode( TypeDescription type )
	{
		if ( MoveMode?.GetType() == type.TargetType ) return;
		MoveMode = EditorTypeLibrary.Create<MoveMode>( type.TargetType );
	}

	public override IEnumerable<EditorTool> GetSubtools()
	{
		yield return new PrimitiveTool( this );
		yield return new MeshSelection( this );
		yield return new ObjectSelection( this );
		yield return new VertexTool( this );
		yield return new EdgeTool( this );
		yield return new FaceTool( this ); 
		yield return new TextureTool( this );
		yield return new VertexPaintTool( this );
	}

	public override void OnEnabled()
	{
		base.OnEnabled();

		AllowGameObjectSelection = false;
		AllowContextMenu = false;

		Selection.Clear();

		SetMoveMode<PositionMode>();
		LoadToolbarCookies();
	}

	public override void OnUpdate()
	{
		AllowGameObjectSelection = CurrentTool?.GetType() == typeof( ObjectSelection );
	}

	public override void OnSelectionChanged()
	{
		CurrentTool?.OnSelectionChanged();
	}

	[Shortcut( "tools.mesh-tool", "m", typeof( SceneViewWidget ) )]
	public static void ActivateTool()
	{
		EditorToolManager.SetTool( nameof( MeshTool ) );
		EditorToolManager.SetSubTool( nameof( ObjectSelection ) );
	}

}
