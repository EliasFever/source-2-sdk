using Sandbox;

namespace Sandbox.UI.Overlay;

public sealed class UISystemOverlay : RootPanel
{
	Panel _loadingOverlayPanel;
	string _loadingOverlayKey;
	bool _loadingVisibleLastTick;

	public UISystemOverlay()
	{
		// Don't choose the panel once in the constructor - the desired overlay can change per load.
		EnsureLoadingOverlayPanel();
	}

	public override void Tick()
	{
		base.Tick();

		var shouldEnsureOverlay = LoadingScreen.IsVisible
			|| !string.IsNullOrWhiteSpace( LoadingScreen.OverlayPanelTypeName )
			|| LoadingScreen.CurrentContext != LoadingScreen.Context.Unknown;

		// Ensure the correct overlay panel is created ahead of time when possible.
		// This avoids "first show" transitions being skipped if the panel is swapped in at the same moment
		// the loading screen becomes visible.
		if ( shouldEnsureOverlay )
		{
			if ( !_loadingVisibleLastTick )
			{
				// First tick where the UI system can actually render the loading overlay.
				LoadingScreen.MarkBecameVisible();
			}

			EnsureLoadingOverlayPanel();
			_loadingVisibleLastTick = true;
		}
		else
		{
			if ( _loadingVisibleLastTick )
			{
				LoadingScreen.ClearVisibleTimestamp();
			}

			_loadingVisibleLastTick = false;
		}
	}

	void EnsureLoadingOverlayPanel()
	{
		var desiredKey = GetDesiredOverlayKey();
		if ( desiredKey == _loadingOverlayKey && _loadingOverlayPanel.IsValid() )
			return;

		// Once the loading overlay is visible, don't swap/recreate the panel mid-visibility phase.
		// This prevents intro/fade animations from restarting when the loading system assigns overlay
		// metadata slightly later (or when projects pre-warm the overlay before starting a load).
		if ( LoadingScreen.IsVisible && _loadingVisibleLastTick && _loadingOverlayPanel.IsValid() )
		{
			return;
		}

		var panel = TryCreateDesiredOverlayPanel( desiredKey );
		if ( panel is null )
		{
			// If we couldn't create the desired panel (eg type not enrolled yet), keep the existing one
			// but allow retry on subsequent ticks.
			return;
		}

		_loadingOverlayPanel?.Delete( true );
		_loadingOverlayPanel = panel;
		_loadingOverlayKey = desiredKey;
		AddChild( _loadingOverlayPanel );
	}

	string GetDesiredOverlayKey()
	{
		// Explicit per-context selection wins.
		if ( !string.IsNullOrWhiteSpace( LoadingScreen.OverlayPanelTypeName ) )
			return $"type:{LoadingScreen.OverlayPanelTypeName}";

		// If the caller didn't explicitly set a panel type name, try to infer one from project loading settings
		// based on the active loading context. This avoids a one-frame fallback to the default overlay when
		// LoadingScreen.IsVisible is toggled before the overlay type is assigned.
			if ( LoadingScreen.IsVisible || LoadingScreen.CurrentContext != LoadingScreen.Context.Unknown )
			{
				var settingsContext = LoadingScreen.CurrentContext switch
				{
					LoadingScreen.Context.Startup => LoadingSettings.LoadingContext.Startup,
					LoadingScreen.Context.NetworkConnect => LoadingSettings.LoadingContext.NetworkConnect,
					LoadingScreen.Context.EditorPlay => LoadingSettings.LoadingContext.EditorPlay,
					_ => LoadingSettings.LoadingContext.SceneTransition
				};

			var inferred = ProjectSettings.Loading?.GetPolicy( settingsContext ).OverlayPanelTypeName;
			if ( !string.IsNullOrWhiteSpace( inferred ) )
				return $"type:{inferred}";

			// If we already have a typed overlay active during this visible phase, keep it rather than swapping
			// back to the legacy/default overlay due to transient state changes.
			if ( _loadingVisibleLastTick && _loadingOverlayKey?.StartsWith( "type:" ) == true && _loadingOverlayPanel.IsValid() )
				return _loadingOverlayKey;
		}

		return "default";
	}

	Panel TryCreateDesiredOverlayPanel( string desiredKey )
	{
		// When a panel type is explicitly requested (key starts with type:), we should only succeed if we can
		// actually create that panel. Falling back to the base overlay while still claiming the type key would
		// "lock in" the fallback for the rest of the visible phase.
		if ( desiredKey?.StartsWith( "type:", StringComparison.Ordinal ) == true )
		{
			var typeName = desiredKey["type:".Length..];
			var panelType = Game.TypeLibrary?.GetType( typeof( Panel ), typeName, preferAddonAssembly: true, exactFullName: true );
			var panel = panelType?.Create<Panel>();
			return panel;
		}

		// Base default.
		// Ensure base overlay styles exist even if typelibrary metadata is missing.
		StyleSheet.Load( "UISystem/Overlays/LoadingOverlay.razor.scss", true, failSilently: true );
		return new Overlays.LoadingOverlay();
	}

	protected override void UpdateScale( Rect screenSize )
	{
		Scale = Screen.DesktopScale;

		var minimumHeight = 1080.0f * Screen.DesktopScale;

		if ( screenSize.Height < minimumHeight )
		{
			Scale *= screenSize.Height / minimumHeight;
		}
	}
}

