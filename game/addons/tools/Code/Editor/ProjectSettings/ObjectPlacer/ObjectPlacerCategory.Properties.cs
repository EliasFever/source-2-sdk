namespace Editor.ProjectSettingPages;

internal sealed partial class ObjectPlacerCategory
{
	void AddComponentRows( Layout parent, ObjectPlacerEntry entry )
	{
		var componentType = FindComponentType( entry.ComponentTypeName );
		if ( componentType is null )
			return;

		var availableProperties = GetEditableProperties( componentType ).ToArray();
		if ( availableProperties.Length == 0 )
			return;

		var overrides = parent.AddColumn();
		overrides.Spacing = 4;

		foreach ( var propertyOverride in entry.PropertyOverrides.ToArray() )
			AddPropertyOverrideRow( overrides, entry, componentType, propertyOverride, availableProperties );

		var addRow = parent.AddRow();
		addRow.AddStretchCell();
		addRow.Add( StyledButton( "Add Property", "add", () => AddPropertyOverride( entry, availableProperties ), primary: true ) );
	}

	void AddPropertyOverrideRow( Layout parent, ObjectPlacerEntry entry, TypeDescription componentType, ObjectPlacerPropertyOverride propertyOverride, ObjectPlacerEditablePropertyInfo[] availableProperties )
	{
		var container = parent.AddColumn();
		container.Spacing = 3;

		var row = AddLabeledRow( container, "Property", spacing: 8 );
		var serializedOverride = propertyOverride.GetSerialized();
		serializedOverride.OnPropertyChanged += changed =>
		{
			if ( changed?.Name != nameof( ObjectPlacerPropertyOverride.PropertyName ) )
				return;

			propertyOverride.ResourcePath = null;
			RebuildMaster();
			RebuildDetails();
			StateHasChanged();
		};

		row.Add( new ObjectPlacerPropertyDropdownWidget( serializedOverride.GetProperty( nameof( ObjectPlacerPropertyOverride.PropertyName ) ), availableProperties ), 1 ).FixedHeight = Theme.RowHeight;

		row.Add( DeleteButton( "Remove property", () =>
		{
			entry.PropertyOverrides.Remove( propertyOverride );
			RebuildMaster();
			RebuildDetails();
			StateHasChanged();
		} ) );

		AddPropertyValueRow( container, componentType, propertyOverride );
	}

	void AddPropertyValueRow( Layout parent, TypeDescription componentType, ObjectPlacerPropertyOverride propertyOverride )
	{
		var previewProperty = CreatePreviewProperty( componentType, propertyOverride );
		if ( previewProperty is null )
			return;

		var valueRow = parent.AddRow();
		valueRow.Spacing = 8;
		valueRow.Add( new Widget( null ) { MinimumWidth = DetailLabelWidth, MaximumWidth = DetailLabelWidth } );

		var valueWidget = ControlWidget.Create( previewProperty );
		valueWidget.MinimumWidth = 0;
		valueWidget.HorizontalSizeMode = SizeMode.CanShrink | SizeMode.CanGrow;
		valueRow.Add( valueWidget, 1 ).FixedHeight = Theme.RowHeight;
	}

	void AddPropertyOverride( ObjectPlacerEntry entry, ObjectPlacerEditablePropertyInfo[] availableProperties )
	{
		entry.PropertyOverrides.Add( new ObjectPlacerPropertyOverride
		{
			PropertyName = availableProperties.FirstOrDefault().Name
		} );
		RebuildMaster();
		RebuildDetails();
		StateHasChanged();
	}

	SerializedProperty CreatePreviewProperty( TypeDescription componentType, ObjectPlacerPropertyOverride propertyOverride )
	{
		if ( componentType is null || string.IsNullOrWhiteSpace( propertyOverride.PropertyName ) )
			return null;

		var go = new GameObject( false, "Object Placer Preview" );
		go.Flags = GameObjectFlags.NotSaved | GameObjectFlags.Hidden;
		_previewObjects.Add( go );

		var component = go.Components.Create( componentType );
		if ( component is null )
			return null;

		var so = component.GetSerialized();
		var prop = so.GetProperty( propertyOverride.PropertyName );
		if ( prop is null || !IsEditableProperty( prop ) )
			return null;

		ApplyStoredPropertyValue( prop, propertyOverride );
		so.OnPropertyChanged += changed =>
		{
			if ( changed?.Name != prop.Name )
				return;

			StorePropertyValue( prop, propertyOverride );
			StateHasChanged();
		};

		return prop;
	}

	IEnumerable<ObjectPlacerEditablePropertyInfo> GetEditableProperties( TypeDescription componentType )
	{
		var go = new GameObject( false, "Object Placer Property Probe" );
		go.Flags = GameObjectFlags.NotSaved | GameObjectFlags.Hidden;
		_previewObjects.Add( go );

		var component = go.Components.Create( componentType );
		if ( component is null )
			yield break;

		foreach ( var prop in component.GetSerialized() )
		{
			if ( IsIgnoredProperty( prop ) || !IsEditableProperty( prop ) )
				continue;

			yield return new ObjectPlacerEditablePropertyInfo( prop.Name, string.IsNullOrWhiteSpace( prop.DisplayName ) ? prop.Name : prop.DisplayName );
		}
	}

	static bool IsIgnoredProperty( SerializedProperty prop )
	{
		if ( prop is null )
			return true;

		return IgnoredPropertyKeys.Contains( NormalizePropertyName( prop.Name ) )
			|| IgnoredPropertyKeys.Contains( NormalizePropertyName( prop.DisplayName ) );
	}

	static string NormalizePropertyName( string value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
			return string.Empty;

		return new string( value.Where( char.IsLetterOrDigit ).ToArray() ).ToLowerInvariant();
	}

	static bool IsEditableProperty( SerializedProperty prop )
	{
		if ( prop?.PropertyType is null )
			return false;

		var type = prop.PropertyType;
		return typeof( Resource ).IsAssignableFrom( type )
			|| type == typeof( string )
			|| type == typeof( bool )
			|| type == typeof( int )
			|| type == typeof( float )
			|| type == typeof( double )
			|| type.IsEnum;
	}

	static void ApplyStoredPropertyValue( SerializedProperty prop, ObjectPlacerPropertyOverride propertyOverride )
	{
		if ( prop is null || propertyOverride is null )
			return;

		if ( typeof( Resource ).IsAssignableFrom( prop.PropertyType ) )
		{
			propertyOverride.Kind = ObjectPlacerPropertyOverrideKind.Resource;
			var path = propertyOverride.ResourcePath ?? propertyOverride.Value;
			if ( string.IsNullOrWhiteSpace( path ) )
				return;

			var asset = AssetSystem.FindByPath( path );
			var resource = asset?.LoadResource( prop.PropertyType );
			if ( resource is not null )
				prop.SetValue( resource );

			return;
		}

		propertyOverride.Kind = ObjectPlacerPropertyOverrideKind.Value;
		if ( !string.IsNullOrWhiteSpace( propertyOverride.Value ) && TryParseValue( propertyOverride.Value, prop.PropertyType, out var value ) )
			prop.SetValue( value );
	}

	static void StorePropertyValue( SerializedProperty prop, ObjectPlacerPropertyOverride propertyOverride )
	{
		if ( prop is null || propertyOverride is null )
			return;

		if ( typeof( Resource ).IsAssignableFrom( prop.PropertyType ) )
		{
			propertyOverride.Kind = ObjectPlacerPropertyOverrideKind.Resource;
			propertyOverride.ResourcePath = prop.GetValue<Resource>()?.ResourcePath;
			propertyOverride.Value = propertyOverride.ResourcePath;
			return;
		}

		propertyOverride.Kind = ObjectPlacerPropertyOverrideKind.Value;
		propertyOverride.ResourcePath = null;
		propertyOverride.Value = prop.GetValue<object>()?.ToString();
	}

	static bool TryParseValue( string text, Type type, out object value )
	{
		value = null;

		try
		{
			if ( type == typeof( string ) )
				value = text;
			else if ( type == typeof( bool ) && bool.TryParse( text, out var boolValue ) )
				value = boolValue;
			else if ( type == typeof( int ) && int.TryParse( text, out var intValue ) )
				value = intValue;
			else if ( type == typeof( float ) && float.TryParse( text, out var floatValue ) )
				value = floatValue;
			else if ( type == typeof( double ) && double.TryParse( text, out var doubleValue ) )
				value = doubleValue;
			else if ( type.IsEnum )
				value = Enum.Parse( type, text );

			return value is not null;
		}
		catch
		{
			return false;
		}
	}

	// John: This kinda sucks, especially since some stuff doesn't exist by default.
	// But this is an easier way to ignore some stuff for now
	static readonly HashSet<string> IgnoredPropertyKeys = new( StringComparer.OrdinalIgnoreCase )
	{
		"serializedguid",
		"componentversion",
		"isproxy",
		"targetname",
		"internalid",
		"editorvis",
		"flags",
		"entitygizmosize",
		"enabled",
		"active",
		"isvalid",
		"geteditorvis"
	};
}
