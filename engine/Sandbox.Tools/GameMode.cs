using Native;

namespace Editor;

/// <summary>
/// Registers a widget with the input system, so it uses SDL.
/// </summary>
public static class GameMode
{
	static Widget _inPlay;
	static Widget _focusSource;
	static nint _registeredHostWindowId;
	static nint _registeredRenderWindowId;
	static nint _focusWindowId;
	static nint _editorMainWindowId;
	static bool _switchedEditorMainWindow;
	static bool _ownsHostWindowRegistration;

	/// <summary>
	/// Is a render widget the active play widget
	/// </summary>
	internal static bool IsPlayWidget( SceneRenderingWidget widget ) => widget == _inPlay;

	/// <summary>
	/// Given a widget, register it for SDL input, and tell the engine this is the swapchain we have
	/// </summary>
	/// <param name="widget"></param>
	public static void SetPlayWidget( SceneRenderingWidget widget )
	{
		var renderWindowId = (nint)widget._widget.winId();
		var hostWindow = widget.GetWindow() ?? widget;
		var hostWindowId = (nint)hostWindow._widget.winId();

		if ( _inPlay == widget && _registeredHostWindowId == hostWindowId && _registeredRenderWindowId == renderWindowId )
			return;

		UnregisterCurrent();

		_focusSource = widget;
		_focusSource.Focused += WidgetFocused;
		_focusSource.Blurred += WidgetBlurred;
		_editorMainWindowId = GetEditorMainWindowId();

		// Vanilla behavior: register and focus the actual render widget window.
		NativeEngine.InputSystem.RegisterWindowWithSDL( renderWindowId );
		_registeredRenderWindowId = renderWindowId;

		// Popup behavior: if host is not the editor main window, register/own it.
		_ownsHostWindowRegistration = hostWindowId != 0
			&& hostWindowId != renderWindowId
			&& !IsEditorMainWindow( hostWindowId );

		if ( _ownsHostWindowRegistration )
		{
			NativeEngine.InputSystem.RegisterWindowWithSDL( hostWindowId );
			_registeredHostWindowId = hostWindowId;

			if ( _editorMainWindowId != 0 && _editorMainWindowId != hostWindowId )
			{
				NativeEngine.InputSystem.SetEditorMainWindow( hostWindowId );
				_switchedEditorMainWindow = true;
			}
		}

		g_pEngineServiceMgr.SetEngineState( renderWindowId, widget.SwapChain );

		_focusWindowId = renderWindowId;
		_inPlay = widget;

		// For embedded viewport play, keep vanilla behavior and force a refocus.
		// For popup mode, don't auto-focus/capture; let the user click into the render area.
		if ( hostWindowId == renderWindowId )
		{
			widget.Blur();
			widget.Focus();
		}
	}

	public static void ClearPlayMode()
	{
		UnregisterCurrent();
	}

	/// <summary>
	/// When the editor gains focus of the game widget, tell the input system so it'll mouse capture (if it wants to)
	/// </summary>
	private static void WidgetFocused( FocusChangeReason reason )
	{
		if ( _focusWindowId == 0 )
			return;

		NativeEngine.InputSystem.OnEditorGameFocusChange( _focusWindowId, true );
	}

	/// <summary>
	/// When the editor loses focus of the game widget, tell the input system so it stops trying to do mouse capture.
	/// </summary>
	private static void WidgetBlurred( FocusChangeReason reason )
	{
		if ( _focusWindowId == 0 )
			return;

		NativeEngine.InputSystem.OnEditorGameFocusChange( _focusWindowId, false );
	}

	static void UnregisterCurrent()
	{
		if ( _inPlay.IsValid() )
		{
			_inPlay.Blur();
		}

		if ( _focusSource.IsValid() )
		{
			_focusSource.Focused -= WidgetFocused;
			_focusSource.Blurred -= WidgetBlurred;
		}

		if ( _focusWindowId != 0 )
		{
			NativeEngine.InputSystem.OnEditorGameFocusChange( _focusWindowId, false );
		}

		if ( _registeredRenderWindowId != 0 )
		{
			NativeEngine.InputSystem.UnregisterWindowFromSDL( _registeredRenderWindowId );
		}

		if ( _ownsHostWindowRegistration && _registeredHostWindowId != 0 )
		{
			NativeEngine.InputSystem.UnregisterWindowFromSDL( _registeredHostWindowId );
		}

		if ( _switchedEditorMainWindow && _editorMainWindowId != 0 )
		{
			NativeEngine.InputSystem.SetEditorMainWindow( _editorMainWindowId );
		}

		_inPlay = null;
		_focusSource = null;
		_registeredHostWindowId = 0;
		_registeredRenderWindowId = 0;
		_focusWindowId = 0;
		_editorMainWindowId = 0;
		_switchedEditorMainWindow = false;
		_ownsHostWindowRegistration = false;
	}

	static bool IsEditorMainWindow( nint windowId )
	{
		var editorMainId = GetEditorMainWindowId();
		return editorMainId != 0 && editorMainId == windowId;
	}

	static nint GetEditorMainWindowId()
	{
		var editorWindow = Sandbox.Internal.GlobalToolsNamespace.EditorWindow;
		if ( !editorWindow.IsValid() )
			return 0;

		return (nint)editorWindow._widget.winId();
	}
}
