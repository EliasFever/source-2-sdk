namespace Editor.PathEditor;

using System;
using System.Collections.Generic;
using System.Text;

[Title( "Move/Position Path Points" )]
[Icon( "control_camera" )]
[Alias( "tools.pathpoint-tool" )]
[Group( "1" )]
[Order( 0 )]
public class PathPointEditTool( PathTool Tool ) : EditorTool
{
	private PathTrack path;
	private readonly HashSet<int> selectedIndices = new();
	private Vector3 dragDelta;
	private IDisposable undoScope;

	public override bool HasBoxSelectionMode() => true;

	public override void OnUpdate()
	{
		base.OnUpdate();

		if ( path == null )
		{
			path = Scene.GetAllComponents<PathTrack>().FirstOrDefault();
			if ( path == null ) return;
		}

		if ( path.PathPointPositions.Count == 0 )
			return;

		using var scope = Gizmo.Scope( "PathPointEditTool" );

		// Hover detection
		var closestIndex = GetClosestPointToRay( 8f );
		if ( closestIndex.HasValue )
		{
			Gizmo.Hitbox.TrySetHovered( path.PathPointPositions[closestIndex.Value] );
		}

		// Draw selected points
		DrawGizmos();

		// Move selected points
		if ( Gizmo.Control.Position( "pathpoint-move", Vector3.Zero, out var delta, Rotation.Identity ) )
		{
			StartDrag();

			foreach ( var i in selectedIndices.ToList() )
			{
				path.PathPointPositions[i] += delta;
			}

			path.MarkDirty();
		}
		else
		{
			// Mouse released
			undoScope?.Dispose();
			undoScope = null;
		}
	}

	private void StartDrag()
	{
		if ( undoScope != null ) return;
		undoScope = SceneEditorSession.Active.UndoScope( "Move Path Point(s)" ).Push();
	}

	private void DrawGizmos()
	{
		Gizmo.Draw.IgnoreDepth = true;
		foreach ( var i in selectedIndices )
		{
			Gizmo.Draw.Color = Color.Yellow;
			Gizmo.Draw.Sprite( path.PathPointPositions[i], 8, null, false );
		}
	}

	protected override void OnBoxSelect( Frustum frustum, Rect screenRect, bool isFinal )
	{
		if ( path == null ) return;

		var selection = new HashSet<int>();
		var previous = new HashSet<int>();

		for ( int i = 0; i < path.PathPointPositions.Count; i++ )
		{
			var pos = path.PathPointPositions[i];
			if ( frustum.IsInside( pos ) )
				selection.Add( i );
			else
				previous.Add( i );
		}

		foreach ( var i in selection )
			selectedIndices.Add( i );

		foreach ( var i in previous )
			selectedIndices.Remove( i );
	}

	private int? GetClosestPointToRay( float maxDistance )
	{
		if ( path == null || path.PathPointPositions.Count == 0 )
			return null;

		var camera = Gizmo.Camera;
		if ( camera == null )
			return null;

		var ray = new Ray( camera.Position, camera.Rotation.Forward );

		float closestDist = maxDistance;
		int closestIndex = -1;

		for ( int i = 0; i < path.PathPointPositions.Count; i++ )
		{
			var p = path.PathPointPositions[i];

			float dist = ray.Position.Distance( p );
			if ( dist < closestDist )
			{
				closestDist = dist;
				closestIndex = i;
			}
		}

		return closestIndex >= 0 ? closestIndex : null;
	}

	private int? GetClosestPoint( float radius )
	{
	///	var tool = Tool;
		var camera = Gizmo.Camera;
		if ( camera == null )
			return null;

		var ray = new Ray( camera.Position, camera.Rotation.Forward );

		float best = radius;
		int bestIndex = -1;

		for ( int i = 0; i < Tool.pathPointPositions.Count; i++ )
		{
			//float d = ray.Distance( Tool.pathPointPositions[i] );
			//if ( d < best )
			//{
			//	best = d;
			//	bestIndex = i;
			//}
		}

		return bestIndex >= 0 ? bestIndex : null;
	}

}
