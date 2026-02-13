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
		None,                       // Independent toggle
		SingleExclusive,            // Only one active in this group, cannot unselect
		SingleToggleable,           // Only one active, but can click again to unselect
		ConditionalPreserveState,   // Child disabled but state preserved
		ConditionalClearState,      // Child disabled and gets reset
		ExternallyControlled        // Disabled unless explicitly activated by code
	}

	public enum ToolActionType
	{
		Shortcut,                   // Existing behavior
		MethodCall,                 // Call a bound delegate
		PropertyToggle,             // Toggle a boolean property
		PropertySet                 // Set specific value
	}

	public class ToolOptionDef
	{
		public string Name;
		public string Description;              // Optional

		public Option Widget;                   // Stores the Option created in toolbar

		public string Icon;
		public string ToggledIcon;              // Optional
		public List<string> IconCycle;          // Optional cycle of icons

		public int CurrentIconIndex = 0;        // Index in IconCycle
		public string OverrideIcon;             // Externally forced icon

		public string Hotkey;                   // Optional

		public bool Checkable = false;
		public bool Separator = false;
		public bool Active = false;             // Current active state
		public bool DisableDuringPlay = true;   // Disable/Enable buttons when in Play mode

		public string Group = null;
		public string ConditionalOn = null;     // Only used if GroupType == Conditional

		public bool ExternalEnabled = false;

		public string ShortcutAction;           // Mapping to a shortcut e.g. "mesh.vertex"

		public ToolActionType ActionType = ToolActionType.Shortcut;

		/// If ActionType is MethodCall
		public Action Method;

		/// If ActionType is PropertyToggle
		public Func<bool> Getter;
		public Action<bool> Setter;

		/// If ActionType is PropertySet
		public Action SetterAction;

		/// Optional runtime resolver used to synchronize visual toggle state with editor state (hotkeys, mode changes).
		public Func<bool> ActiveResolver;

		/// Optional runtime resolver used to synchronize enabled state.
		public Func<bool> EnabledResolver;

		/// True when this definition has a valid executable action.
		public bool ActionAvailable = true;

		public ToolBarOptionGroupType GroupType = ToolBarOptionGroupType.None;
	}

	private static Dictionary<string, MethodInfo> s_shortcutCache;
	private static Dictionary<ToolOptionDef, (bool Active, bool Checked)> s_prePlayState = new();

	private static bool s_inPlayMode = false;
	private static bool s_needsFullRefresh = true;
	private static string s_lastModeName;
	private static string s_lastSubModeName;
	private static bool s_lastGamePlaying;
	private static bool s_lastGamePaused;
	private static bool s_lastGlobalSpace;
	private static bool s_lastGizmosEnabled;
	private static SceneViewWidget.ViewMode? s_lastViewMode;

	private static string _pendingSubtool = null;
	private static string s_lastTransformMode;

	private sealed class ToolBarEvalContext
	{
		public List<ToolOptionDef> Definitions;
		public Dictionary<string, ToolOptionDef> DefinitionsByName;
	}

	private delegate void GroupHandler( ToolOptionDef def, ToolBarEvalContext context );

	private static readonly Dictionary<ToolActionType, Func<ToolOptionDef, bool>> s_actionValidators = new()
	{
		[ToolActionType.Shortcut] = ValidateShortcutAction,
		[ToolActionType.MethodCall] = ValidateMethodCallAction,
		[ToolActionType.PropertyToggle] = ValidatePropertyToggleAction,
		[ToolActionType.PropertySet] = ValidatePropertySetAction
	};

	private static readonly Dictionary<ToolActionType, Action<ToolOptionDef>> s_actionExecutors = new()
	{
		[ToolActionType.MethodCall] = ExecuteMethodCallAction,
		[ToolActionType.PropertyToggle] = ExecutePropertyToggleAction,
		[ToolActionType.PropertySet] = ExecutePropertySetAction
	};

	private static readonly Dictionary<ToolBarOptionGroupType, GroupHandler> s_groupHandlers = new()
	{
		[ToolBarOptionGroupType.ConditionalPreserveState] = ApplyConditionalState,
		[ToolBarOptionGroupType.ConditionalClearState] = ApplyConditionalState,
		[ToolBarOptionGroupType.ExternallyControlled] = ApplyExternallyControlledState,
		[ToolBarOptionGroupType.SingleExclusive] = ApplySingleExclusiveState
	};

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
				if ( !def.ActionAvailable )
					return;

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

			ValidateAndApplyActionAvailability( def );

			UpdateOptionIcon( def );
		}
	}

	private static bool HasValidAction( ToolOptionDef def )
	{
		return s_actionValidators.TryGetValue( def.ActionType, out var validator ) && validator( def );
	}

	private static void ValidateAndApplyActionAvailability( ToolOptionDef def )
	{
		def.ActionAvailable = HasValidAction( def );

		if ( def.Widget == null || def.ActionAvailable )
			return;

		ApplyDisabledState( def );
	}

	private static void ValidateAllToolActionAvailability()
	{
		if ( _allToolbars == null || _allToolbars.Count == 0 )
			return;

		foreach ( var barCtx in _allToolbars )
		{
			if ( barCtx?.Definitions == null )
				continue;

			foreach ( var def in barCtx.Definitions )
			{
				if ( def == null || def.Separator )
					continue;

				ValidateAndApplyActionAvailability( def );
			}
		}
	}

	/// <summary>
	/// Logic specific to certain toolbars.
	/// </summary>
	private static void HandleSpecialLogic( List<ToolOptionDef> defs, ToolOptionDef activated )
	{
		var context = BuildEvalContext( defs );

		foreach ( var def in defs )
		{
			if ( !def.ActionAvailable )
			{
				ApplyDisabledState( def );
				continue;
			}

			if ( s_groupHandlers.TryGetValue( def.GroupType, out var handler ) )
				handler( def, context );
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
			SetIconIfChanged( def.Widget, def.OverrideIcon );
			return;
		}

		// If IconCycle is set, use current index
		if ( def.IconCycle != null && def.IconCycle.Count > 0 )
		{
			SetIconIfChanged( def.Widget, def.IconCycle[def.CurrentIconIndex] );
			return;
		}

		// Group / Active logic fallback
		SetIconIfChanged( def.Widget, def.Active
			? def.ToggledIcon ?? def.Icon
			: def.Icon );
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
				continue;   // Skip assemblies that can't be reflected
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

	private static void ExecuteToolAction( ToolOptionDef def )
	{
		if ( s_actionExecutors.TryGetValue( def.ActionType, out var executor ) )
			executor( def );
	}

	private static bool ShouldRunRefresh( bool force )
	{
		if ( force || s_needsFullRefresh )
			return true;

		var modeName = EditorToolManager.CurrentModeName;
		var subModeName = EditorToolManager.CurrentSubModeName;
		var playing = Game.IsPlaying;
		var paused = Game.IsPaused;
		var globalSpace = EditorScene.GizmoSettings.GlobalSpace;
		var gizmosEnabled = EditorScene.GizmoSettings.GizmosEnabled;
		var viewMode = SceneViewWidget.Current?.CurrentView;

		return modeName != s_lastModeName
			|| subModeName != s_lastSubModeName
			|| playing != s_lastGamePlaying
			|| paused != s_lastGamePaused
			|| globalSpace != s_lastGlobalSpace
			|| gizmosEnabled != s_lastGizmosEnabled
			|| viewMode != s_lastViewMode;
	}

	private static void CaptureRefreshSnapshot()
	{
		s_lastModeName = EditorToolManager.CurrentModeName;
		s_lastSubModeName = EditorToolManager.CurrentSubModeName;
		s_lastGamePlaying = Game.IsPlaying;
		s_lastGamePaused = Game.IsPaused;
		s_lastGlobalSpace = EditorScene.GizmoSettings.GlobalSpace;
		s_lastGizmosEnabled = EditorScene.GizmoSettings.GizmosEnabled;
		s_lastViewMode = SceneViewWidget.Current?.CurrentView;
		s_needsFullRefresh = false;
	}

	private static void SetCheckedIfChanged( Option widget, bool value )
	{
		if ( widget.Checked != value )
			widget.Checked = value;
	}

	private static void SetEnabledIfChanged( Option widget, bool value )
	{
		if ( widget.Enabled != value )
			widget.Enabled = value;
	}

	private static void SetIconIfChanged( Option widget, string value )
	{
		if ( widget.Icon != value )
			widget.Icon = value;
	}

	private static void RefreshToolbarStates( bool force = false )
	{
		if ( _allToolbars == null || _allToolbars.Count == 0 )
			return;

		if ( !ShouldRunRefresh( force ) )
			return;

		foreach ( var barCtx in _allToolbars )
		{
			if ( barCtx?.Definitions == null )
				continue;

			var defs = barCtx.Definitions;

			foreach ( var def in defs )
			{
				if ( def?.Widget == null || def.Separator )
					continue;

				if ( ShouldForceDisabled( def ) )
				{
					ApplyDisabledState( def );
					continue;
				}

				if ( def.ActiveResolver != null )
				{
					try
					{
						def.Active = def.ActiveResolver();
					}
					catch
					{
						// Keep previous state if resolver fails.
					}
				}

				if ( def.Checkable )
				{
					SetCheckedIfChanged( def.Widget, def.Active );
				}

				if ( def.EnabledResolver != null )
				{
					try
					{
						SetEnabledIfChanged( def.Widget, def.EnabledResolver() );
					}
					catch
					{
						// Preserve existing enabled state.
					}
				}

				UpdateOptionIcon( def );
			}

			HandleSpecialLogic( defs, null );
		}

		CaptureRefreshSnapshot();
	}

	private static ToolBarEvalContext BuildEvalContext( List<ToolOptionDef> defs )
	{
		var byName = new Dictionary<string, ToolOptionDef>( defs.Count, StringComparer.Ordinal );
		foreach ( var d in defs )
		{
			if ( d?.Name is not null )
				byName[d.Name] = d;
		}

		return new ToolBarEvalContext
		{
			Definitions = defs,
			DefinitionsByName = byName
		};
	}

	private static bool ValidateShortcutAction( ToolOptionDef def )
		=> !string.IsNullOrWhiteSpace( def.ShortcutAction )
		&& s_shortcutCache?.ContainsKey( def.ShortcutAction ) == true;

	private static bool ValidateMethodCallAction( ToolOptionDef def )
		=> def.Method != null;

	private static bool ValidatePropertyToggleAction( ToolOptionDef def )
		=> def.Getter != null && def.Setter != null;

	private static bool ValidatePropertySetAction( ToolOptionDef def )
		=> def.SetterAction != null;

	private static void ExecuteMethodCallAction( ToolOptionDef def )
		=> def.Method?.Invoke();

	private static void ExecutePropertyToggleAction( ToolOptionDef def )
	{
		bool v = !def.Getter();
		def.Setter( v );
	}

	private static void ExecutePropertySetAction( ToolOptionDef def )
		=> def.SetterAction?.Invoke();

	private static void ApplyConditionalState( ToolOptionDef def, ToolBarEvalContext context )
	{
		if ( string.IsNullOrEmpty( def.ConditionalOn ) )
			return;

		context.DefinitionsByName.TryGetValue( def.ConditionalOn, out var parent );
		bool parentActive = parent?.Active ?? false;

		if ( def.Widget == null )
			return;

		SetEnabledIfChanged( def.Widget, parentActive );

		if ( parentActive || def.GroupType != ToolBarOptionGroupType.ConditionalClearState )
			return;

		def.Active = false;
		SetCheckedIfChanged( def.Widget, false );
		SetIconIfChanged( def.Widget, def.Icon );
	}

	private static void ApplyExternallyControlledState( ToolOptionDef def, ToolBarEvalContext context )
	{
		if ( def.Widget == null )
			return;

		SetEnabledIfChanged( def.Widget, def.ExternalEnabled );

		if ( def.ExternalEnabled )
			return;

		def.Active = false;
		if ( def.Checkable )
			SetCheckedIfChanged( def.Widget, false );

		SetIconIfChanged( def.Widget, def.Icon );
	}

	private static void ApplySingleExclusiveState( ToolOptionDef def, ToolBarEvalContext context )
	{
		foreach ( var other in context.Definitions )
		{
			if ( other == def )
				continue;

			if ( other.Group != def.Group || other.Widget == null )
				continue;

			SetCheckedIfChanged( other.Widget, other.Active );
			SetIconIfChanged( other.Widget, other.Active ? other.ToggledIcon ?? other.Icon : other.Icon );
		}
	}

	private static bool ShouldForceDisabled( ToolOptionDef def )
		=> !def.ActionAvailable || (s_inPlayMode && def.DisableDuringPlay);

	private static void ApplyDisabledState( ToolOptionDef def )
	{
		if ( def?.Widget == null )
			return;

		def.Active = false;
		if ( def.Checkable )
			SetCheckedIfChanged( def.Widget, false );

		SetEnabledIfChanged( def.Widget, false );
		SetIconIfChanged( def.Widget, def.Icon );
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

		s_needsFullRefresh = true;
		RefreshToolbarStates( force: true );
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
		=> Activate( nameof( MeshTool ), nameof( MeshSelection ) );

	public static void SelectObjects()
		=> Activate( nameof( MeshTool ), nameof( ObjectSelection ) );

	public static void SelectNavigation()
		=> Activate( nameof( NavMeshTool ) );

	//
	// General tools modes
	//
	public static void SelectBlockTool()
		=> Activate( nameof( MeshTool ), nameof( MeshTool ) );

	public static void SelectPathTool()
		=> ActiveProjectTool( "PathTool" );

	public static void SelectPhysicsTool()
		=> Activate( nameof( PhysicsEditorTool ) );

	public static void SelectTerraintool()
		=> Activate( nameof( TerrainEditorTool ) );

	public static void SelectClippingTool()
		=> OpenMeshSubTool( () => new ClipTool() );

	public static void SelectMirrorTool()
		=> OpenMeshSubTool( () => new MirrorTool() );

	public static void SelectBlendTool()
		=> Activate( nameof( MeshTool ), nameof( VertexPaintTool ) );

	private static void OpenMeshSubTool( Func<EditorTool> toolFactory )
	{
		var tools = SceneViewWidget.Current?.Tools;
		if ( tools == null )
			return;

		if ( tools.CurrentTool is not MeshTool )
		{
			EditorToolManager.SetTool( nameof( MeshTool ) );
			tools.UpdateTool( EditorToolManager.CurrentModeName );
		}

		if ( tools.CurrentTool is not MeshTool meshTool )
			return;

		var tool = toolFactory?.Invoke();
		if ( tool == null )
			return;

		tool.Manager = meshTool.Manager;
		meshTool.CurrentTool = tool;
	}

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
		Type toolType = AppDomain.CurrentDomain.GetAssemblies()
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
