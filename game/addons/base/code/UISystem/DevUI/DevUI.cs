namespace Sandbox.UI.Dev;

using Sandbox;

public class DevLayer : RootPanel
{
	ExceptionNotification ExceptionNotification;

	public DevLayer()
	{
		Log.Info( "Creating DevLayer" );

		// Stylesheet autoload depends on TypeLibrary metadata (ClassFileLocationAttribute),
		// which can be missing in some base-context init paths. Load the key DevUI sheets
		// explicitly so the UI is visible even without TypeLibrary enrollment.
		LoadDevUiStyles();

		AddChild<DeveloperMode>();
		AddChild<ConsoleOverlay>();

		ExceptionNotification = AddChild<ExceptionNotification>();

		MenuUtility.AddLogger( OnConsoleMessage );
	}

	public override void Tick()
	{
		base.Tick();

		// DevUI.cs.scss disables pointer events for the whole DevLayer unless this class is present.
		// Keep it in sync with the devui focused state so unfocused windows can stay visible without eating input.
		SetClass( "developermode", DeveloperMode.Open && DeveloperMode.Focused );
	}

	void LoadDevUiStyles()
	{
		const bool failSilently = true;

		// Use non-rooted paths here so we don't end up loading the same sheet twice
		// (auto-loaded sheets are typically non-rooted via ClassFileLocationAttribute).
		StyleSheet.Load( "UISystem/DevUI/DevUI.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Window/DevWindow.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Tabs/DevTabs.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/DevMode/DeveloperMode.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/DevMode/ConvarToggle.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/DevMode/RenderModeSelect.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Stats/StatsContainer.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Stats/StatValue.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/ConsoleOverlay/ConsoleOverlay.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Console/Console.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Console/ConsoleRow.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Console/LogEventPanel.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Layouts/DevGroup.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Layouts/Columns.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Layouts/Row.razor.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Controls/DevScrollPanel.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Controls/DevScrollBar.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Controls/DevScrollView.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/Controls/DevVirtualList.cs.scss", true, failSilently );
		StyleSheet.Load( "UISystem/DevUI/ExceptionNotification.cs.scss", true, failSilently );

		// Generic UI controls used by DevUI extension tabs.
		StyleSheet.Load( "UI/Controls/VideoPanel.razor.scss", true, failSilently );
		StyleSheet.Load( "UI/Controls/VideoControls.razor.scss", true, failSilently );
	}

	public override void OnDeleted()
	{
		base.OnDeleted();

		MenuUtility.RemoveLogger( OnConsoleMessage );
	}

	[ConVar( "devui_scale" )]
	public static float DevUI_Scale { get; set; } = 1.0f;

	protected override void UpdateScale( Rect screenSize )
	{
		Scale = Screen.DesktopScale * DevUI_Scale;
	}

	private void OnConsoleMessage( LogEvent entry )
	{
		if ( !ThreadSafe.IsMainThread )
			return;

		if ( entry.Level == LogLevel.Error )
		{
			ExceptionNotification.OnException( entry );
		}
	}
}
