namespace Core;

public partial class PathTrack
{
	private void UpdatePath()
	{
		if ( !isDirty ) return;

		splinePoints.Clear();

		if ( PathPointPositions.Count < 2 )
			return;

		float scaledSpacing = Spacing / 1000f;

		if ( CurrentInterpolation == InterpolationMode.Linear )
		{
			foreach ( var point in PathPointPositions )
				splinePoints.Add( point );
		}
		else if ( CurrentInterpolation == InterpolationMode.Spline )
		{
			var extendedPoints = new List<Vector3>();

			extendedPoints.Add( PathPointPositions[0] );
			foreach ( var point in PathPointPositions )
				extendedPoints.Add( point );
			extendedPoints.Add( PathPointPositions[^1] );

			for ( int i = 0; i < extendedPoints.Count - 3; i++ )
			{
				var p0 = extendedPoints[i];
				var p1 = extendedPoints[i + 1];
				var p2 = extendedPoints[i + 2];
				var p3 = extendedPoints[i + 3];

				for ( float t = 0; t <= 1.0f; t += scaledSpacing )
				{
					var point = Vector3.CatmullRomSpline( p0, p1, p2, p3, t );
					splinePoints.Add( point );
				}
			}

			splinePoints.Add( extendedPoints[^2] );
		}

		if ( CurrentPathType == PathType.StaticCable || CurrentPathType == PathType.Rope )
		{
			Vector3 cameraPosition =
				!Game.IsPlaying && Gizmo.Camera != null ? Gizmo.Camera.Position :
				Game.IsPlaying && Scene.Camera != null ? Scene.Camera.WorldPosition :
				Vector3.Zero;

			GenerateCableMesh( cameraPosition );
		}
		else
		{
			cableObject?.Delete();
			cableObject = null;
		}

		isDirty = false;
	}

	private List<Vector3> GenerateCurvatureLODPoints( float curvatureThreshold, int straightSpacing )
	{
		if ( splinePoints == null || splinePoints.Count < 3 )
			return splinePoints;

		List<Vector3> lodPoints = new();
		lodPoints.Add( splinePoints[0] );

		for ( int i = 1; i < splinePoints.Count - 1; i++ )
		{
			Vector3 prev = splinePoints[i - 1];
			Vector3 curr = splinePoints[i];
			Vector3 next = splinePoints[i + 1];

			Vector3 dir1 = (curr - prev).Normal;
			Vector3 dir2 = (next - curr).Normal;

			float dot = Vector3.Dot( dir1, dir2 );

			if ( dot < curvatureThreshold || (i % straightSpacing == 0) )
				lodPoints.Add( curr );
		}

		lodPoints.Add( splinePoints[^1] );
		return lodPoints;
	}
}
