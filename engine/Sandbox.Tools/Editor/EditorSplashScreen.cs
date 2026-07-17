using NativeEngine;
using Sandbox.DataModel;
using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace Editor
{
	internal class EditorSplashScreen : Widget
	{
		internal static EditorSplashScreen Singleton;

		Pixmap BackgroundImage;

		const float ProgressAreaHeight = 14;
		const float LogOverlayHeight = 30;
		const float BottomAreaHeight = ProgressAreaHeight;
		const float ProgressInset = 2;
		Color ProgressTrackColor = new Color( 42f / 255f, 52f / 255f, 79f / 255f, 1f );
		Color ProgressFillColor = new Color( 52f / 255f, 80f / 255f, 160f / 255f, 1f );

		string PendingMessage = "Starting...";
		string DisplayedMessage = "Starting...";

		internal const string DefaultSplashScreen = "common/splash_screen.png";
		internal const string DefaultIcon = "common/logo.png";

		public EditorSplashScreen() : base( null, true )
		{
			WindowFlags = WindowFlags.Window | WindowFlags.Customized | WindowFlags.FramelessWindowHint | WindowFlags.MSWindowsFixedSizeDialogHint;

			Singleton = this;
			DeleteOnClose = true;

			string projectFile = Sandbox.Utility.CommandLine.GetSwitch( "-project", "" ).TrimQuoted();
			JsonElement root = default;

			if ( !string.IsNullOrEmpty( projectFile ) && File.Exists( projectFile ) )
			{
				using var doc = JsonDocument.Parse( File.ReadAllText( projectFile ) );
				root = doc.RootElement.Clone();
			}

			string projectName = ResolveProjectTitle( root );
			WindowTitle = $"Opening {projectName}";

			SetWindowIcon(
				EditorUtility.Projects.ResolveProjectAsset(
					root,
					projectFile,
					ProjectConfig.MetaIconKey,
					DefaultIcon,
					Pixmap.FromFile
				)
			);

			BackgroundImage = EditorUtility.Projects.ResolveProjectAsset(
				root,
				projectFile,
				ProjectConfig.MetaSplashKey,
				DefaultSplashScreen,
				Pixmap.FromFile
			);

			// We get the colors from the splash, so that the progress bar 
			// matches the overall tone of the image.
			UpdateProgressColorsFromSplash();

			string geometryCookie = EditorCookie.GetString( "splash.geometry", null );
			RestoreGeometry( geometryCookie );

			var aspect = (float)BackgroundImage.Height / BackgroundImage.Width;
			Size = new( 580, (580 * aspect).FloorToInt() + BottomAreaHeight );

			Show();
			UpdateGeometry();
			CenterWindow();
			Focus();

			if ( DpiScale != 1.0f )
			{
				BackgroundImage = BackgroundImage.Resize( BackgroundImage.Size * DpiScale );
			}

			WidgetUtil.MakeWindowDraggable( _widget );

			ConstrainToScreen();

			g_pToolFramework2.SetStallMonitorMainThreadWindow( _widget );
			Logging.OnMessage += OnConsoleMessage;
		}

		public override void OnDestroyed()
		{
			Logging.OnMessage -= OnConsoleMessage;

			base.OnDestroyed();
			Singleton = null;
		}

		void OnConsoleMessage( LogEvent e )
		{
			OnMessage( e.Message );

			g_pToolFramework2.Spin();
			NativeEngine.EngineGlobal.ToolsStallMonitor_IndicateActivity();
		}

		public static void StartupFinish()
		{
			if ( Singleton.IsValid() )
			{
				EditorCookie.Set( "splash.geometry", Singleton.SaveGeometry() );
				Singleton.Destroy();
			}

			Singleton = null;
		}

		public void OnMessage( string message )
		{
			PendingMessage = message;
			LatestMessage = message;
			Update();
		}

		string LatestMessage;
		float Progress;

		/// <summary>
		/// Updates the progress bar
		/// </summary>
		public static void SetProgress( float progress )
		{
			if ( !Singleton.IsValid() ) return;

			Singleton.Progress = progress.Clamp( 0f, 1f );
			Singleton.Update();
		}

		/// <summary>
		/// Set the current displayed message
		/// </summary>
		public static void SetMessage( string message )
		{
			if ( !Singleton.IsValid() ) return;

			Singleton.LatestMessage = message;
			Singleton.Update();

			g_pToolFramework2.Spin();
			NativeEngine.EngineGlobal.ToolsStallMonitor_IndicateActivity();
		}

		protected override bool OnClose()
		{
			return false;
		}

		protected override void OnPaint()
		{
			var imageRect = LocalRect;
			imageRect.Bottom -= BottomAreaHeight;
			Paint.Draw( imageRect, BackgroundImage );

			DisplayedMessage = PendingMessage;

			var logRect = new Rect( imageRect.Left, imageRect.Top, imageRect.Width, LogOverlayHeight );
			var progressAreaRect = new Rect( LocalRect.Left, imageRect.Bottom, LocalRect.Width, ProgressAreaHeight );

			Paint.ClearPen();
			Paint.SetBrush( Color.Black.WithAlpha( 0.55f ) );
			Paint.DrawRect( logRect );

			var textRect = logRect.Shrink( 8, 4 );

			Paint.SetPen( Color.White.WithAlpha( 0.85f ) );
			Paint.SetFont( "Century Gothic", 8, 400 );
			Paint.DrawText( textRect, LatestMessage ?? DisplayedMessage ?? "Bootstrapping..", TextFlag.LeftCenter );

			Paint.ClearPen();
			Paint.SetBrush( ProgressTrackColor );
			Paint.DrawRect( progressAreaRect );

			if ( Progress > 0f )
			{
				var fillRect = progressAreaRect.Shrink( ProgressInset );
				fillRect.Width *= Progress;

				Paint.SetBrush( ProgressFillColor );
				Paint.DrawRect( fillRect, 2.0f );
			}
		}

		private string ResolveProjectTitle( JsonElement root )
		{
			if ( root.TryGetProperty( "Title", out var titleProp ) )
				return titleProp.GetString();

			return "S&Box Editor";
		}

		void UpdateProgressColorsFromSplash()
		{
			if ( BackgroundImage is null || BackgroundImage.Width <= 0 || BackgroundImage.Height <= 0 )
				return;

			int stepX = Math.Max( 1, BackgroundImage.Width / 56 );
			int stepY = Math.Max( 1, BackgroundImage.Height / 56 );

			double sumR = 0;
			double sumG = 0;
			double sumB = 0;
			double weightSum = 0;

			for ( int y = 0; y < BackgroundImage.Height; y += stepY )
			{
				for ( int x = 0; x < BackgroundImage.Width; x += stepX )
				{
					var c = BackgroundImage.GetPixel( x, y );
					if ( c.a <= 0.01f )
						continue;

					double w = c.a;
					sumR += c.r * w;
					sumG += c.g * w;
					sumB += c.b * w;
					weightSum += w;
				}
			}

			if ( weightSum <= 0.0 )
				return;

			float avgR = (float)(sumR / weightSum);
			float avgG = (float)(sumG / weightSum);
			float avgB = (float)(sumB / weightSum);

			// Keep the splash tone, but nudge it brighter for a clearer progress fill.
			ProgressFillColor = new Color(
				Math.Min( 1f, avgR * 0.85f + 0.12f ),
				Math.Min( 1f, avgG * 0.85f + 0.12f ),
				Math.Min( 1f, avgB * 0.85f + 0.12f ),
				1f
			);

			// Same hue family, much darker for contrast against the fill.
			ProgressTrackColor = new Color(
				Math.Max( 0.03f, ProgressFillColor.r * 0.22f ),
				Math.Max( 0.03f, ProgressFillColor.g * 0.22f ),
				Math.Max( 0.03f, ProgressFillColor.b * 0.22f ),
				1f
			);
		}
	}
}
