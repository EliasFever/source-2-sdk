namespace Editor;

public partial class SceneViewportWidget
{
	/// <summary>
	/// Is this viewport the game view?
	/// </summary>
	public bool IsGameView { get; private set; }

	/// <summary>
	/// Is the active game render target external to this viewport?
	/// </summary>
	public bool IsExternalGameView { get; private set; }

	/// <summary>
	/// Called when the SceneView's view mode changes.
	/// </summary>
	public void OnViewModeChanged( SceneViewWidget.ViewMode viewMode )
	{
		Renderer.Scene = Session.Scene;
		GizmoInstance.Selection = Session.Selection;

		if ( _editorCamera.IsValid() && _editorCamera.Scene != Session.Scene )
		{
			// make sure the editor camera exists in the correct scene
			_editorCamera.DestroyGameObject();
			_editorCamera = Renderer.CreateSceneEditorCamera();
		}

		_activeCamera = viewMode switch
		{
			SceneViewWidget.ViewMode.Game when IsGameView => null,
			SceneViewWidget.ViewMode.GameEjected => _ejectCamera,
			_ => _editorCamera,
		};

		Renderer.Camera = _activeCamera;
		Renderer.EnableEngineOverlays = IsGameView;
		Renderer.Visible = !(viewMode == SceneViewWidget.ViewMode.Game && IsExternalGameView);
		ViewportOptions.Visible = viewMode != SceneViewWidget.ViewMode.Game;
	}

	/// <summary>
	/// Set this viewport as the game view.
	/// </summary>
	public void SetGameView( SceneRenderingWidget playWidget = null, bool showPlaceholder = false )
	{
		playWidget ??= Renderer;

		GameMode.SetPlayWidget( playWidget );
		IsGameView = true;
		IsExternalGameView = !ReferenceEquals( playWidget, Renderer ) && showPlaceholder;
		if ( !IsExternalGameView )
		{
			Renderer.Visible = true;
		}
		Tools.DisposeAll();
	}

	/// <summary>
	/// Clear this viewport as the game view.
	/// </summary>
	public void ClearGameView()
	{
		GameMode.ClearPlayMode();
		IsGameView = false;
		IsExternalGameView = false;
		Renderer.Visible = true;

		SetDefaultSize();
	}

	/// <summary>
	/// Called when ejecting from the game state.
	/// </summary>
	public void OnEject()
	{
		GameMode.ClearPlayMode();
		IsGameView = false;
		IsExternalGameView = false;
		Renderer.Visible = true;

		SetDefaultSize();

		var hasExistingEjectCamera = _ejectCamera.IsValid();
		var shouldSnapToGameplayCamera = SceneView.EjectMode == SceneViewWidget.EjectCameraMode.ResetFromGameplayCamera
			|| !hasExistingEjectCamera;

		if ( shouldSnapToGameplayCamera )
		{
			var gameCamera = Renderer.Scene.Camera;
			if ( gameCamera.IsValid() )
			{
				// Start eject camera from gameplay camera transform.
				State.CameraPosition = gameCamera.WorldPosition;
				State.CameraRotation = gameCamera.WorldRotation;
			}
		}

		if ( !hasExistingEjectCamera )
			_ejectCamera = Renderer.CreateSceneEditorCamera();
	}

	/// <summary>
	/// Called when possessing back into the game state.
	/// </summary>
	public void OnPossessGame()
	{
		GameMode.SetPlayWidget( Renderer );
		IsGameView = true;
		IsExternalGameView = false;
		Renderer.Visible = true;
		Tools.DisposeAll();
	}
}
