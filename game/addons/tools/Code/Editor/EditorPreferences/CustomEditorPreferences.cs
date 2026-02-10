namespace Editor.Preferences;

/// <summary>
/// Custom editor preferences storage.
/// This is essentially an extension for editor additions that get stored in settings.
/// </summary>
public static partial class CustomEditorPreferences
{
	private const string Prefix = "custom_editor_prefs.";

	[Title( "Show Legacy Viewport Toolbar" )]
	public static bool ShowLegacyViewportToolbar
	{
		get => Get( "toolbars.show_legacy_viewport", true );
		set => Set( "toolbars.show_legacy_viewport", value );
	}

	[Title( "Build Toolbars On Startup" )]
	public static bool BuildToolbarsOnStartup
	{
		get => Get( "toolbars.build_on_startup", false );
		set => Set( "toolbars.build_on_startup", value );
	}

	public static T Get<T>( string key, T defaultValue = default )
	{
		return EditorCookie.Get( Prefix + key, defaultValue );
	}

	public static void Set<T>( string key, T value )
	{
		EditorCookie.Set( Prefix + key, value );
	}
}
