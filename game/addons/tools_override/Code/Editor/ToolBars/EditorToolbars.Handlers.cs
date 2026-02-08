namespace Editor;

using Editor;
using Editor.MapEditor;
using Editor.MeshEditor;
using Editor.TerrainEditor;
using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Editor.EditorToolBars;

public static partial class EditorToolBars
{
	public enum ToolBarOptionGroupType
	{
		None,						// Independent toggle
		SingleExclusive,			// Only one active in this group, cannot unselect
		SingleToggleable,			// Only one active, but can click again to unselect
		ConditionalPreserveState,   // Child disabled but state preserved
		ConditionalClearState,      // Child disabled and gets reset
		ExternallyControlled        // Disabled unless explicitly activated by code
	}

	public enum ToolActionType
	{
    	Shortcut,        			// Existing behavior
    	MethodCall,      			// Call a bound delegate
    	PropertyToggle,  			// Toggle a boolean property
    	PropertySet      			// Set specific value
	}

	public class ToolOptionDef
	{
		public string Name;
		public string Description;				// Optional

		public Option Widget;					// Stores the Option created in toolbar

		public string Icon;
		public string ToggledIcon;              // Optional
		public List<string> IconCycle;          // Optional cycle of icons

		public int CurrentIconIndex = 0;		// Index in IconCycle
		public string OverrideIcon;				// Externally forced icon

		public string Hotkey;					// Optional

		public bool Checkable = false;
		public bool Separator = false;
		public bool Active = false;             // Current active state
		public bool DisableDuringPlay = true;   // Disable/Enable buttons when in Play mode

		public string Group = null;
		public string ConditionalOn = null;     // Only used if GroupType == Conditional

		public bool ExternalEnabled = false;

		public string ShortcutAction;			// Mapping to a shortcut e.g. "mesh.vertex"

		public ToolActionType ActionType = ToolActionType.Shortcut;

    	/// If ActionType is MethodCall
    	public Action Method;

    	/// If ActionType is PropertyToggle
	    public Func<bool> Getter;
    	public Action<bool> Setter;

    	/// If ActionType is PropertySet
	    public Action SetterAction;

		public ToolBarOptionGroupType GroupType = ToolBarOptionGroupType.None;
	}

	private static Dictionary<string, MethodInfo> s_shortcutCache;
	private static Dictionary<ToolOptionDef, (bool Active, bool Checked)> s_prePlayState = new();

	private static bool s_inPlayMode = false;

	private static string _pendingSubtool = null;
	private static string s_lastTransformMode;

	/// <summary>
	/// Adds a collection of tool option definitions to the specified toolbar, configuring their selection behavior and
	/// initial state.
	/// </summary>
	/// <remarks>If single-selection mode is enabled, activating one option will automatically deactivate all
	/// others. Options marked as separators in the definitions will be added as separators to the toolbar. The method also
	/// supports group-based exclusive selection if defined in the option definitions.</remarks>
	/// <param name="bar">The toolbar to which the tool options and separators will be added.</param>
	/// <param name="defs">A list of tool option definitions that specify the options and separators to add to the toolbar.</param>
	/// <param name="singleSelect">true to enable single-selection mode, where only one option can be active at a time; otherwise, false to allow
	/// multiple options to be active.</param>
	public static void AddDefs( ToolBar bar, List<ToolOptionDef> defs, bool singleSelect = false )
	{
		_allToolbars.Add( new ToolBarContext
		{
			Bar = bar,
			Definitions = defs
		} );

		foreach ( var def in defs )
		{
			if ( def.Separator )
			{
				bar.AddSeparator();
				continue;
			}

			Option option = null;

			void callback()
			{
				// GROUP LOGIC
				if ( def.GroupType == ToolBarOptionGroupType.SingleExclusive && !string.IsNullOrEmpty( def.Group ) )
				{
					foreach ( var d in defs )
					{
						if ( d.Group != def.Group || d == def )
							continue;

						d.Active = false;
						if ( d.Widget != null )
						{
							d.Widget.Checked = false;
							d.Widget.Icon = d.Icon;
						}
					}

					def.Active = true;
					option.Checked = true;
					UpdateOptionIcon( def ); // call your existing helper
				}
				else if ( singleSelect )
				{
					// Single-selection toolbar mode
					foreach ( var d in defs )
					{
						if ( d.Widget == null ) continue;

						if ( d == def )
						{
							d.Active = true;
							d.Widget.Checked = true;
						}
						else
						{
							d.Active = false;
							d.Widget.Checked = false;
						}

						// Always update icon for all buttons
						if ( d.Widget != null )
						{
							if ( d == def )
								UpdateOptionIcon( d );
							else
								d.Widget.Icon = d.Icon;
						}
					}
				}
				else
				{
					if ( def.IconCycle != null && def.IconCycle.Count > 0 )
					{
						def.CurrentIconIndex = (def.CurrentIconIndex + 1) % def.IconCycle.Count;
						UpdateOptionIcon( def );
					}
					else
					{
						// Multi-select toggle
						def.Active = !def.Active;
						option.Checked = def.Active;
						UpdateOptionIcon( def );
					}
				}

				HandleSpecialLogic( defs, def );

				// Here, fishy-fishy!
				ExecuteToolAction( def );
			}

			// Add the option to the toolbar
			option = bar.AddOption( def.Name, def.Active ? def.ToggledIcon ?? def.Icon : def.Icon, callback );
			option.Checkable = def.Checkable;
			option.ToolTip = !string.IsNullOrWhiteSpace( def.Hotkey )
				? $"{def.Name} [{def.Hotkey}]"
				: def.Name;

			def.Widget = option;

			UpdateOptionIcon( def );
		}
	}

	/// <summary>
	/// Logic specific to certain toolbars.
	/// </summary>
	private static void HandleSpecialLogic( List<ToolOptionDef> defs, ToolOptionDef activated )
	{
		foreach ( var def in defs )
		{
			// CONDITIONAL LOGIC (Preserve or Reset)
			if ( (def.GroupType == ToolBarOptionGroupType.ConditionalPreserveState ||
				  def.GroupType == ToolBarOptionGroupType.ConditionalClearState)
				 && !string.IsNullOrEmpty( def.ConditionalOn ) )
			{
				// Find parent
				var parent = defs.Find( d => d.Name == def.ConditionalOn );
				bool parentActive = parent?.Active ?? false;

				if ( def.Widget != null )
				{
					def.Widget.Enabled = parentActive;

					if ( !parentActive )
					{
						if ( def.GroupType == ToolBarOptionGroupType.ConditionalClearState )
						{
							// HARD CONDITIONAL — reset state
							def.Active = false;
							def.Widget.Checked = false;
							def.Widget.Icon = def.Icon;
						}
					}
				}
			}

			// EXTERNALLY CONTROLLED
			if ( def.GroupType == ToolBarOptionGroupType.ExternallyControlled )
			{
				def.Widget.Enabled = def.ExternalEnabled;

				if ( !def.ExternalEnabled )
				{
					def.Active = false;

					if ( def.Checkable )
						def.Widget.Checked = false;

					def.Widget.Icon = def.Icon;
				}
			}

			// MUTUAL EXCLUSIVITY (unchanged)
			if ( def.GroupType == ToolBarOptionGroupType.SingleExclusive )
			{
				foreach ( var other in defs )
				{
					if ( other == def ) continue;
					if ( other.Group == def.Group && other.Widget != null )
					{
						other.Widget.Checked = other.Active;
						other.Widget.Icon = other.Active ? other.ToggledIcon ?? other.Icon : other.Icon;
					}
				}
			}
		}
	}

	/// <summary>
	/// Updates the displayed icon of a ToolOptionDef.
	/// Prioritizes external override icon, then cycles through IconCycle if set, and finally falls back to Active/ToggledIcon logic.
	/// </summary>
	private static void UpdateOptionIcon( ToolOptionDef def )
	{
		if ( def.Widget == null ) return;

		// External override takes priority
		if ( !string.IsNullOrEmpty( def.OverrideIcon ) )
		{
			def.Widget.Icon = def.OverrideIcon;
			return;
		}

		// If IconCycle is set, use current index
		if ( def.IconCycle != null && def.IconCycle.Count > 0 )
		{
			def.Widget.Icon = def.IconCycle[def.CurrentIconIndex];
			return;
		}

		// Group / Active logic fallback
		def.Widget.Icon = def.Active
			? def.ToggledIcon ?? def.Icon
			: def.Icon;
	}

	/// <summary>
	/// Builds the cache of methods annotated with the ShortcutAttribute across all loaded assemblies in the current
	/// application domain.
	/// </summary>
	/// <remarks>This method scans all assemblies currently loaded in the application domain and collects static
	/// methods marked with the ShortcutAttribute. Assemblies that cannot be reflected are skipped.</remarks>
	private static void BuildShortcutCache()
	{
		s_shortcutCache = [];

		foreach ( var asm in AppDomain.CurrentDomain.GetAssemblies() )
		{
			Type[] types;

			try
			{
				types = asm.GetTypes();
			}
			catch
			{
				continue;	// Skip assemblies that can't be reflected
			}

			foreach ( var type in types )
			{
				foreach ( var method in type.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic ) )
				{
					var shortcutAttr = method.GetCustomAttribute<ShortcutAttribute>();
					if ( shortcutAttr != null )
					{
						s_shortcutCache[shortcutAttr.Identifier] = method;
					}
				}
			}
		}

		Log.Info( $"[Toolbar] Cached {s_shortcutCache.Count} shortcuts." );

		//foreach ( var key in s_shortcutCache.Keys )
		//{
		//	Log.Info( $"[Toolbar] - {key}" );
		//}
	}

	private static void ExecuteToolAction(ToolOptionDef def)
	{
		switch (def.ActionType)
		{
			// This was stupid I think, we'll just listen to shortcuts instead
			// case ToolActionType.Shortcut:
			// 	if (!string.IsNullOrEmpty(def.ShortcutAction))
			// 		Shortcut.Execute(def.ShortcutAction);
			// 	break;

			case ToolActionType.MethodCall:
				def.Method?.Invoke();
				break;

			case ToolActionType.PropertyToggle:
				bool v = !def.Getter();
				def.Setter(v);
				break;

			case ToolActionType.PropertySet:
				def.SetterAction?.Invoke();
				break;
		}
	}

	private static void SetPlayMode( bool playing )
	{
		s_inPlayMode = playing;

		if ( _allToolbars == null )
			return;

		foreach ( var barCtx in _allToolbars )
		{
			if ( barCtx?.Definitions == null )
				continue;

			foreach ( var def in barCtx.Definitions )
			{
				if ( def?.Widget == null || !def.DisableDuringPlay )
					continue;

				if ( playing )
				{
					// Save current state before disabling
					s_prePlayState[def] = (def.Active, def.Widget.Checked);

					def.Widget.Enabled = false;
					def.Active = false;
					def.Widget.Checked = false;
					def.Widget.Icon = def.Icon;
				}
				else
				{
					// Restore previous state
					if ( s_prePlayState.TryGetValue( def, out var state ) )
					{
						def.Active = state.Active;
						def.Widget.Checked = state.Checked;
						UpdateOptionIcon( def );
					}

					def.Widget.Enabled = true;
				}
			}
		}

		if ( !playing )
			s_prePlayState.Clear(); // clean up
	}

	public static void SelectTransformMode( string mode, bool userClicked = true )
	{
		var tools = SceneViewWidget.Current.Tools;
		var tool = tools.CurrentTool;

		if ( tool == null )
			return;

		// If restoring, use the global stored mode
		if ( mode == null )
		{
			if ( string.IsNullOrEmpty( s_lastTransformMode ) )
				return;

			mode = s_lastTransformMode;
			userClicked = false;
		}

		// Always store the new mode globally
		if ( userClicked )
			s_lastTransformMode = mode;

		//	Log.Info( $"Currently Selected: {mode} for {tool}" );

		switch ( tool )
		{
			case MeshTool meshTool:
				var type = EditorTypeLibrary
					.GetTypes<MoveMode>()
					.FirstOrDefault( t =>
						t.GetAttribute<AliasAttribute>()?.Value?.FirstOrDefault() == $"mesh.{mode}.mode" );

				if ( type != null )
					meshTool.SetMoveMode( type );
				break;

			case ObjectEditorTool:
				switch ( mode )
				{
					case "move": PositionEditorTool.ActivateSubTool(); break;
					case "rotate": RotationEditorTool.ActivateSubTool(); break;
					case "scale": ScaleEditorTool.ActivateSubTool(); break;
				}
				break;
		}
	}
}

public class ToolBarContext
{
	public ToolBar Bar;
	public List<ToolOptionDef> Definitions;
}

public static class EditorToolBarsActions
{
	//
	// Transform selection modes
	//
	public static void ActivateMove() 
		=> EditorToolBars.SelectTransformMode( "move", true );
	public static void ActivateRotate() 
		=> EditorToolBars.SelectTransformMode( "rotate", true );
	public static void ActivateScale() 
		=> EditorToolBars.SelectTransformMode( "scale", true );
	public static void ActivatePivot() 
		=> EditorToolBars.SelectTransformMode( "pivot", true );

	//
	// Mesh selection modes
	//
	public static void SelectVertices()
		=> Activate( nameof( MeshTool ), nameof( VertexTool ) );

	public static void SelectEdges()
		=> Activate( nameof( MeshTool ), nameof( EdgeTool ) );

	public static void SelectFaces()
		=> Activate( nameof( MeshTool ), nameof( FaceTool ) );

	public static void SelectMeshes()
		=> Activate( nameof( ObjectEditorTool ) );

	public static void SelectObjects()
		=> Activate( nameof( ObjectEditorTool ) );

	public static void SelectNavigation()
		=> Activate( nameof( NavMeshTool ) );

	//
	// General tools modes
	//
	public static void SelectBlockTool()
		=> Activate( nameof( MeshTool ), nameof( BlockTool ) );

	public static void SelectPathTool()
		=> ActiveProjectTool( "PathTool" );

	public static void SelectPhysicsTool()
		=> Activate( nameof( PhysicsEditorTool ) );

	public static void SelectTerraintool()
		=> Activate( nameof( TerrainEditorTool ) );

	//
	// Core static activator
	//
	private static void Activate( string toolName, string subToolName = null )
	{
		var tools = SceneViewWidget.Current.Tools;

		if ( string.IsNullOrEmpty( subToolName ) )
		{
			EditorToolManager.SetTool( toolName );
			tools.UpdateTool( EditorToolManager.CurrentModeName );
			return;
		}

		var current = tools.CurrentTool;
		if ( current != null && current.GetType().Name == toolName )
		{
			EditorToolManager.SetSubTool( subToolName );
			tools.UpdateSubTool( EditorToolManager.CurrentSubModeName );
			return;
		}

		EditorToolManager.SetTool( toolName );
		tools.UpdateTool( EditorToolManager.CurrentModeName );

		EditorToolManager.SetSubTool( subToolName );
		tools.UpdateSubTool( EditorToolManager.CurrentSubModeName );
	}

	public static void ActiveProjectTool( string tool )
	{
		string toolName = nameof( tool );

		// Try to get the type by name, including assembly-qualified name if needed
		Type? toolType = AppDomain.CurrentDomain.GetAssemblies()
			.Select( a => a.GetType( toolName ) )
			.FirstOrDefault( t => t != null );

		if ( toolType != null )
		{
			// Found the type, activate it
			Activate( toolType.Name ); // or use any activation logic
		}
		else Log.Warning( $"Tool '{toolName}' not found. Skipping activation." );
	}
}
