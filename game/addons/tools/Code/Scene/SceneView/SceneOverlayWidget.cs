namespace Editor;

public class SceneOverlayWidget : Widget
{
	public static SceneOverlayWidget Active { get; private set; }

	public Layout Header { get; private set; }

	internal SceneOverlayWidget( Widget parent ) : base( parent )
	{
		TranslucentBackground = true;
		NoSystemBackground = true;

		WindowFlags = WindowFlags.FramelessWindowHint | WindowFlags.Tool;

		Active = this;

		Layout = Layout.Column();
		Layout.Margin = 8;

		var header = Layout.AddRow();
		header.AddStretchCell();
		Header = header.AddRow();
		Header.Spacing = 4;

		Layout.AddStretchCell();

		// doesn't handle floating windows, but there's no way to hook into dockwrapper events right now
		EditorWindow.Moved += UpdateDimensions;

		TransparentForMouseEvents = true;
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		if ( EditorWindow.IsValid() )
		{
			EditorWindow.Moved -= UpdateDimensions;
		}
	}

	int lastGeometryHash = -1;

	[EditorEvent.Frame]
	private void UpdateDimensions()
	{
		if ( !Parent.IsValid() )
			return;

		// this wasn't always being triggered properly when relying on widget events from the parent (causing HUGE jank)
		int geometryHash = HashCode.Combine( Parent.ScreenPosition, Parent.Size );
		if ( lastGeometryHash != geometryHash )
		{
			Position = Parent.ScreenPosition;
			Size = Parent.Size;
		}

		lastGeometryHash = geometryHash;
	}

	internal RealTimeSince timeSinceNeededRedraw = 0.0f;

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( timeSinceNeededRedraw > 0.1f
			|| SceneOverlayNotifications.ShouldAnimate
			|| SceneEditorExtensions.ShouldDrawCenteredFlyCursor )
		{
			Update();
			timeSinceNeededRedraw = 0.0f;
		}
	}

	protected override void OnPaint()
	{
		Active = this;

		if ( Parent is SceneViewportWidget vw )
		{
			if ( vw.SceneView.CurrentView == SceneViewWidget.ViewMode.Game )
			{
				EditorEvent.Run( "sceneview.paintoverlay" );
			}
		}

		SceneOverlayNotifications.Draw( this );

		if ( SceneEditorExtensions.ShouldDrawCenteredFlyCursor )
		{
			DrawCenteredCursor();
		}
	}

	private void DrawCenteredCursor()
	{
		var center = new Vector2( Size.x * 0.5f, Size.y * 0.5f );
		var line = 7.0f;
		var gap = 3.0f;
		var thickness = 1.5f;

		Paint.SetPen( Color.White.WithAlpha( 0.9f ), thickness );
		Paint.DrawLine( center + new Vector2( -line, 0 ), center + new Vector2( -gap, 0 ) );
		Paint.DrawLine( center + new Vector2( gap, 0 ), center + new Vector2( line, 0 ) );
		Paint.DrawLine( center + new Vector2( 0, -line ), center + new Vector2( 0, -gap ) );
		Paint.DrawLine( center + new Vector2( 0, gap ), center + new Vector2( 0, line ) );
	}
}
