namespace Editor;

internal sealed class SceneGamePopupWindow : Window
{
	public SceneRenderingWidget Renderer { get; }

	private readonly Action _onClosed;
	private readonly Scene _scene;
	private string _lastWindowTitle;

	public SceneGamePopupWindow( Scene scene, Action onClosed )
	{
		_onClosed = onClosed;
		_scene = scene;

		DeleteOnClose = true;
		WindowFlags = WindowFlags.Window | WindowFlags.WindowTitle | WindowFlags.WindowSystemMenuHint | WindowFlags.CloseButton
		| WindowFlags.MinimizeButton | WindowFlags.MaximizeButton | WindowFlags.WindowStaysOnTopHint;

		StatusBar = null; // Gets rid of the unwanted padding at the bottom of the window.

		var screenRect = EditorWindow.IsValid()
			? EditorWindow.ScreenGeometry
			: new Rect( Vector2.Zero, new Vector2( 1366, 768 ) );

		Size = screenRect.Size * 0.5f;
		if ( Size.x < 800 || Size.y < 450 )
		{
			Size = new Vector2( 800, 450 );
		}

		MinimumSize = new Vector2( 640, 360 );
		Position = screenRect.Position + (screenRect.Size - Size) * 0.5f;

		Canvas = new Widget( this )
		{
			Layout = Layout.Column(),
		};
		Canvas.Layout.Margin = 0;
		Canvas.Layout.Spacing = 0;

		Renderer = Canvas.Layout.Add( new SceneRenderingWidget() );
		Renderer.Scene = scene;
		Renderer.FocusMode = FocusMode.TabOrClickOrWheel;
		Renderer.HorizontalSizeMode = SizeMode.CanGrow | SizeMode.Expand;
		Renderer.VerticalSizeMode = SizeMode.CanGrow | SizeMode.Expand;

		RefreshWindowTitle();
		Show();
	}

	protected override void OnPaint()
	{
		base.OnPaint();
		RefreshWindowTitle();
	}

	protected override void OnResize()
	{
		base.OnResize();
		RefreshWindowTitle();
	}

	protected override void OnBlur( FocusChangeReason reason )
	{
		base.OnBlur( reason );
		Renderer?.Blur();
	}

	protected override void OnClosed()
	{
		base.OnClosed();
		_onClosed?.Invoke();
	}

	private void RefreshWindowTitle()
	{
		var config = Project.Current?.Config;
		var projectTitle = config?.Title;
		var projectLabel = string.IsNullOrWhiteSpace( projectTitle ) ? "Project" : projectTitle;

		var sceneName = string.IsNullOrWhiteSpace( _scene?.Name ) ? "Scene" : _scene.Name;

		var size = Renderer?.Size ?? Size;
		var width = Math.Max( 1, (int)size.x );
		var height = Math.Max( 1, (int)size.y );
		var resolution = $"{width}x{height}";

		var title = $"{projectLabel} - {sceneName} [{resolution}]";
		if ( title == _lastWindowTitle )
			return;

		_lastWindowTitle = title;
		WindowTitle = title;
	}
}
