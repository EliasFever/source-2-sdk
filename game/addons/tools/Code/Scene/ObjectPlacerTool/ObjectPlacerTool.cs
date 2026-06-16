namespace Editor;

using System;

[Title( "Object Placer Tool" )]
[Icon( "playlist_add" )]
[EditorTool( "tools.object-placer-tool" )]
[Alias( "tools.object-placer-tool" )]
[Group( "Tools" )]
public partial class ObjectPlacerTool : EditorTool
{
	private ObjectPlacerConfig _settings;
	private ObjectPlacerEntry _selectedEntry;
	private TypeDescription _selectedDerivedComponentType;
	private float _clickPulseTimer = -1f;
	private Vector3 _clickPulsePosition;
	private Vector3 _clickPulseNormal = Vector3.Up;

	private static readonly Color PlacementPreviewStartColor = Color.Parse( "#ffee00FF" )!.Value;
	private static readonly Color PlacementPreviewEndColor = Color.Parse( "#ffee0079" )!.Value;
	private const string SettingsCookie = "ObjectPlacerTool";

	public ObjectPlacerTool()
	{
		RebuildSidebarOnSelectionChange = false;
	}

	public Type SelectedType
	{
		get => _selectType;
		set
		{
			_selectType = value;
			_selectedDerivedComponentType = value is null ? null : EditorTypeLibrary.GetType( value );
			_selectedEntry = null;
			_browser?.Update();
		}
	}

	[Range( 0, 360 ), Step( 1 )]
	public float YawRotation
	{
		get => _yawRotation;
		set
		{
			_yawRotation = SnapToStep( value, EditorScene.GizmoSettings.AngleSpacing );
			PersistPlacementSettings();
		}
	}
	//Temp solution, use rotation snap when changing that value
	private static float SnapToStep( float value, float step )
	{
		if ( step <= 0f ) return value;
		return MathF.Round( value / step ) * step;
	}

	[Description( "Offset distance from surface normal if 'use surface normal' is selected, otherwise uses vertical offset" )]
	public float DistanceOffset
	{
		get => _distanceOffset;
		set
		{
			_distanceOffset = value;
			PersistPlacementSettings();
		}
	}

	public bool SelectWhenPlaced
	{
		get => _selectWhenPlaced;
		set
		{
			_selectWhenPlaced = value;
			PersistPlacementSettings();
		}
	}

	[Description( "Uses surface normal for distance offset only" )]
	public bool UseSurfaceNormal
	{
		get => _useSurfaceNormal;
		set
		{
			_useSurfaceNormal = value;
			PersistPlacementSettings();
		}
	}
	private static float _yawRotation = 0f;
	private static float _distanceOffset = 0f;
	public static bool _selectWhenPlaced = true;
	private static bool _useSurfaceNormal = false;
	private static Type _selectType = null;

	public override void OnEnabled()
	{
		base.OnEnabled();
		AllowGameObjectSelection = false;
		Selection.Clear();
		_settings = ObjectPlacerConfig.Load();
		LoadPlacementSettings();

	}

	private static void PersistPlacementSettings()
	{
		ProjectCookie.Set<float>( $"{SettingsCookie}.YawRotation", _yawRotation );
		ProjectCookie.Set<float>( $"{SettingsCookie}.DistanceOffset", _distanceOffset );
		ProjectCookie.Set<bool>( $"{SettingsCookie}.SelectWhenPlaced", _selectWhenPlaced );
		ProjectCookie.Set<bool>( $"{SettingsCookie}.UseSurfaceNormal", _useSurfaceNormal );
		ProjectCookie.Set<int>( $"{SettingsCookie}.ViewMode", _selectedViewMode );
	}

	private static void LoadPlacementSettings()
	{
		_yawRotation = SnapToStep( ProjectCookie.Get<float>( $"{SettingsCookie}.YawRotation", 0f ), EditorScene.GizmoSettings.AngleSpacing );
		_distanceOffset = ProjectCookie.Get<float>( $"{SettingsCookie}.DistanceOffset", 0f );
		_selectWhenPlaced = ProjectCookie.Get<bool>( $"{SettingsCookie}.SelectWhenPlaced", true );
		_useSurfaceNormal = ProjectCookie.Get<bool>( $"{SettingsCookie}.UseSurfaceNormal", false );
		_selectedViewMode = Math.Clamp( ProjectCookie.Get<int>( $"{SettingsCookie}.ViewMode", 1 ), 0, 2 );
	}

	public override void OnUpdate()
	{
		base.OnUpdate();

		var target = GetPlacementTarget();
		if ( target is null )
			return;

		var trace = MeshTrace.Run();
		if ( !trace.Hit )
			return;

		var position = GetPlacementPosition( trace );
		var normal = GetPlacementNormal( trace );

		DrawPulsingCircle( position, normal, 3f );
		DrawClickPulse( _clickPulsePosition, _clickPulseNormal, 3f, ref _clickPulseTimer );

		if ( !Gizmo.WasLeftMousePressed || Gizmo.Pressed.Any )
			return;

		Place( target, trace );
		_clickPulsePosition = position;
		_clickPulseNormal = normal;
		_clickPulseTimer = 0f;
	}

	private object GetPlacementTarget()
	{
		if ( _selectedEntry is not null )
			return _selectedEntry;

		if ( _selectedDerivedComponentType is not null )
			return _selectedDerivedComponentType;

		if ( _selectType is not null )
			return EditorTypeLibrary.GetType( _selectType );

		return null;
	}

	internal void SelectEntry( ObjectPlacerEntry entry )
	{
		_selectedEntry = entry;
		_selectedDerivedComponentType = null;
		_selectType = null;
		UpdateClassPickerButton();
		_browser?.Update();
	}

	private void Place( object target, SceneTraceResult trace )
	{
		using var scene = SceneEditorSession.Scope();
		using var undo = SceneEditorSession.Active.UndoScope( "Place Object" ).WithGameObjectCreations().Push();

		var go = target switch
		{
			ObjectPlacerEntry entry => CreateFromEntry( entry ),
			TypeDescription componentType => CreateComponentObject( componentType, componentType.Title ),
			_ => null
		};

		if ( !go.IsValid() )
			return;

		go.WorldPosition = GetPlacementPosition( trace );
		go.WorldRotation = Rotation.FromYaw( YawRotation );
		go.MakeNameUnique();

		if ( SelectWhenPlaced )
		{
			Selection.Clear();
			Selection.Add( go );
		}

	}

	private Vector3 GetPlacementPosition( SceneTraceResult trace )
	{
		var position = UseSurfaceNormal
			? trace.HitPosition + GetPlacementNormal( trace ) * DistanceOffset
			: trace.HitPosition + Vector3.Up * DistanceOffset;

		return SnapPlacementPosition( position );
	}

	private static Vector3 SnapPlacementPosition( Vector3 position )
	{
		if ( Gizmo.Settings.SnapToGrid == Gizmo.IsCtrlPressed )
			return position;

		var spacing = Gizmo.Settings.GridSpacing;
		if ( spacing <= 0f )
			return position;

		return position.SnapToGrid( spacing, true, true, true );
	}

	private static Vector3 GetPlacementNormal( SceneTraceResult trace )
	{
		return trace.Normal.Length > 0.001f ? trace.Normal.Normal : Vector3.Up;
	}

	private static void DrawPulsingCircle( Vector3 position, Vector3 normal, float baseRadius, int sections = 32 )
	{
		var rawPulse = 0.5f + 0.5f * MathF.Sin( (float)Time.Now * 0.75f * MathF.PI * 2 );
		var easedPulse = EaseInOutSine( rawPulse );
		var radius = baseRadius * (1.05f + 0.5f * easedPulse);

		Gizmo.Draw.Color = Color.Lerp( PlacementPreviewStartColor, PlacementPreviewEndColor, easedPulse );
		using var scope = Gizmo.Scope( "object_placer_preview", new Transform( position, Rotation.LookAt( normal ) ) );
		Gizmo.Draw.LineCircle( 0, radius, sections: sections );
	}

	private static void DrawClickPulse( Vector3 position, Vector3 normal, float baseRadius, ref float timer, float duration = 0.2f, int sections = 32 )
	{
		if ( timer < 0f )
			return;

		timer += Time.Delta;
		var t = MathF.Min( timer / duration, 1f );
		var radiusMultiplier = 1f + 0.5f * (1f - t);

		Gizmo.Draw.Color = Color.Lerp( PlacementPreviewStartColor, PlacementPreviewEndColor, 1f - t );
		using var scope = Gizmo.Scope( "object_placer_click_pulse", new Transform( position, Rotation.LookAt( normal ) ) );
		Gizmo.Draw.LineCircle( 0, baseRadius * radiusMultiplier, sections: sections );

		if ( t >= 1f )
			timer = -1f;
	}

	private static float EaseInOutSine( float value )
	{
		return -(MathF.Cos( MathF.PI * value ) - 1f) / 2f;
	}

	private static GameObject CreateFromEntry( ObjectPlacerEntry entry )
	{
		if ( entry is null )
			return null;

		if ( entry.Kind == ObjectPlacerEntryKind.Prefab )
			return CreatePrefabObject( entry );

		var componentType = FindComponentType( entry.ComponentTypeName );
		if ( componentType is null )
			return null;

		var go = CreateComponentObject( componentType, entry.Name );
		ApplyPropertyOverrides( go, componentType, entry );
		return go;
	}

	private static GameObject CreateComponentObject( TypeDescription componentType, string name )
	{
		if ( componentType is null || !typeof( Component ).IsAssignableFrom( componentType.TargetType ) )
			return null;

		var go = new GameObject( true, string.IsNullOrWhiteSpace( name ) ? componentType.Title : name );
		go.Components.Create( componentType );
		return go;
	}

	private static GameObject CreatePrefabObject( ObjectPlacerEntry entry )
	{
		if ( !entry.Prefab.IsValid() )
			return null;

		var prefabScene = SceneUtility.GetPrefabScene( entry.Prefab );
		return prefabScene?.Clone();
	}

	private static void ApplyPropertyOverrides( GameObject go, TypeDescription componentType, ObjectPlacerEntry entry )
	{
		if ( !go.IsValid() || entry.PropertyOverrides is null || entry.PropertyOverrides.Count == 0 )
			return;

		var component = go.Components.Get( componentType.TargetType );
		if ( component is null )
			return;

		foreach ( var propertyOverride in entry.PropertyOverrides )
		{
			if ( string.IsNullOrWhiteSpace( propertyOverride.PropertyName ) )
				continue;

			var property = FindProperty( component, propertyOverride.PropertyName );
			if ( property is null )
				continue;

			ApplyPropertyOverride( property, propertyOverride );
		}
	}

	private static void ApplyPropertyOverride( SerializedProperty property, ObjectPlacerPropertyOverride propertyOverride )
	{
		if ( typeof( Resource ).IsAssignableFrom( property.PropertyType ) )
		{
			var path = propertyOverride.ResourcePath ?? propertyOverride.Value;
			if ( string.IsNullOrWhiteSpace( path ) )
				return;

			var asset = AssetSystem.FindByPath( path );
			var resource = asset?.LoadResource( property.PropertyType );
			if ( resource is not null )
				property.SetValue( resource );

			return;
		}

		if ( string.IsNullOrWhiteSpace( propertyOverride.Value ) )
			return;

		if ( TryParseValue( propertyOverride.Value, property.PropertyType, out var value ) )
			property.SetValue( value );
	}

	private static bool TryParseValue( string text, Type type, out object value )
	{
		value = null;

		try
		{
			if ( type == typeof( string ) )
			{
				value = text;
				return true;
			}

			if ( type == typeof( bool ) && bool.TryParse( text, out var boolValue ) )
			{
				value = boolValue;
				return true;
			}

			if ( type == typeof( int ) && int.TryParse( text, out var intValue ) )
			{
				value = intValue;
				return true;
			}

			if ( type == typeof( float ) && float.TryParse( text, out var floatValue ) )
			{
				value = floatValue;
				return true;
			}

			if ( type == typeof( double ) && double.TryParse( text, out var doubleValue ) )
			{
				value = doubleValue;
				return true;
			}

			if ( type.IsEnum )
			{
				value = Enum.Parse( type, text );
				return true;
			}
		}
		catch
		{
			return false;
		}

		return false;
	}

	private static SerializedProperty FindProperty( Component component, string propertyName )
	{
		var so = component.GetSerialized();

		if ( !string.IsNullOrWhiteSpace( propertyName ) )
			return so.GetProperty( propertyName );

		foreach ( var prop in so )
			return prop;

		return null;
	}

	private static TypeDescription FindComponentType( string typeName )
	{
		if ( string.IsNullOrWhiteSpace( typeName ) )
			return null;

		return EditorTypeLibrary.GetTypes<Component>()
			.FirstOrDefault( t => t.Name.Equals( typeName, StringComparison.OrdinalIgnoreCase )
				|| t.FullName.Equals( typeName, StringComparison.OrdinalIgnoreCase )
				|| t.ClassName.Equals( typeName, StringComparison.OrdinalIgnoreCase )
				|| t.TargetType.Name.Equals( typeName, StringComparison.OrdinalIgnoreCase )
				|| t.TargetType.FullName.Equals( typeName, StringComparison.OrdinalIgnoreCase ) );
	}
}

