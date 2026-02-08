using System;

namespace Core;

public partial class PathTrack
{
	[Header( "LOD Settings" )]
	[Feature( "LOD" )]
	[Space( 12 )]
	[Property, MakeDirty, Title( "Enable LODs" )]
	public bool EnableLODs { get; set; } = true;

	[Property, MakeDirty, Title( "Number of LOD Levels" ), Range( 0, 4 ), Step( 1 )]
	[Feature( "LOD" )]
	[ShowIfPlus( ShowIfPlusLogical.None, "EnableLODs", true )]
	public int LODLevelCount
	{
		get => _lodLevelCount;
		set
		{
			_lodLevelCount = Math.Clamp( value, 0, MaxLODLevels );
			ValidateLODDistances();
			ValidateLODSpacings();
			ValidateLODSidesMultipliers();
		}
	}
	private int _lodLevelCount = 2;
	private const int MaxLODLevels = 4;


	#region LOD Distances
	[Property, MakeDirty, Feature( "LOD" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 0, ShowIfPlusComparison.GreaterOrEqual )]
	public float LODDistance0 { get; set; } = 500f;
	[Property, MakeDirty, Feature( "LOD" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 1, ShowIfPlusComparison.GreaterOrEqual )]
	public float LODDistance1 { get; set; } = 1000f;
	[Property, MakeDirty, Feature( "LOD" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 2, ShowIfPlusComparison.GreaterOrEqual )]
	public float LODDistance2 { get; set; } = 2000f;
	[Property, MakeDirty, Feature( "LOD" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 3, ShowIfPlusComparison.GreaterOrEqual )]
	public float LODDistance3 { get; set; } = 4000f;
	#endregion

	#region LOD Spacings
	[Property, MakeDirty, Feature( "LOD" ), Group( "Spacings" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 0, ShowIfPlusComparison.GreaterOrEqual )]
	public int LODSpacings0 { get; set; } = 1;
	[Property, MakeDirty, Feature( "LOD" ), Group( "Spacings" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 1, ShowIfPlusComparison.GreaterOrEqual )]
	public int LODSpacings1 { get; set; } = 2;
	[Property, MakeDirty, Feature( "LOD" ), Group( "Spacings" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 2, ShowIfPlusComparison.GreaterOrEqual )]
	public int LODSpacings2 { get; set; } = 4;
	[Property, MakeDirty, Feature( "LOD" ), Group( "Spacings" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 3, ShowIfPlusComparison.GreaterOrEqual )]
	public int LODSpacings3 { get; set; } = 6;
	#endregion

	#region LOD Sides Multipliers
	[Property, MakeDirty, Feature( "LOD" ), Group( "Sides Multiplier" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 0, ShowIfPlusComparison.GreaterOrEqual )]
	public float LODSidesMultiplier0 { get; set; } = 1f;
	[Property, MakeDirty, Feature( "LOD" ), Group( "Sides Multiplier" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 1, ShowIfPlusComparison.GreaterOrEqual )]
	public float LODSidesMultiplier1 { get; set; } = 0.75f;
	[Property, MakeDirty, Feature( "LOD" ), Group( "Sides Multiplier" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 2, ShowIfPlusComparison.GreaterOrEqual )]
	public float LODSidesMultiplier2 { get; set; } = 0.5f;
	[Property, MakeDirty, Feature( "LOD" ), Group( "Sides Multiplier" )]
	[ShowIfPlus( ShowIfPlusLogical.And, "EnableLODs", true, "LODLevelCount", 3, ShowIfPlusComparison.GreaterOrEqual )]
	public float LODSidesMultiplier3 { get; set; } = 0.25f;
	#endregion


	#region LOD Validatio
	// Validation Methods
	private void ValidateLODDistances()
	{
		for ( int i = _lodLevelCount; i < MaxLODLevels; i++ )
		{
			switch ( i )
			{
				case 0: LODDistance0 = 500f; break;
				case 1: LODDistance1 = 1000f; break;
				case 2: LODDistance2 = 2000f; break;
				case 3: LODDistance3 = 4000f; break;
			}
		}
	}

	private void ValidateLODSpacings()
	{
		for ( int i = _lodLevelCount; i < MaxLODLevels; i++ )
		{
			switch ( i )
			{
				case 0: LODSpacings0 = 1; break;
				case 1: LODSpacings1 = 2; break;
				case 2: LODSpacings2 = 4; break;
				case 3: LODSpacings3 = 6; break;
			}
		}
	}

	private void ValidateLODSidesMultipliers()
	{
		for ( int i = _lodLevelCount; i < MaxLODLevels; i++ )
		{
			switch ( i )
			{
				case 0: LODSidesMultiplier0 = 1f; break;
				case 1: LODSidesMultiplier1 = 0.75f; break;
				case 2: LODSidesMultiplier2 = 0.5f; break;
				case 3: LODSidesMultiplier3 = 0.25f; break;
			}
		}
	}
	#endregion


	private void GenerateCableMesh( Vector3 cameraPosition )
	{
		if ( splinePoints == null || splinePoints.Count < 2 )
			return;

		// If LODs are disabled or no LOD levels, generate full-detail mesh
		if ( !EnableLODs || _lodLevelCount == 0 )
		{
			GenerateMesh( new List<Vector3>( splinePoints ), Sides );
			return;
		}

		var cameraPos = cameraPosition != Vector3.Zero
			? cameraPosition
			: Scene.Camera?.WorldPosition ?? Gizmo.Camera?.Position ?? Vector3.Zero;

		float distance = Vector3.DistanceBetween( splinePoints[0], cameraPos );		// Determine distance to camera
		int lodLevel = GetLODLevel( distance );										// Determine LOD level based on distance
		lodLevel = Math.Clamp( lodLevel, 0, _lodLevelCount - 1 );

		// Fetch spacing and sides multiplier for this LOD
		int spacing = lodLevel switch
		{
			0 => LODSpacings0,
			1 => LODSpacings1,
			2 => LODSpacings2,
			3 => LODSpacings3,
			_ => 1
		};

		float sidesMultiplier = lodLevel switch
		{
			0 => LODSidesMultiplier0,
			1 => LODSidesMultiplier1,
			2 => LODSidesMultiplier2,
			3 => LODSidesMultiplier3,
			_ => 1f
		};

		var lodSplinePoints = new List<Vector3>();									// Generate LOD points
		for ( int i = 0; i < splinePoints.Count; i += spacing )
			lodSplinePoints.Add( splinePoints[i] );

		if ( !lodSplinePoints.Contains( splinePoints[^1] ) )						// Ensure last point is included
			lodSplinePoints.Add( splinePoints[^1] );

		int sidesForLOD = (int)Math.Clamp( Sides * sidesMultiplier, 3, 64 );		// Generate mesh with dynamic sides
		GenerateMesh( lodSplinePoints, sidesForLOD );
	}

	private void GenerateMesh( List<Vector3> points, int sidesForLOD )
	{
		if ( points.Count < 2 )
			return;

		var material = CableMaterial != null ? Material.Load( CableMaterial.ResourcePath )
											 : Material.Load( "materials/dev/dev_texture_surface_concrete1_tinted.vmat" );
		var mesh = new Mesh( material );
		var vb = new VertexBuffer();
		vb.Init( true );

		float radius = Radius * 0.5f;
		int segmentCount = points.Count;

		List<float> originalLengths = ComputeCumulativeLengths( splinePoints );

		// Generate vertices
		for ( int i = 0; i < segmentCount; i++ )
		{
			var center = points[i];
			Vector3 tangent = (i < segmentCount - 1)
				? (points[i + 1] - points[i]).Normal
				: (i > 0 ? (points[i] - points[i - 1]).Normal : Vector3.Zero);

			Vector3 bitangent = MathF.Abs( Vector3.Dot( tangent, Vector3.Up ) ) > 0.95f
				? Vector3.Forward
				: Vector3.Up;

			Vector3 normal = Vector3.Cross( tangent, bitangent ).Normal;
			bitangent = Vector3.Cross( tangent, normal ).Normal;

			for ( int j = 0; j < sidesForLOD - 1; j++ )
			{
				float angle = j / (float)(sidesForLOD - 1) * MathF.Tau;
				Vector3 offset = normal * MathF.Cos( angle ) + bitangent * MathF.Sin( angle );
				Vector3 position = center + offset * radius;

				Vector3 _normalVec = offset.Normal;
				Vector3 _tangentVec = Vector3.Cross( _normalVec, tangent ).Normal;
				Vector4 _tangent = new Vector4( _tangentVec, -1.0f );

				Vector2 uv = GetUVMapping( i, j, points, sidesForLOD, splinePoints, originalLengths );

				vb.Add( new Vertex
				{
					Position = position,
					Normal = _normalVec,
					Tangent = _tangent,
					TexCoord0 = uv
				} );
			}
		}

		int vertsPerRing = sidesForLOD - 1;

		// Generate triangles
		for ( int i = 0; i < segmentCount - 1; i++ )
		{
			int ringStart = i * vertsPerRing;
			int nextRingStart = (i + 1) * vertsPerRing;

			for ( int j = 0; j < vertsPerRing; j++ )
			{
				int a = ringStart + j;
				int b = ringStart + (j + 1) % vertsPerRing;
				int c = nextRingStart + j;
				int d = nextRingStart + (j + 1) % vertsPerRing;

				vb.AddRawIndex( a ); vb.AddRawIndex( b ); vb.AddRawIndex( c );
				vb.AddRawIndex( b ); vb.AddRawIndex( d ); vb.AddRawIndex( c );
			}
		}

		mesh.CreateBuffers( vb );

		var model = new ModelBuilder().AddMesh( mesh ).Create();

		if ( cableObject == null )
		{
			cableObject = new SceneObject( Scene.SceneWorld, model, new Transform( Vector3.Zero, Rotation.Identity ) );
		}
		else
		{
			cableObject.Model = model;
		}
	}

	private Vector2 GetUVMapping( int segmentIndex, int sideIndex, List<Vector3> lodSplinePoints, int sidesForLOD, List<Vector3> originalSplinePoints, List<float> originalLengths )
	{
		float trueLength = GetOriginalSplineDistanceAt(
			lodSplinePoints[segmentIndex],
			originalSplinePoints,
			originalLengths
		);

		float totalLength = originalLengths[^1];
		float scaleMultiplier = 100f;
		float along = (trueLength / totalLength) * (TextureScale * scaleMultiplier) + TextureOffsetAlong;

		float around = sideIndex / (float)(sidesForLOD - 1);
		if ( sideIndex == sidesForLOD - 1 ) around = 1f;

		around = around * TextureRepeatCircumference + TextureOffsetAround;

		return (TexOrientation == TextureOrientation.Horizontal)
			? new Vector2( around, along )
			: new Vector2( along, around );
	}

	private List<float> ComputeCumulativeLengths( List<Vector3> points )
	{
		var lengths = new List<float> { 0f };
		for ( int i = 1; i < points.Count; i++ )
		{
			float segmentLength = Vector3.DistanceBetween( points[i - 1], points[i] );
			lengths.Add( lengths[^1] + segmentLength );
		}
		return lengths;
	}

}
