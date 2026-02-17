namespace Editor;

public partial class SceneViewWidget
{
	private const string PopupGameWindowCookie = "SceneView.PopupGameWindow";

	private SceneGamePopupWindow _gamePopupWindow;
	private bool _closingGamePopupProgrammatically;

	public enum ViewMode
	{
		Scene,
		Game,
		GameEjected
	}

	public ViewMode CurrentView { get; private set; }
	private ViewMode lastView;

	private SceneViewportWidget _gameViewport;

	public static bool UsePopupGameWindow
	{
		get => ProjectCookie.Get( PopupGameWindowCookie, false );
		set
		{
			ProjectCookie.Set( PopupGameWindowCookie, value );
			Current?.OnPopupGameWindowPreferenceChanged();
		}
	}

	[Event( "scene.play" )]
	public void OnScenePlay()
	{
		if ( !Session.IsPlaying ) return;
		CurrentView = ViewMode.Game;

		_gameViewport = _viewports.FirstOrDefault().Value;
		if ( !_gameViewport.IsValid() )
		{
			OnViewModeChanged();
			return;
		}

		if ( UsePopupGameWindow )
		{
			StartGameInPopupWindow();
		}
		else
		{
			_gameViewport.SetGameView();
		}

		OnViewModeChanged();
		if ( !_gameViewport.IsExternalGameView )
		{
			ViewportTools.UpdateViewportFromCookie();
		}
	}

	[Event( "scene.stop" )]
	public void OnSceneStop()
	{
		CloseGamePopupWindow( true );
		CurrentView = ViewMode.Scene;

		if ( _gameViewport.IsValid() )
		{
			_gameViewport.ClearGameView();
			_gameViewport = null;
		}

		OnViewModeChanged();
	}

	public void ToggleEject()
	{
		if ( !Session.IsPlaying ) return;
		if ( !_gameViewport.IsValid() ) return;

		CurrentView = CurrentView == ViewMode.Game ? ViewMode.GameEjected : ViewMode.Game;

		if ( CurrentView == ViewMode.Game )
		{
			if ( UsePopupGameWindow )
			{
				StartGameInPopupWindow();
			}
			else
			{
				CloseGamePopupWindow( true );
				_gameViewport.OnPossessGame();
			}
		}
		else if ( CurrentView == ViewMode.GameEjected )
		{
			CloseGamePopupWindow( true );
			_gameViewport.OnEject();
		}

		OnViewModeChanged();

		if ( CurrentView == ViewMode.Game && !_gameViewport.IsExternalGameView )
		{
			ViewportTools.UpdateViewportFromCookie();
		}
	}

	/// <summary>
	/// Current view mode changed
	/// </summary>
	void OnViewModeChanged()
	{
		ViewportTools.Rebuild();
		UpdateSidebarVisibility();

		foreach ( var viewport in _viewports.Values )
		{
			viewport.OnViewModeChanged( CurrentView );
		}
	}

	public SceneViewportWidget GetGameTarget()
	{
		return _gameViewport;
	}

	/// <summary>
	/// Set the game viewport to free sizing mode
	/// </summary>
	public void SetFreeSize()
	{
		var viewport = GetGameTarget();
		if ( viewport.IsValid() )
		{
			viewport.SetDefaultSize();
		}
	}

	/// <summary>
	/// Set the game viewport to a specific aspect ratio
	/// </summary>
	public void SetForceAspect( float aspect )
	{
		var viewport = GetGameTarget();
		if ( viewport.IsValid() )
		{
			viewport.SetAspectRatio( aspect );
		}
	}

	/// <summary>
	/// Set the game viewport to a specific resolution
	/// </summary>
	public void SetForceResolution( Vector2 resolution )
	{
		var viewport = GetGameTarget();
		if ( viewport.IsValid() )
		{
			viewport.SetResolution( resolution );
		}
	}

	private void StartGameInPopupWindow()
	{
		if ( !_gameViewport.IsValid() )
			return;

		CloseGamePopupWindow( true );

		_gamePopupWindow = new SceneGamePopupWindow( Session.Scene, OnGamePopupWindowClosed );
		_gameViewport.SetGameView( _gamePopupWindow.Renderer, showPlaceholder: true );
	}

	private void CloseGamePopupWindow( bool programmatically )
	{
		if ( !_gamePopupWindow.IsValid() )
			return;

		_closingGamePopupProgrammatically = programmatically;
		_gamePopupWindow.Close();
		if ( programmatically )
		{
			_gamePopupWindow = null;
		}
	}

	private void OnGamePopupWindowClosed()
	{
		var wasProgrammaticClose = _closingGamePopupProgrammatically;
		_closingGamePopupProgrammatically = false;
		_gamePopupWindow = null;

		if ( wasProgrammaticClose )
			return;

		if ( !_gameViewport.IsValid() || !Session.IsPlaying || CurrentView != ViewMode.Game )
			return;

		MainThread.Queue( () =>
		{
			if ( !_gameViewport.IsValid() || !Session.IsPlaying || CurrentView != ViewMode.Game )
				return;

			if ( EditorWindow.IsValid() )
			{
				EditorWindow.Focus( true );
			}

			RestoreGameToMainViewport();

			MainThread.Queue( () =>
			{
				if ( !_gameViewport.IsValid() || !Session.IsPlaying || CurrentView != ViewMode.Game )
					return;

				_gameViewport.Renderer.Blur();
				_gameViewport.Renderer.Focus();
			} );
		} );
	}

	private void OnPopupGameWindowPreferenceChanged()
	{
		if ( !_gameViewport.IsValid() || !Session.IsPlaying || CurrentView != ViewMode.Game )
			return;

		if ( UsePopupGameWindow )
		{
			StartGameInPopupWindow();
		}
		else
		{
			CloseGamePopupWindow( true );
			RestoreGameToMainViewport();
		}

		OnViewModeChanged();
	}

	private void RestoreGameToMainViewport()
	{
		if ( !_gameViewport.IsValid() || !Session.IsPlaying || CurrentView != ViewMode.Game )
			return;

		_gameViewport.OnPossessGame();
		OnViewModeChanged();
		ViewportTools.UpdateViewportFromCookie();
	}
}
