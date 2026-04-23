namespace Sandbox;

[Expose]
public class LoadingSettings : ConfigData
{
	[Hide]
	public override int Version => 1;

	public enum LoadingContext
	{
		Startup,
		SceneTransition,
		NetworkConnect,
		EditorPlay
	}

	public readonly record struct Policy( float MinimumVisibleSeconds, bool RequireInputToContinue, string ContinueInputAction, string OverlayPanelTypeName );

	/// <summary>
	/// Legacy: default minimum duration (in seconds) to keep the loading screen visible for scene loads.
	/// </summary>
	[Hide]
	public float MinimumVisibleSeconds { get; set; } = 0.0f;

	/// <summary>
	/// Legacy: default behavior for requiring input to continue once a scene finishes loading.
	/// </summary>
	[Hide]
	public bool RequireInputToContinue { get; set; } = false;

	/// <summary>
	/// Legacy: default input action name used when <see cref="RequireInputToContinue"/> is true.
	/// If null or whitespace, any key or input action press will continue.
	/// </summary>
	[Hide]
	public string ContinueInputAction { get; set; }

	[Group( "Startup" )]
	[Title( "Enable Startup Overlay" )]
	[Description( "If enabled, the initial splash->first scene load will use the UI loading overlay and can be gated by minimum visible time / input. By default this is disabled because startup is preceded by a native splash that may cover the UI overlay." )]
	public bool EnableStartupOverlay { get; set; } = false;

	[Group( "Startup" ), ShowIf( nameof( EnableStartupOverlay ), true )]
	public float StartupMinimumVisibleSeconds { get; set; } = 0.0f;

	[Group( "Startup" ), ShowIf( nameof( EnableStartupOverlay ), true )]
	public bool StartupRequireInputToContinue { get; set; } = false;

	[Group( "Startup" ), ShowIf( nameof( EnableStartupOverlay ), true )]
	public string StartupContinueInputAction { get; set; }

	[Group( "Startup" ), ShowIf( nameof( EnableStartupOverlay ), true )]
	[Title( "Overlay Panel Type" )]
	[Description( "Fully qualified panel type name (Razor-generated class). Leave empty to use default overlay." )]
	[PanelTypeDropdown( typeof( Sandbox.UI.Panel ) )]
	public string StartupOverlayPanelTypeName { get; set; }

	[Group( "Scene Transition" )]
	public float SceneTransitionMinimumVisibleSeconds { get; set; } = 0.0f;

	[Group( "Scene Transition" )]
	public bool SceneTransitionRequireInputToContinue { get; set; } = false;

	[Group( "Scene Transition" )]
	public string SceneTransitionContinueInputAction { get; set; }

	[Group( "Scene Transition" )]
	[Title( "Overlay Panel Type" )]
	[Description( "Fully qualified panel type name (Razor-generated class). Leave empty to use default overlay." )]
	[PanelTypeDropdown( typeof( Sandbox.UI.Panel ) )]
	public string SceneTransitionOverlayPanelTypeName { get; set; }

	[Group( "Network Connect" )]
	public float NetworkConnectMinimumVisibleSeconds { get; set; } = 0.0f;

	[Group( "Network Connect" )]
	public bool NetworkConnectRequireInputToContinue { get; set; } = false;

	[Group( "Network Connect" )]
	public string NetworkConnectContinueInputAction { get; set; }

	[Group( "Network Connect" )]
	[Title( "Overlay Panel Type" )]
	[Description( "Fully qualified panel type name (Razor-generated class). Leave empty to use default overlay." )]
	[PanelTypeDropdown( typeof( Sandbox.UI.Panel ) )]
	public string NetworkConnectOverlayPanelTypeName { get; set; }

	[Group( "Editor Play" )]
	public float EditorPlayMinimumVisibleSeconds { get; set; } = 0.0f;

	[Group( "Editor Play" )]
	public bool EditorPlayRequireInputToContinue { get; set; } = false;

	[Group( "Editor Play" )]
	public string EditorPlayContinueInputAction { get; set; }

	[Group( "Editor Play" )]
	[Title( "Overlay Panel Type" )]
	[Description( "Fully qualified panel type name (Razor-generated class). Leave empty to use default overlay." )]
	[PanelTypeDropdown( typeof( Sandbox.UI.Panel ) )]
	public string EditorPlayOverlayPanelTypeName { get; set; }

	public Policy GetPolicy( LoadingContext context )
	{
		return context switch
		{
			LoadingContext.Startup => EnableStartupOverlay
				? new Policy( StartupMinimumVisibleSeconds, StartupRequireInputToContinue, StartupContinueInputAction, StartupOverlayPanelTypeName )
				: new Policy( 0.0f, false, null, null ),
			LoadingContext.SceneTransition => new Policy( SceneTransitionMinimumVisibleSeconds, SceneTransitionRequireInputToContinue, SceneTransitionContinueInputAction, SceneTransitionOverlayPanelTypeName ),
			LoadingContext.NetworkConnect => new Policy( NetworkConnectMinimumVisibleSeconds, NetworkConnectRequireInputToContinue, NetworkConnectContinueInputAction, NetworkConnectOverlayPanelTypeName ),
			LoadingContext.EditorPlay => new Policy( EditorPlayMinimumVisibleSeconds, EditorPlayRequireInputToContinue, EditorPlayContinueInputAction, EditorPlayOverlayPanelTypeName ),
			_ => new Policy( 0.0f, false, null, null )
		};
	}

	protected override void OnValidate()
	{
		base.OnValidate();

		// Back-compat: if legacy fields are set, use them as defaults for scene transitions.
		if ( SceneTransitionMinimumVisibleSeconds == 0.0f && MinimumVisibleSeconds != 0.0f )
			SceneTransitionMinimumVisibleSeconds = MinimumVisibleSeconds;

		if ( !SceneTransitionRequireInputToContinue && RequireInputToContinue )
			SceneTransitionRequireInputToContinue = true;

		if ( string.IsNullOrWhiteSpace( SceneTransitionContinueInputAction ) && !string.IsNullOrWhiteSpace( ContinueInputAction ) )
			SceneTransitionContinueInputAction = ContinueInputAction;
	}
}
