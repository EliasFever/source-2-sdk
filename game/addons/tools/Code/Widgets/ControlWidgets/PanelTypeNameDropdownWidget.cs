using Sandbox;
using System;

namespace Editor;

/// <summary>
/// Dropdown widget for selecting a panel type by name, has a search field.
/// </summary>
[CustomEditor( typeof( string ), WithAllAttributes = new[] { typeof( PanelTypeDropdownAttribute ) } )]
public sealed class PanelTypeDropdownWidget : DropdownControlWidget<string>
{
	readonly Type _baseType;

	public PanelTypeNameDropdownWidget( SerializedProperty property ) : base( property )
	{
		property.TryGetAttribute<PanelTypeDropdownAttribute>( out var attr );
		if ( attr is null )
			throw new Exception( "PanelTypeNameDropdownWidget property has no PanelTypeDropdownAttribute" );
		_baseType = attr.BaseType ?? typeof( Sandbox.UI.Panel );
	}

	protected override bool EnableSearch => true;
	protected override string SearchPlaceholder => "Search panels...";

	protected override string GetDisplayText()
	{
		if ( SerializedProperty.IsMultipleDifferentValues )
			return "Multiple Values";

		var value = SerializedProperty.GetValue<string>( null );
		if ( string.IsNullOrWhiteSpace( value ) )
			return "Default";

		var type = EditorTypeLibrary.GetType( value );
		if ( type is not null )
			return type.Title;

		return value;
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		var currentProjectAssemblyName = GetCurrentProjectAssemblyName();

		yield return new Entry
		{
			Value = null,
			Label = "Default",
			Description = "Use the base loading overlay.",
			Icon = "block"
		};

		foreach ( var type in EditorTypeLibrary.GetTypes( _baseType ).OrderBy( x => x.Title ) )
		{
			if ( type.IsAbstract ) continue;
			if ( !type.TargetType.IsAssignableTo( _baseType ) ) continue;

			var fullName = type.TargetType.FullName;
			if ( string.IsNullOrWhiteSpace( fullName ) )
				continue;

			// Only show panels from the current project assembly (plus the explicit base default entry above).
			if ( !string.IsNullOrWhiteSpace( currentProjectAssemblyName ) )
			{
				var asmName = type.TargetType.Assembly?.GetName()?.Name;
				if ( !string.Equals( asmName, currentProjectAssemblyName, StringComparison.OrdinalIgnoreCase ) )
					continue;
			}

			yield return new Entry
			{
				Value = fullName,
				Label = type.Title,
				Icon = type.Icon ?? "widgets",
				Description = fullName
			};
		}
	}

	static string GetCurrentProjectAssemblyName()
	{
		// In the editor, project types compile into an assembly named "package.<org>.<ident>".
		// Example: local.sweeper -> package.local.sweeper
		var proj = Sandbox.Project.Current;
		if ( proj is null ) return null;

		var org = proj.Config?.Org?.Trim();
		var ident = proj.Config?.Ident?.Trim();
		if ( string.IsNullOrWhiteSpace( org ) || string.IsNullOrWhiteSpace( ident ) )
			return null;

		return $"package.{org}.{ident}";
	}

	protected override void OnItemSelected( object item )
	{
		if ( item is Entry e )
		{
			SerializedProperty.SetValue( e.Value );
			return;
		}

		SerializedProperty.SetValue( item as string );
	}
}
