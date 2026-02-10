namespace Editor.Preferences;

internal partial class PageInterface
{
	// Populate this!
	static void RegisterGroups() 
	{ 
		RegisterGroup( ToolbarsGroup ); 
	}

	// John: Free example for those who want to make their own groups :)
	private static void ToolbarsGroup( Layout layout )
	{
		var toolbarsGroup = new CollapsibleCategory( null, "S2 Styled Toolbars" );
		toolbarsGroup.Container.Layout.Spacing = 0;
		layout.Add( toolbarsGroup );

		AddControlSheetBoolRow( toolbarsGroup.Container.Layout, "Enable S&Box Viewport Toolbars",
			() => CustomEditorPreferences.ShowLegacyViewportToolbar,
			v => CustomEditorPreferences.ShowLegacyViewportToolbar = v );

		AddControlSheetBoolRow( toolbarsGroup.Container.Layout, "Build S2 Toolbars On Editor Startup",
			() => CustomEditorPreferences.BuildToolbarsOnStartup,
			v => CustomEditorPreferences.BuildToolbarsOnStartup = v );

		AddActionRow( toolbarsGroup.Container.Layout, "Force Rebuild S2 Toolbars", "autorenew", () => EditorToolBars.RebuildToolbars() );
	}
}
