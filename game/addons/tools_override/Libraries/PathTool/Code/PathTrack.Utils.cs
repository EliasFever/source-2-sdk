using System;

namespace Core;

public partial class PathTrack
{
	public void MarkDirty()
	{
		isDirty = true;
	}

	private bool IsInSegment( Vector3 a, Vector3 b, Vector3 p1, Vector3 p2 )
	{
		var segmentMin = Vector3.Min( p1, p2 );
		var segmentMax = Vector3.Max( p1, p2 );
		var mid = (a + b) * 0.5f;
		return mid.x >= segmentMin.x && mid.x <= segmentMax.x &&
			   mid.y >= segmentMin.y && mid.y <= segmentMax.y &&
			   mid.z >= segmentMin.z && mid.z <= segmentMax.z;
	}

	private float DistanceRatio( Vector3 a, Vector3 b, Vector3 p1, Vector3 p2 )
	{
		var mid = (a + b) * 0.5f;
		float totalDist = (p1 - p2).Length;
		if ( totalDist <= 0.001f ) return 0f;
		float dist = (p1 - mid).Length;
		return dist / totalDist;
	}

	private float GetOriginalSplineDistanceAt( Vector3 point, List<Vector3> originalPoints, List<float> originalLengths )
	{
		float minDist = float.MaxValue;
		int closestIndex = 0;

		for ( int i = 0; i < originalPoints.Count; i++ )
		{
			float dist = Vector3.DistanceBetween( point, originalPoints[i] );
			if ( dist < minDist )
			{
				minDist = dist;
				closestIndex = i;
			}
		}

		return originalLengths[Math.Clamp( closestIndex, 0, originalLengths.Count - 1 )];
	}

	private int GetLODLevel( float distanceToCamera )
	{
		if ( !EnableLODs || _lodLevelCount == 0 )
			return 0; // No LOD, full detail

		int lodLevel = 0;

		for ( int i = 0; i < _lodLevelCount; i++ )
		{
			float lodDistance = i switch
			{
				0 => LODDistance0,
				1 => LODDistance1,
				2 => LODDistance2,
				3 => LODDistance3,
				_ => float.MaxValue
			};

			if ( distanceToCamera >= lodDistance )
				lodLevel = i;
		}

		// Clamp to valid LOD levels
		return Math.Clamp( lodLevel, 0, _lodLevelCount - 1 );
	}

	private (float curvatureThreshold, int straightSpacing) GetLODSettings( int lodLevel )
	{
		switch ( lodLevel )
		{
			case 0: return (1f, 1);
			case 1: return (0.985f, 2);
			case 2: return (0.975f, 6);
			default: return (0.985f, 6);
		}
	}

	private float GetSidesMultiplier( int lodLevel )
	{
		switch ( lodLevel )
		{
			case 0: return 1.0f;
			case 1: return 0.75f;
			case 2: return 0.5f;
			default: return 0.25f;
		}
	}
}
