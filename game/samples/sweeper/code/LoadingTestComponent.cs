namespace Sweeper;

using Sandbox;
using Sweeper.UI.Loading;
using System;
using System.Threading.Tasks;

public class LoadingTestComponent : Component, ISceneLoadingEvents
{
	[Property] public SceneFile NewScene { get; set; }
	[Property] public SceneFile NewSceneAlt { get; set; }

	[Property] public float PrewarmSeconds { get; set; } = 3f;
	[Property] public float OverlayWaitTimeoutSeconds { get; set; } = 0.5f;

	enum TransitionPhase
	{
		Idle,
		WaitingForOverlay,
		WaitingForPrewarm,
		StartedLoad
	}

	TransitionPhase _phase;
	float _overlayWaitUntil;
	float _prewarmUntil;
	float _requestedPrewarmSeconds;
	SceneLoadOptions _pendingOptions;

	protected override void OnUpdate()
	{
		// John: Had this for sanity testing, nothing more

		// if ( Input.Pressed( "View" ) )
		// 	ForceTransition();

		// if ( Input.Pressed( "Voice" ) )
		// 	ForceTransitionAlt();

		UpdateTransition();
	}
	
	protected override async Task OnLoad()
	{
		Log.Info( "On Load Called." );

		// John: We can test some things here and 
		// eventually we'll need this anywho.

		// LoadingScreen.Title = "Loading Test Component..";
		// await Task.DelayRealtimeSeconds( 1.0f );

		//	await Task.DelayRealtimeSeconds( 5.0f );

		// Log.Info( "Loading finished!" );
		// await Task.DelayRealtimeSeconds( 1.0f );
	}

	[Button]
	public void ForceTransition() => RequestSceneTransition( NewScene, useAltOverlay: false, PrewarmSeconds );

	[Button]
	public void ForceTransitionAlt() => RequestSceneTransition( NewSceneAlt ?? NewScene, useAltOverlay: true, PrewarmSeconds );

	private void RequestSceneTransition( SceneFile targetScene, bool useAltOverlay, float prewarmSeconds )
	{
		if ( _phase != TransitionPhase.Idle )
			return;

		if ( targetScene == null )
			return;

		var options = new SceneLoadOptions
		{
			DeleteEverything = true,
			ShowLoadingScreen = true,
		};

		options.SetScene( targetScene );

		options.LoadingOverlayPanelTypeNameOverride = useAltOverlay
			? "Sweeper.UI.Loading.AlternativeLoading"
			: null;

		options.MinimumLoadingScreenSeconds = useAltOverlay
			? 1f
			: ProjectSettings.Loading.SceneTransitionMinimumVisibleSeconds;

		_pendingOptions = options;

		if ( prewarmSeconds > 0.0f )
		{
			var context = LoadingScreen.Context.SceneTransition;

			var overlayTypeToPrime =
				options.LoadingOverlayPanelTypeNameOverride
				?? ProjectSettings.Loading?.GetPolicy( LoadingSettings.LoadingContext.SceneTransition ).OverlayPanelTypeName;

			LoadingOverlayState.PrewarmRequested = true;
			LoadingScreen.PrimeOverlay( context, overlayTypeToPrime );
			LoadingScreen.IsVisible = true;

			_requestedPrewarmSeconds = prewarmSeconds;
			_overlayWaitUntil = RealTime.Now + MathF.Max( 0.0f, OverlayWaitTimeoutSeconds );
			_prewarmUntil = 0.0f;
			_phase = TransitionPhase.WaitingForOverlay;
			return;
		}

		if ( !Game.ChangeScene( _pendingOptions ) )
		{
			_pendingOptions = default;
			_phase = TransitionPhase.Idle;
			return;
		}

		_pendingOptions = default;
		_requestedPrewarmSeconds = 0.0f;
		_overlayWaitUntil = 0.0f;
		_prewarmUntil = 0.0f;
		_phase = TransitionPhase.StartedLoad;
	}

	private void UpdateTransition()
	{
		switch ( _phase )
		{
			case TransitionPhase.Idle:
				return;

			case TransitionPhase.WaitingForOverlay:
			{
				EnsureLoadingScreenVisible();

				var uiRendered = LoadingScreen.VisibleSince > 0.0f;
				var timedOut = RealTime.Now >= _overlayWaitUntil;

				if ( uiRendered || timedOut )
				{
					_prewarmUntil = RealTime.Now + MathF.Max( 0.0f, _requestedPrewarmSeconds );
					_phase = TransitionPhase.WaitingForPrewarm;
				}

				return;
			}

			case TransitionPhase.WaitingForPrewarm:
			{
				EnsureLoadingScreenVisible();

				if ( RealTime.Now < _prewarmUntil )
					return;

				var started = Game.ChangeScene( _pendingOptions );
				_pendingOptions = default;

				if ( !started )
				{
					LoadingOverlayState.ClearPrewarm();
					_phase = TransitionPhase.Idle;
					return;
				}

				_phase = TransitionPhase.StartedLoad;
				return;
			}

			case TransitionPhase.StartedLoad:
			{
				// Overlay handles clearing prewarm at end of real loading.
				if ( !LoadingScreen.IsVisible && !LoadingOverlayState.PrewarmRequested )
				{
					_phase = TransitionPhase.Idle;
					_overlayWaitUntil = 0.0f;
					_prewarmUntil = 0.0f;
					_requestedPrewarmSeconds = 0.0f;
				}

				return;
			}

			default:
				_phase = TransitionPhase.Idle;
				return;
		}
	}

	private void EnsureLoadingScreenVisible()
	{
		// Keep the engine flag asserted during the prewarm window so the overlay can become "live"
		// (VisibleSince > 0) before starting a potentially fast load.
		LoadingScreen.IsVisible = true;

		if ( string.IsNullOrWhiteSpace( LoadingScreen.Title ) )
			LoadingScreen.Title = "Loading Scene";
	}

}
