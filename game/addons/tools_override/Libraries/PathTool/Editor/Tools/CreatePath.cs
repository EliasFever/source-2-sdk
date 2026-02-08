namespace Editor.PathEditor;

using Editor.MapEditor;
using Editor.MeshEditor;
using System;
using System.Collections.Generic;
using System.Text;


[Title( "Create Path Points" )]
[Icon( "add" )]
[Alias( "tools.pathpoint-create-tool" )]
[Group( "1" )]
[Order( 0 )]
public class PathPointCreateTool( PathTool Tool ) : EditorTool
{

	public override void OnUpdate()
	{
		base.OnUpdate();

		if ( Tool == null )
			return;

		var tr = Scene.Trace
			.Ray( Gizmo.CurrentRay, 5000 )
			.UseRenderMeshes( true )
			.UsePhysicsWorld( false )
			.Run();

		if ( !tr.Hit )
			return;

		using ( Gizmo.Scope( "path-create-cursor" ) )
		{
			Gizmo.Transform = new Transform( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
			Gizmo.Draw.LineCircle( 0, 10 );
		}

		if ( Gizmo.WasLeftMousePressed && !Gizmo.Pressed.Any )
		{
			Tool.pathPointPositions.Add( tr.HitPosition + Vector3.Up * 0.6f );
		}
	}
	private bool TryGetRayHit( Ray ray, out Vector3 hitPos )
	{
		// Replace with your scene/raycast logic
		hitPos = ray.Position + ray.Forward * 100;
		return true;
	}

	[Shortcut( "tools.pathpoint-create-tool", "n", typeof( SceneViewportWidget ) )]
	public static void ActivateSubTool()
	{
		if ( EditorToolManager.CurrentModeName != nameof( PathTool ) ) return;
		EditorToolManager.SetSubTool( nameof( PathPointCreateTool ) );
	}
}
