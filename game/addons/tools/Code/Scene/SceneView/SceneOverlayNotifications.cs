namespace Editor;

public static class SceneOverlayNotifications
{
	private sealed class OverlayNotice
	{
		public string Text { get; init; }
		public Pixmap Icon { get; init; }
		public float Duration { get; init; }
		public RealTimeSince SinceShown { get; set; }
	}

	private sealed class PersistentOverlayNotice
	{
		public string Text { get; set; }
		public Pixmap Icon { get; set; }
		public bool TargetVisible { get; set; }
		public float Visibility { get; set; }
	}

	private static readonly Dictionary<string, Pixmap> s_iconCache = new();
	private static OverlayNotice s_activeNotice;
	private static readonly Dictionary<string, PersistentOverlayNotice> s_persistentNotices = new();

	public static bool ShouldAnimate
	{
		get
		{
			if ( s_activeNotice is not null )
				return true;

			foreach ( var notice in s_persistentNotices.Values )
			{
				if ( notice.Visibility < 0.999f || !notice.TargetVisible )
					return true;
			}

			return false;
		}
	}

	public static void SetPersistent( string key, string text, string iconPath = null, bool enabled = true )
	{
		if ( string.IsNullOrWhiteSpace( key ) )
			return;

		if ( !s_persistentNotices.TryGetValue( key, out var notice ) )
		{
			notice = new PersistentOverlayNotice
			{
				Visibility = 0.0f,
				TargetVisible = false
			};
			s_persistentNotices[key] = notice;
		}

		if ( !enabled || string.IsNullOrWhiteSpace( text ) )
		{
			notice.TargetVisible = false;
			return;
		}

		notice.Text = text;
		notice.Icon = LoadIcon( iconPath );
		notice.TargetVisible = true;
	}

	public static void Show( string text, string iconPath = null, float duration = 1.5f )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return;

		var icon = LoadIcon( iconPath );
		s_activeNotice = new OverlayNotice
		{
			Text = text,
			Icon = icon,
			Duration = Math.Max( 0.4f, duration ),
			SinceShown = 0.0f
		};
	}

	public static void Draw( Widget widget )
	{
		if ( widget is null )
			return;

		if ( s_activeNotice is not null )
		{
			var notice = s_activeNotice;
			var elapsed = (float)notice.SinceShown;
			if ( elapsed >= notice.Duration )
			{
				s_activeNotice = null;
			}
			else
			{
				const float introDuration = 0.2f;
				const float outroDuration = 0.2f;
				var outroStart = Math.Max( introDuration, notice.Duration - outroDuration );

				float alpha;
				if ( elapsed < introDuration )
				{
					alpha = elapsed / introDuration;
				}
				else if ( elapsed > outroStart )
				{
					alpha = 1.0f - ((elapsed - outroStart) / Math.Max( 0.001f, notice.Duration - outroStart ));
				}
				else
				{
					alpha = 1.0f;
				}

				alpha = alpha.Clamp( 0.0f, 1.0f );
				if ( alpha > 0.0f )
				{
					var size = widget.Size;
					var iconSide = Math.Max( 26.0f, size.y * 0.06f );
					var iconSize = new Vector2( iconSide, iconSide );

					var offsetX = Math.Max( 18.0f, size.x * 0.03f );
					var offsetBottom = Math.Max( 18.0f, size.y * 0.04f );

					var startY = -iconSide;
					var endY = size.y - iconSide - offsetBottom;
					var y = MathX.Lerp( startY, endY, alpha );

					DrawNotice( notice, new Vector2( offsetX, y ), iconSize, alpha, size.x );
				}
			}
		}

		if ( s_persistentNotices.Count == 0 )
			return;

		var index = 0;
		var removeKeys = new List<string>();
		foreach ( var kv in s_persistentNotices )
		{
			var key = kv.Key;
			var notice = kv.Value;
			notice.Visibility = MathX.LerpTo( notice.Visibility, notice.TargetVisible ? 1.0f : 0.0f, RealTime.Delta * 12.0f );

			if ( !notice.TargetVisible && notice.Visibility < 0.01f )
			{
				removeKeys.Add( key );
				continue;
			}

			var size = widget.Size;
			var iconSide = Math.Max( 22.0f, size.y * 0.045f );
			var iconSize = new Vector2( iconSide, iconSide );
			var pos = new Vector2(
				Math.Max( 18.0f, size.x * 0.03f ),
				size.y - iconSide - Math.Max( 18.0f, size.y * 0.04f ) - (index * (iconSide + 8.0f)) );

			var y = MathX.Lerp( -iconSide, pos.y, notice.Visibility.Clamp( 0.0f, 1.0f ) );
			DrawNotice( notice.Text, notice.Icon, new Vector2( pos.x, y ), iconSize, notice.Visibility.Clamp( 0.0f, 1.0f ), size.x );
			index++;
		}

		foreach ( var key in removeKeys )
		{
			s_persistentNotices.Remove( key );
		}
	}

	private static void DrawNotice( OverlayNotice notice, Vector2 pos, Vector2 iconSize, float alpha, float width )
	{
		DrawNotice( notice.Text, notice.Icon, pos, iconSize, alpha, width );
	}

	private static void DrawNotice( string text, Pixmap icon, Vector2 pos, Vector2 iconSize, float alpha, float width )
	{
		Paint.Antialiasing = true;
		var iconRect = new Rect( pos, iconSize );

		if ( icon is not null )
		{
			Paint.Draw( iconRect, icon, alpha );
		}

		var textRect = new Rect(
			new Vector2( iconRect.Right + 10.0f, pos.y ),
			new Vector2( Math.Max( 180.0f, width * 0.35f ), iconSize.y ) );

		DrawOutlinedText( text, "Courier New", 10.0f, alpha, textRect );
	}

	private static void DrawOutlinedText( string text, string fontFamily, float fontSize, float alpha, Rect rect )
	{
		Paint.Antialiasing = false;
		Paint.TextAntialiasing = false;
		Paint.SetFont( fontFamily, fontSize, 400, false );

		Paint.SetPen( Color.Black.WithAlpha( alpha ) );
		Paint.DrawText( new Rect( rect.Position + new Vector2( -1, 0 ), rect.Size ), text, TextFlag.Left | TextFlag.CenterVertically );
		Paint.DrawText( new Rect( rect.Position + new Vector2( 1, 0 ), rect.Size ), text, TextFlag.Left | TextFlag.CenterVertically );
		Paint.DrawText( new Rect( rect.Position + new Vector2( 0, -1 ), rect.Size ), text, TextFlag.Left | TextFlag.CenterVertically );
		Paint.DrawText( new Rect( rect.Position + new Vector2( 0, 1 ), rect.Size ), text, TextFlag.Left | TextFlag.CenterVertically );

		Paint.SetPen( Color.White.WithAlpha( alpha ) );
		Paint.DrawText( rect, text, TextFlag.Left | TextFlag.CenterVertically );
	}

	private static Pixmap LoadIcon( string iconPath )
	{
		if ( string.IsNullOrWhiteSpace( iconPath ) )
			return null;

		if ( s_iconCache.TryGetValue( iconPath, out var cached ) )
			return cached;

		try
		{
			cached = Pixmap.FromFile( iconPath );
		}
		catch
		{
			cached = null;
		}

		s_iconCache[iconPath] = cached;
		return cached;
	}
}
