namespace Editor.PathEditor;

using System;

/// <summary>
/// Create path objects or primitives
/// </summary>
[EditorTool]
[Icon( "polyline" )]
public class PathTool : EditorTool
{
	internal List<Vector3> pathPointPositions = new();
	internal int hoveredPointIndex = -1;
	internal HashSet<int> selectedPointIndices = new();

	private static PathType CurrentPathType { get; set; } = PathType.Generic;
	private static InterpolationMode CurrentInterpolation { get; set; } = InterpolationMode.Linear;

	private Texture pointTexture = Texture.Load( "materials/tools/handle_edged_circle_tga_b183d0e4.generated.vtex_c" );

	public override IEnumerable<EditorTool> GetSubtools()
	{
		yield return new PathPointCreateTool( this );     // Subtool for adding new point
		yield return new PathPointEditTool( this );    // Subtool for manipulating existing points
	}

	private struct PathProperties
	{
		[Group( "Settings" )]
		public readonly PathType PathType { get => CurrentPathType; set => CurrentPathType = value; }

		[Group( "Settings" )]
		public readonly InterpolationMode Interpolation { get => CurrentInterpolation; set => CurrentInterpolation = value; }
	}

	[InlineEditor( Label = false )]
	private readonly PathProperties pathProperties = new();

	public override void OnEnabled()
	{
		AllowGameObjectSelection = false;
		SceneEditorSession.Active.Selection.Clear();
	}

	public override void OnDisabled()
	{
		selectedPointIndices.Clear();
		hoveredPointIndex = -1;
	}

	public override void OnUpdate()
	{
		base.OnUpdate();

		if ( Scene == null || !Scene.IsValid )
			return;

		DrawPathVisualization();
	}

	//private void UpdatePointInteraction()
	//{
	//	hoveredPointIndex = -1;
	//	float closestDistance = float.MaxValue;

	//	for ( int i = 0; i < pathPointPositions.Count; i++ )
	//	{
	//		var pos = pathPointPositions[i];

	//		// Calculate closest point on ray to this position
	//		var rayToPoint = pos - Gizmo.CurrentRay.Position;
	//		var projectionDistance = Vector3.Dot( rayToPoint, Gizmo.CurrentRay.Forward );
	//		var closestPointOnRay = Gizmo.CurrentRay.Position + Gizmo.CurrentRay.Forward * projectionDistance;
	//		var distanceToRay = Vector3.DistanceBetween( pos, closestPointOnRay );

	//		if ( distanceToRay < 12f ) // Within hitbox radius
	//		{
	//			var rayDistance = Vector3.DistanceBetween( Gizmo.CurrentRay.Position, pos );

	//			if ( rayDistance < closestDistance )
	//			{
	//				closestDistance = rayDistance;
	//				hoveredPointIndex = i;
	//			}
	//		}
	//	}

	//	// Handle selection
	//	if ( hoveredPointIndex != -1 && Gizmo.WasLeftMousePressed )
	//	{
	//		if ( Sandbox.Input.Down( "ctrl" ) )
	//		{
	//			// Toggle selection
	//			if ( selectedPointIndices.Contains( hoveredPointIndex ) )
	//				selectedPointIndices.Remove( hoveredPointIndex );
	//			else
	//				selectedPointIndices.Add( hoveredPointIndex );
	//		}
	//		else
	//		{
	//			// Single selection
	//			selectedPointIndices.Clear();
	//			selectedPointIndices.Add( hoveredPointIndex );
	//		}
	//	}

	//	// Clear selection on empty click
	//	if ( Gizmo.WasLeftMousePressed && hoveredPointIndex == -1 && !Sandbox.Input.Down( "ctrl" ) )
	//	{
	//		selectedPointIndices.Clear();
	//	}
	//}

	private void DrawPathVisualization()
	{
		if ( pathPointPositions.Count == 0 )
			return;

		// Draw lines connecting points
		if ( pathPointPositions.Count >= 2 )
		{
			Gizmo.Draw.LineThickness = 2.5f;

			for ( int i = 0; i < pathPointPositions.Count - 1; i++ )
			{
				var a = pathPointPositions[i];
				var b = pathPointPositions[i + 1];

				// Color based on selection
				bool isSegmentSelected = selectedPointIndices.Contains( i ) || selectedPointIndices.Contains( i + 1 );
				Gizmo.Draw.Color = isSegmentSelected ? Color.Yellow : Color.White;

				Gizmo.Draw.Line( a, b );
			}
		}

		// Draw point sprites
		if ( pointTexture == null )
			return;

		for ( int i = 0; i < pathPointPositions.Count; i++ )
		{
			var pos = pathPointPositions[i];
			var spritePos = pos + Vector3.Up * 0.2f;

			float dist = Vector3.DistanceBetween( spritePos, Gizmo.Camera.Position );
			float scale = Math.Clamp( dist * 0.05f, 1.2f, 4.0f );

			bool isHovered = hoveredPointIndex == i;
			bool isSelected = selectedPointIndices.Contains( i );

			// Scale up on hover
			if ( isHovered )
				scale = MathX.Lerp( scale, scale * 1.5f, 0.2f );

			// Set color based on state
			if ( isSelected )
				Gizmo.Draw.Color = Color.Yellow;
			else
				Gizmo.Draw.Color = Color.White;

			Gizmo.Hitbox.BBox( BBox.FromPositionAndSize( spritePos, 12f ) );    // Draw hitbox
			Gizmo.Draw.Sprite( spritePos, scale, pointTexture );                // Draw sprite

			// Draw label for all points
			var labelPos = pos + Vector3.Up * 4f;
			Gizmo.Draw.Color = isSelected ? Color.Yellow : Color.White.WithAlpha( 0.7f );
			Gizmo.Draw.Text( i.ToString(), new Transform( labelPos, Rotation.Identity ), font: "Roboto", size: 12f );
		}
	}

	/// <summary>
	/// Create the sidebar widget for the path tool
	/// </summary>
	public override Widget CreateToolSidebar()
	{
		return new PathToolSidebarWidget( pathProperties.GetSerialized(), this );
	}

	public class PathToolSidebarWidget : ToolSidebarWidget
	{
		private PathTool _tool;
		private Label _pointCountLabel;

		public PathToolSidebarWidget( SerializedObject so, PathTool tool ) : base()
		{
			_tool = tool;

			AddTitle( "Path Tool", "polyline" );

			// Settings Group
			{
				var group = AddGroup( "Settings" );
				var row = group.AddRow();
				row.Spacing = 4;

				var pathTypeControl = ControlWidget.Create( so.GetProperty( nameof( PathProperties.PathType ) ) );
				var interpolationControl = ControlWidget.Create( so.GetProperty( nameof( PathProperties.Interpolation ) ) );

				pathTypeControl.FixedHeight = Theme.ControlHeight;
				interpolationControl.FixedHeight = Theme.ControlHeight;

				row.Add( pathTypeControl );
				row.Add( interpolationControl );
			}

			// Path Info
			{
				var group = AddGroup( "Path Info" );
				_pointCountLabel = group.Add( new Label( $"Points: {_tool.pathPointPositions.Count}" ) );
			}

			// Actions
			{
				var group = AddGroup( "Actions" );
				var row = group.AddRow();
				row.Spacing = 2;
				row.Alignment = TextFlag.Left;

				var Clear = CreateButton( "Clear Path", "delete", "path.clear", ClearPath, _tool.pathPointPositions.Count > 0, row );
				var Finish = CreateButton( "Delete Selected", "remove", "path.delete_selected", DeleteSelected, _tool.selectedPointIndices.Count > 0, row );
				row.Add( Clear );
				row.Add( Finish );

			}

			{
				var row = new Widget { Layout = Layout.Row() };
				row.Layout.Spacing = 4;

				CreateButton( "Create Path Object", "check", "path.create", CreatePathObject, _tool.pathPointPositions.Count > 1, row.Layout );

				Layout.Add( row );
			}

			// Instructions
			{
				var group = AddGroup( "Instructions" );
				var instructions = group.Add( new Label( "Left click on surfaces to add path points.\nClick points to select them.\nHold Ctrl for multi-select.\nPress 'Create Path Object' when finished." ) );
				instructions.WordWrap = true;
			}

			Layout.AddStretchCell();
		}

		[Shortcut( "path.clear", "C", typeof( SceneViewportWidget ) )]
		private void ClearPath()
		{
			_tool.pathPointPositions.Clear();
			_tool.selectedPointIndices.Clear();
			UpdatePointCount();
		}

		[Shortcut( "path.delete_selected", "DEL", typeof( SceneViewportWidget ) )]
		private void DeleteSelected()
		{
			if ( _tool.selectedPointIndices.Count == 0 )
				return;

			// Sort indices in descending order to remove from end first
			var sortedIndices = _tool.selectedPointIndices.OrderByDescending( x => x ).ToList();

			foreach ( var index in sortedIndices )
			{
				if ( index >= 0 && index < _tool.pathPointPositions.Count )
				{
					_tool.pathPointPositions.RemoveAt( index );
				}
			}

			_tool.selectedPointIndices.Clear();
			UpdatePointCount();
		}

		[Shortcut( "path.create", "ENTER", typeof( SceneViewportWidget ) )]
		private void CreatePathObject()
		{
			if ( _tool.pathPointPositions.Count < 2 )
				return;

			using var scope = SceneEditorSession.Scope();

			// Create the path GameObject
			var pathObject = _tool.Scene.CreateObject();
			pathObject.Name = $"Path_{Random.Shared.Next( 1, 1001 )}";

			// Add PathTrack component with the positions
			var pathTrack = pathObject.Components.Create<PathTrack>();
			pathTrack.CurrentPathType = CurrentPathType;
			pathTrack.CurrentInterpolation = CurrentInterpolation;

			// Set the path points directly as a list of positions
			pathTrack.SetPathPoints( _tool.pathPointPositions.ToList() );

			// Select the created path object
			SceneEditorSession.Active.Selection.Clear();
			SceneEditorSession.Active.Selection.Add( pathObject );

			// Clear the working path
			_tool.pathPointPositions.Clear();
			_tool.selectedPointIndices.Clear();

			UpdatePointCount();
		}

		private void UpdatePointCount()
		{
			if ( _pointCountLabel != null )
			{
				_pointCountLabel.Text = $"Points: {_tool.pathPointPositions.Count}";
			}
		}
	}

	private void AddPathPoint( Vector3 position )
	{
		// Simply add the position - no GameObjects created during editing
		pathPointPositions.Add( position + new Vector3( 0, 0, 0.6f ) );
	}

	[Event( "scene.saved" )]
	static void OnSceneSaved( Scene scene )
	{
	}
}
