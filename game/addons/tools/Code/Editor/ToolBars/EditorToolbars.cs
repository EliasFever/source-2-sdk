namespace Editor;

using Editor.MeshEditor;
using Sandbox;

public static partial class EditorToolBars
{
	private static List<ToolBarContext> _allToolbars = new();

	public static ToolBar MainTools;
	public static ToolBar SelectionModes;
	public static ToolBar EditingSettings;
	public static ToolBar ViewSettings;

	private static DockWindow MainWindow;
	private static ViewportTools ViewportTools;
	private static EditorToolManager _subscribedToolManager;

	/// <summary>
	/// On the first creation of the editor - initializes all the necessary toolbars,
	/// builds shortcut cache and stores MainWindow ref within this class.
	/// </summary>
	[Event( "editor.created" )]
	public static void InitToolbars( EditorMainWindow window )
	{
		MainWindow = window;
		var sceneView = SceneViewWidget.Current;
		if ( sceneView != null )
		{
			ViewportTools = sceneView.ViewportTools;
		}

		if ( Preferences.CustomEditorPreferences.BuildToolbarsOnStartup )
		{
			BuildToolbars();
		}
	}

	static void BuildToolbars()
	{
		TryClearToolbars();
		BuildShortcutCache();
		BuildAllToolbars();
		RegisterEditorEventSubscriptions();
		RefreshToolbarStates( force: true );
	}

	/// <summary>
	/// Initializes and configures all toolbars for the main application window.
	/// </summary>
	/// <remarks>This method builds the main tools, selection modes, editing settings, and view settings toolbars. </remarks>
	internal static void BuildAllToolbars()
	{
		if ( MainWindow == null )
			return;

		BuildMainTools( MainWindow );
		BuildSelectionModes( MainWindow );
		BuildEditingSettings( MainWindow );
		BuildViewSettings( MainWindow );
	}

	/// <summary>
	/// Attempts to close and clear all active toolbars, releasing their resources and resetting their references.
	/// </summary>
	/// <remarks>This method is typically used during application shutdown or when resetting the user interface to
	/// ensure that all toolbars are properly disposed of. After calling this method, the toolbar references will be set to
	/// null and should not be used unless reinitialized.</remarks>
	static void TryClearToolbars()
	{
		static void CloseIfExists( ToolBar tb )
		{
			if ( tb != null )
			{
				tb.Close();
				tb.Clear();
			}
		}

		CloseIfExists( MainTools );
		CloseIfExists( SelectionModes );
		CloseIfExists( EditingSettings );
		CloseIfExists( ViewSettings );

		MainTools = null;
		SelectionModes = null;
		EditingSettings = null;
		ViewSettings = null;

		if ( _subscribedToolManager != null )
		{
			_subscribedToolManager.ToolChanged -= OnSceneToolChanged;
			_subscribedToolManager = null;
		}

		_allToolbars.Clear();
		s_needsFullRefresh = true;
	}

	[Event( "tools.gamedata.refresh" )]
	private static void InitializeShortcutCache()
	{
		BuildShortcutCache();
		ValidateAllToolActionAvailability();
		RefreshToolbarStates( force: true );
	}

	private static void RegisterEditorEventSubscriptions()
	{
		TryRegisterSceneViewToolEvents();
	}

	[EditorEvent.Frame]
	private static void TryRegisterSceneViewToolEvents()
	{
		var sceneView = SceneViewWidget.Current;
		var tools = sceneView?.Tools;
		if ( tools == null )
		{
			RefreshToolbarStates();
			return;
		}

		if ( _subscribedToolManager != tools )
		{
			if ( _subscribedToolManager != null )
			{
				_subscribedToolManager.ToolChanged -= OnSceneToolChanged;
			}

			tools.ToolChanged -= OnSceneToolChanged;
			tools.ToolChanged += OnSceneToolChanged;
			_subscribedToolManager = tools;
		}

		RefreshToolbarStates();
	}

	private static void OnSceneToolChanged( EditorTool tool )
	{
		if ( _pendingSubtool != null && tool?.GetType().Name == nameof( MeshTool ) )
		{
			Log.Info( $"Applying delayed subtool: '{_pendingSubtool}'" );
			EditorToolManager.SetSubTool( _pendingSubtool );
			_pendingSubtool = null;
		}

		RefreshToolbarStates( force: true );
	}

	[Event( "scene.play", Priority = 100 )]
	private async static void OnPlay()
	{
		await GameTask.Delay( 100 );

		EditorToolBars.SetPlayMode( true );
	}

	[Event( "scene.stop", Priority = 100 )]
	private async static void OnStop()
	{
		await GameTask.Delay( 100 );

		EditorToolBars.SetPlayMode( false );
	}

	// MAIN TOOLS
	private static void BuildMainTools( DockWindow window )
	{
		MainTools = new ToolBar( window, "Main Tools" );
		MainTools.SetIconSize( new Vector2( 32, 32 ) );

		AddDefs( MainTools, CreateMainToolDefs(), singleSelect: true );

		window.AddToolBar( MainTools, ToolbarPosition.Left );
		RegisterToolBar( "MainTools", MainTools, window );
	}

	// SELECTION MODES
	private static void BuildSelectionModes( DockWindow window )
	{
		SelectionModes = new ToolBar( window, "Selection Modes" );
		SelectionModes.SetIconSize( 24 );
		SelectionModes.ButtonStyle = ToolButtonStyle.TextBesideIcon;

		Label label = new( SelectionModes )
		{
			Text = "Select: ",
			Color = Theme.Text
		};
		SelectionModes.AddWidget( label );

		AddDefs( SelectionModes, CreateSelectionModeDefs(), singleSelect: true );

		window.AddToolBar( SelectionModes, ToolbarPosition.Top );
		RegisterToolBar( "SelectionModes", SelectionModes, window );
	}

	// EDITING SETTINGS
	private static void BuildEditingSettings( DockWindow window )
	{
		EditingSettings = new ToolBar( window, "Editing Settings" );
		SelectionModes.SetIconSize( 22 );

		Label label = new( EditingSettings )
		{
			Text = "Editing: ",
			Color = Theme.Text
		};
		EditingSettings.AddWidget( label );

		AddDefs( EditingSettings, CreateEditingSettingDefs() );

		window.AddToolBar( EditingSettings, ToolbarPosition.Top );
		RegisterToolBar( "EditingSettings", EditingSettings, window );
	}

	// VIEW SETTINGS
	private static void BuildViewSettings( DockWindow window )
	{
		ViewSettings = new ToolBar( window, "View Settings" );
		SelectionModes.SetIconSize( 22 );

		Label label = new( ViewSettings )
		{
			Text = "View: ",
			Color = Theme.Text
		};
		ViewSettings.AddWidget( label );

		AddDefs( ViewSettings, CreateViewSettingDefs() );

		window.AddToolBar( ViewSettings, ToolbarPosition.Top );
		RegisterToolBar( "ViewSettings", ViewSettings, window );
	}

	[Event( "tools.editorwindow.postcreateview" )]
	private static void AddToolbarTogglesToViewMenu( Menu menu )
	{
		if ( MainWindow == null )
			return;

		menu.AddSeparator();
		AddToolbarToggleOption( menu, "Editor - Main Tools", MainTools );
		AddToolbarToggleOption( menu, "Editor - Selection Modes", SelectionModes );
		AddToolbarToggleOption( menu, "Editor - Editing Settings", EditingSettings );
		AddToolbarToggleOption( menu, "Editor - View Settings", ViewSettings );
	}

	private static void AddToolbarToggleOption( Menu menu, string title, ToolBar toolbar )
	{
		if ( toolbar == null )
			return;

		var option = menu.AddOption( title, "hammer/appicon.ico" );
		option.Checkable = true;
		option.Checked = toolbar.Visible;
		option.Toggled += ( visible ) => toolbar.Visible = visible;
	}
}

