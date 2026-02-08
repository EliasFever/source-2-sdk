using System;
using System.Collections.Generic;

namespace Core;

public enum PathType { Generic, StaticCable, Rope, Trajectory, PathCorner }
public enum InterpolationMode { Linear, Spline }
public enum TextureOrientation { Horizontal, Vertical }

[Description( "A path." )]
[Icon( "polyline" )]
[Title( "Path" )]
public partial class PathTrack : Component, Component.ExecuteInEditor
{
	protected override string GetEditorVis() { return null; }

	[Property, MakeDirty] public PathType CurrentPathType { get; set; } = PathType.Generic;
	[Property, MakeDirty] public InterpolationMode CurrentInterpolation { get; set; } = InterpolationMode.Linear;

	[Property, Feature( "Debug" ), ReadOnly] public List<Vector3> PathPointPositions { get; set; } = new();

	private SceneObject cableObject;

	[Space( 12 )]
	[Header( "Cable Texture options:" )]
	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty]
	public Material CableMaterial { get; set; }

	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty, Title( "Texture Orientation" )]
	public TextureOrientation TexOrientation { get; set; } = TextureOrientation.Horizontal;

	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty, Range( 0.03125f, 4f ), Title( "Texture Scale" )]
	public float TextureScale { get; set; } = 1f;

	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty, Range( 0.03125f, 32f ), Title( "Texture Repeat Around" )]
	public float TextureRepeatCircumference { get; set; } = 1f;

	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty, Range( -1f, 1f ), Title( "Texture Offset Along" )]
	public float TextureOffsetAlong { get; set; } = 0f;

	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty, Range( -1f, 1f ), Title( "Texture Offset Around" )]
	public float TextureOffsetAround { get; set; } = 0f;

	[Space( 12 )]
	[Header( "3D Spline options:" )]
	[Order( 1 )]
	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty, Range( 3, 64 ), Step( 1 ), Title( "Number of slides" )]
	public int Sides { get; set; } = 10;

	[Order( 1 )]
	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty, Range( 20, 512 ), Step( 1 )]
	public float Spacing { get; set; } = 120;

	[Order( 1 )]
	[Property, ShowIf( nameof( CurrentPathType ), PathType.StaticCable ), MakeDirty, Range( 1, 256 ), Step( 1 )]
	public float Radius { get; set; } = 32;

	[Property, MakeDirty] public bool ShowObjects { get; set; } = true;

	bool isDirty = true;
	List<Vector3> splinePoints = new();
	public SceneCamera EditorCamera;

	private float previousDistance = -1f;

	protected override void OnEnabled()
	{
		base.OnEnabled();
		if ( cableObject != null )
			cableObject.RenderingEnabled = true;
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		if ( cableObject != null )
			cableObject.RenderingEnabled = false;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		cableObject?.Delete();
		cableObject = null;
	}

	protected override void OnDirty()
	{
		base.OnDirty();
		MarkDirty();
		UpdatePath();

		switch ( CurrentInterpolation )
		{
			case InterpolationMode.Linear:
			case InterpolationMode.Spline:
				UpdatePath();
				break;
		}
	}

	protected override void OnFixedUpdate()
	{
		lodCheckTimer += Time.Delta;
		if ( lodCheckTimer < 0.2f )
			return;

		lodCheckTimer = 0f;

		Vector3 cameraPos =
			!Game.IsPlaying && EditorCamera != null ? EditorCamera.Position :
			Game.IsPlaying && Scene.Camera != null ? Scene.Camera.WorldPosition :
			Vector3.Zero;

		float distance = Vector3.DistanceBetween( splinePoints[0], cameraPos );
		int lodLevel = GetLODLevel( distance );

		if ( lodLevel != previousLODLevel || Math.Abs( distance - previousDistance ) > 0.5f )
		{
			previousLODLevel = lodLevel;
			previousDistance = distance;
			GenerateCableMesh( cameraPos );
		}
	}

	public void SetPathPoints( List<Vector3> positions )
	{
		PathPointPositions.Clear();
		PathPointPositions.AddRange( positions );
		MarkDirty();
	}

}
