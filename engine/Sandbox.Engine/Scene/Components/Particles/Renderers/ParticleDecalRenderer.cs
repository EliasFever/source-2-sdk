namespace Sandbox;

[Expose]
[Title( "Particle Decal Renderer" )]
[Category( "Particles" )]
[Icon( "lens_blur" )]
public sealed class ParticleDecalRenderer : ParticleController, Component.ExecuteInEditor
{
	public enum DecalSelectionMode
	{
		Random,
		Next,
		Previous
	}

	public enum DecalSortMode
	{
		Stable,
		NewestOnTop,
		OldestOnTop
	}

	[Property, WideMode, Header( "General" )]
	public List<DecalDefinition> Decals { get; set; } = [];

	[Property]
	public DecalSelectionMode SelectionMode { get; set; } = DecalSelectionMode.Random;

	[Property, Header( "Projection" )]
	public ParticleFloat Scale { get; set; } = 1.0f;

	[Property]
	public ParticleFloat Rotation { get; set; } = 0.0f;

	[Property, Range( 0.0f, 8.0f )]
	public float SurfaceBias { get; set; } = 0.5f;

	[Property]
	public float MinProjectionDepth { get; set; } = -8f;

	[Property]
	public float MaxProjectionDepth { get; set; } = 8f;

	[Property, Header( "Visuals" )]
	public Color ConstantColorTint { get; set; } = Color.White;

	[Property]
	public ParticleGradient ColorTint { get; set; } = Color.White;

	[Property]
	public ParticleFloat Brightness { get; set; } = 1.0f;

	[Property, Range( 0, 1 )]
	public ParticleFloat Alpha { get; set; } = 1.0f;

	[Property, Range( 0, 1 )]
	public ParticleFloat ColorMix { get; set; } = 1.0f;

	[Property]
	public ParticleFloat Parallax { get; set; } = 1.0f;

	[Property, Range( 0, 1 )]
	public float AttenuationAngle { get; set; } = 1.0f;

	[Property, Header( "Pseudo Bump" )]
	public float PseudoBumpStrength { get; set; } = 1.0f;

	[Property, Range( 0.0f, 1.0f )]
	public float PseudoBumpColorSuppression { get; set; } = 0.5f;

	[Property, Range( 0.0f, 4.0f )]
	public float PseudoBumpParallaxBoost { get; set; } = 1.0f;

	[Title( "Surface Variation" )]
	[Property, FeatureEnabled( "SurfaceVariation", Icon = "shuffle" )]
	public bool SurfaceVariation { get; set; }

	[Property, Feature( "SurfaceVariation" ), Range( 0.0f, 180.0f )]
	public float SurfaceYawJitter { get; set; } = 0.0f;

	[Property, Feature( "SurfaceVariation" ), Range( 0.0f, 1.0f )]
	public float SurfaceScaleJitter { get; set; } = 0.0f;

	[Property, Feature( "SurfaceVariation" ), Range( 0.0f, 1.0f )]
	public float SurfaceBrightnessJitter { get; set; } = 0.0f;

	[Property, Feature( "SurfaceVariation" ), Range( 0.0f, 1.0f )]
	public float SurfaceColorMixJitter { get; set; } = 0.0f;

	[Property, Feature( "SurfaceVariation" ), Range( 0.0f, 1.0f )]
	public float SurfaceParallaxJitter { get; set; } = 0.0f;

	[Property, Group( "Collision" )]
	public bool BlockSpawnIfParticleWouldDieOnCollision { get; set; } = false;

	[Property, Group( "Collision" )]
	public bool SpawnNewDecalOnEveryCollision { get; set; } = false;

	[Property, Group( "Collision" )]
	public bool StickToHitObjects { get; set; } = true;

	[Property, Group( "Collision" )]
	public bool AllowMultipleActiveDecals { get; set; } = false;

	[Property, Group( "Collision" ), Range( 1, 512 )]
	public int MaxDecalsPerParticle { get; set; } = 8;

	[Property, Group( "Collision" )]
	public bool PersistDecalAfterParticleDeath { get; set; } = false;

	[Property, Group( "Collision" )]
	public bool ForceSpawnOnDisableCollision { get; set; } = true;

	[Property, Group( "Collision" ), Range( 0.0f, 100f )]
	public ParticleFloat DecalLifetime { get; set; } = 8.0f;

	[Title( "Persistent Fade" )]
	[Property, Group( "Collision" ), FeatureEnabled( "PersistentFade", Icon = "animation" )]
	public bool PersistentFade { get; set; } = false;

	[Property, Feature( "PersistentFade" )]
	public ParticleFloat PersistentAlphaOverLife { get; set; } = new ParticleFloat( 1, 0 );

	[Property, Group( "Projection Filters" )]
	public TagSet IncludeHitTags { get; set; } = new();

	[Property, Group( "Projection Filters" )]
	public TagSet ExcludeHitTags { get; set; } = new();

	[Property, Group( "Sorting" )]
	public DecalSortMode SortMode { get; set; } = DecalSortMode.NewestOnTop;

	[Property, Group( "Sorting" ), Range( 0, 255 )]
	public uint SortLayer { get; set; } = 0;

	[Property, Group( "Sorting" ), Range( 1, 255 )]
	public uint SortPerDecalStep { get; set; } = 1;

	[Property, Group( "Sorting" )]
	public bool SortPerHitObject { get; set; } = true;

	[Property, Group( "Performance" ), Range( 1, 8192 )]
	public int MaxTotalDecals { get; set; } = 512;

	[Property, Group( "Performance" ), Range( 1, 4096 )]
	public int MaxPersistentDecals { get; set; } = 128;

	[Title( "Distance Culling" )]
	[Property, FeatureEnabled( "DistanceCulling", Icon = "near_me" )]
	public bool EnableDistanceCulling { get; set; } = false;

	[Property, Feature( "DistanceCulling" ), Range( 0.0f, 50000.0f )]
	public float MaxDecalDrawDistance { get; set; } = 5000.0f;

	[Property, Feature( "DistanceCulling" )]
	public bool CullPersistentByDistance { get; set; } = true;

	[Property, Feature( "DistanceCulling" )]
	public bool CullActiveByDistance { get; set; } = false;

	[Property, Group( "Debug" )]
	public bool EnableDebug { get; set; }

	[Property, Group( "Debug" )]
	public bool DebugDrawGizmos { get; set; } = true;

	[Property, Group( "Debug" )]
	public bool DebugDrawVolume { get; set; } = true;

	private int _sequenceIndex = -1;

	private readonly object _debugSync = new();
	private Vector3 _dbgParticlePos;
	private Vector3 _dbgHitPos;
	private Vector3 _dbgHitNormal;
	private Vector3 _dbgLockedPos;
	private Vector3 _dbgLockedNormal;
	private Vector3 _dbgVolumeSize;
	private Rotation _dbgRotation;
	private bool _dbgHasCollision;
	private bool _dbgPassedFilters;
	private bool _dbgLocked;
	private bool _dbgVisible;
	private string _dbgStatus = "none";
	private string _dbgHitObjectName = "<none>";

	private readonly object _persistentSync = new();
	private readonly List<PersistentDecal> _persistentDecals = new();

	private readonly object _trackedSync = new();
	private readonly List<TrackedDecal> _trackedDecals = new();

	private sealed class PersistentDecal
	{
		public DecalSceneObject SceneObject;
		public float StartAt;
		public float ExpireAt;
		public float LifeTime;
		public Color BaseColor;
		public uint Seed;
	}

	private sealed class TrackedDecal
	{
		public DecalSceneObject SceneObject;
		public float SpawnAt;
	}

	internal DecalDefinition SelectDecal( Particle p )
	{
		if ( Decals is null || Decals.Count == 0 )
			return null;

		if ( Decals.Count == 1 )
			return Decals[0];

		switch ( SelectionMode )
		{
			case DecalSelectionMode.Next:
				_sequenceIndex = _sequenceIndex < 0 ? 0 : (_sequenceIndex + 1) % Decals.Count;
				return Decals[_sequenceIndex];

			case DecalSelectionMode.Previous:
				_sequenceIndex = _sequenceIndex < 0 ? Decals.Count - 1 : (_sequenceIndex - 1 + Decals.Count) % Decals.Count;
				return Decals[_sequenceIndex];

			default:
				return Decals[(int)(p.Rand( 123 ) * Decals.Count) % Decals.Count];
		}
	}

	internal void RegisterActiveDecal( DecalSceneObject sceneObject )
	{
		if ( sceneObject is null || !sceneObject.IsValid() )
			return;

		lock ( _trackedSync )
		{
			RegisterTracked_NoLock( sceneObject );
			CullTrackedByBudget_NoLock();
		}
	}

	internal void UnregisterActiveDecal( DecalSceneObject sceneObject )
	{
		if ( sceneObject is null )
			return;

		lock ( _trackedSync )
		{
			for ( int i = _trackedDecals.Count - 1; i >= 0; i-- )
			{
				if ( ReferenceEquals( _trackedDecals[i].SceneObject, sceneObject ) )
					_trackedDecals.RemoveAt( i );
			}
		}
	}

	internal bool ShouldCullByDistance( Vector3 worldPos, bool persistent )
	{
		if ( !EnableDistanceCulling )
			return false;

		if ( persistent && !CullPersistentByDistance )
			return false;

		if ( !persistent && !CullActiveByDistance )
			return false;

		if ( MaxDecalDrawDistance <= 0.0f )
			return false;

		var cam = Scene?.Camera;
		if ( cam is null )
			return false;

		return worldPos.Distance( cam.WorldPosition ) > MaxDecalDrawDistance;
	}

	internal void AdoptPersistentDecal( DecalSceneObject sceneObject, float lifeTime, uint seed = 0 )
	{
		if ( sceneObject is null || !sceneObject.IsValid() )
			return;

		var now = Time.Now;
		var expireAt = lifeTime > 0.0f ? now + lifeTime : float.PositiveInfinity;
		var resolvedSeed = seed != 0 ? seed : (uint)(now * 1000.0f);

		lock ( _persistentSync )
		{
			_persistentDecals.Add( new PersistentDecal
			{
				SceneObject = sceneObject,
				StartAt = now,
				ExpireAt = expireAt,
				LifeTime = lifeTime,
				BaseColor = sceneObject.Color,
				Seed = resolvedSeed
			} );

			CullPersistentByCount_NoLock();
		}

		lock ( _trackedSync )
		{
			RegisterTracked_NoLock( sceneObject );
			CullTrackedByBudget_NoLock();
		}
	}

	protected override void OnParticleCreated( Particle p )
	{
		var selected = SelectDecal( p );
		if ( selected is null )
			return;

		p.AddListener( new ParticleDecal( this, selected ), this );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		var now = Time.Now;

		lock ( _persistentSync )
		{
			for ( int i = _persistentDecals.Count - 1; i >= 0; i-- )
			{
				var p = _persistentDecals[i];
				if ( p.SceneObject is null || !p.SceneObject.IsValid() )
				{
					_persistentDecals.RemoveAt( i );
					continue;
				}

				if ( ShouldCullByDistance( p.SceneObject.Transform.Position, persistent: true ) )
				{
					p.SceneObject.Delete();
					_persistentDecals.RemoveAt( i );
					continue;
				}

				if ( PersistentFade && p.LifeTime > 0.0f && float.IsFinite( p.ExpireAt ) )
				{
					float d = Math.Clamp( (now - p.StartAt) / p.LifeTime, 0.0f, 1.0f );
					float alphaMul = PersistentAlphaOverLife.Evaluate( d, (int)p.Seed );
					if ( !float.IsFinite( alphaMul ) ) alphaMul = 1.0f;
					alphaMul = Math.Clamp( alphaMul, 0.0f, 1.0f );
					p.SceneObject.Color = p.BaseColor.WithAlpha( Math.Clamp( p.BaseColor.a * alphaMul, 0.0f, 1.0f ) );
				}

				if ( now >= p.ExpireAt )
				{
					p.SceneObject.Delete();
					_persistentDecals.RemoveAt( i );
				}
			}

			CullPersistentByCount_NoLock();
		}

		lock ( _trackedSync )
		{
			CullTrackedByBudget_NoLock();
		}
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		lock ( _persistentSync )
		{
			for ( int i = 0; i < _persistentDecals.Count; i++ )
			{
				var p = _persistentDecals[i];
				if ( p.SceneObject is not null && p.SceneObject.IsValid() )
					p.SceneObject.Delete();
			}

			_persistentDecals.Clear();
		}

		lock ( _trackedSync )
		{
			for ( int i = 0; i < _trackedDecals.Count; i++ )
			{
				var d = _trackedDecals[i];
				if ( d.SceneObject is not null && d.SceneObject.IsValid() )
					d.SceneObject.Delete();
			}

			_trackedDecals.Clear();
		}
	}

	private void RegisterTracked_NoLock( DecalSceneObject sceneObject )
	{
		for ( int i = 0; i < _trackedDecals.Count; i++ )
		{
			if ( ReferenceEquals( _trackedDecals[i].SceneObject, sceneObject ) )
				return;
		}

		_trackedDecals.Add( new TrackedDecal
		{
			SceneObject = sceneObject,
			SpawnAt = Time.Now
		} );
	}

	private void CullTrackedByBudget_NoLock()
	{
		for ( int i = _trackedDecals.Count - 1; i >= 0; i-- )
		{
			if ( _trackedDecals[i].SceneObject is null || !_trackedDecals[i].SceneObject.IsValid() )
				_trackedDecals.RemoveAt( i );
		}

		var max = Math.Max( MaxTotalDecals, 1 );
		while ( _trackedDecals.Count > max )
		{
			var oldest = _trackedDecals[0];
			if ( oldest.SceneObject is not null && oldest.SceneObject.IsValid() )
				oldest.SceneObject.Delete();
			_trackedDecals.RemoveAt( 0 );
		}
	}

	private void CullPersistentByCount_NoLock()
	{
		var max = Math.Max( MaxPersistentDecals, 1 );
		while ( _persistentDecals.Count > max )
		{
			var oldest = _persistentDecals[0];
			if ( oldest.SceneObject is not null && oldest.SceneObject.IsValid() )
				oldest.SceneObject.Delete();
			_persistentDecals.RemoveAt( 0 );
		}
	}

	protected override void DrawGizmos()
	{
		if ( !EnableDebug || !DebugDrawGizmos )
			return;

		Vector3 particlePos;
		Vector3 hitPos;
		Vector3 hitNormal;
		Vector3 lockedPos;
		Vector3 lockedNormal;
		Vector3 volumeSize;
		Rotation rotation;
		bool hasCollision;
		bool locked;
		bool visible;
		bool passed;

		lock ( _debugSync )
		{
			particlePos = _dbgParticlePos;
			hitPos = _dbgHitPos;
			hitNormal = _dbgHitNormal;
			lockedPos = _dbgLockedPos;
			lockedNormal = _dbgLockedNormal;
			volumeSize = _dbgVolumeSize;
			rotation = _dbgRotation;
			hasCollision = _dbgHasCollision;
			locked = _dbgLocked;
			visible = _dbgVisible;
			passed = _dbgPassedFilters;
		}

		var world = WorldTransform;
		var particlePosL = world.PointToLocal( particlePos );
		var hitPosL = world.PointToLocal( hitPos );
		var hitNormalL = world.NormalToLocal( hitNormal ).Normal;
		var lockedPosL = world.PointToLocal( lockedPos );
		var lockedNormalL = world.NormalToLocal( lockedNormal ).Normal;
		var lockedRotL = world.RotationToLocal( rotation );

		Gizmo.Draw.LineThickness = 1;

		Gizmo.Draw.Color = Color.Cyan;
		Gizmo.Draw.LineSphere( particlePosL, 1.5f );

		if ( hasCollision )
		{
			Gizmo.Draw.Color = passed ? Color.Yellow : Color.Red;
			Gizmo.Draw.LineSphere( hitPosL, 2.0f );
			Gizmo.Draw.Line( hitPosL, hitPosL + hitNormalL * 14.0f );
		}

		if ( locked )
		{
			Gizmo.Draw.Color = visible ? Color.Green : Color.Orange;
			Gizmo.Draw.LineSphere( lockedPosL, 2.5f );
			Gizmo.Draw.Line( lockedPosL, lockedPosL + lockedNormalL * 18.0f );

			if ( DebugDrawVolume )
			{
				using ( Gizmo.Scope() )
				{
					Gizmo.Transform = new Transform( lockedPosL, lockedRotL );
					Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( Vector3.Zero, volumeSize ) );
				}
			}
		}
	}
}
