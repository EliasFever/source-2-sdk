using Editor.Preferences;

namespace Editor;

public partial class ViewportOptions : Widget
{
	SceneViewportWidget SceneViewportWidget;
	Button ViewSettingsButton;
	IconButton SceneViewButton;
	readonly Color ButtonBackgroundColor = "#404040";
	readonly Color ButtonOutlineColor = "#5b5b5b";
	public string ViewSettingsButtonRightIcon { get; set; } = "arrow_drop_down";

	SceneViewportWidget.ViewMode? LastViewMode;
	SceneCameraDebugMode? LastRenderMode;
	bool? LastUseSboxViewportToolbars;

	[Title( "Field Of View" )]
	[Range( 1.0f, 180.0f )]
	[Step( 0.1f )]
	public float CameraFieldOfView
	{
		get => EditorPreferences.CameraFieldOfView;
		set => EditorPreferences.CameraFieldOfView = value;
	}

	bool UseSboxViewportToolbars => EditorToolBars.ShowLegacyToolbar;

	public ViewportOptions( SceneViewportWidget sceneViewportWidget )
	{
		SceneViewportWidget = sceneViewportWidget;
		Layout = Layout.Row();
		Layout.Spacing = 4;

		Rebuild();
	}

	[EditorEvent.Hotload]
	public void Rebuild()
	{
		Layout.Clear( true );
		LastUseSboxViewportToolbars = UseSboxViewportToolbars;

		var showOnlyViewSettingsButton = UseSboxViewportToolbars;

		if ( !showOnlyViewSettingsButton )
		{
			var so = EditorScene.GizmoSettings.GetSerialized();

			{
				var group = Layout.Add( AddGroup() );

				AddToggleButton(
					group.Layout,
					"Draw Gizmos",
					() => "touch_app",
					() => EditorScene.GizmoSettings.GizmosEnabled,
					( v ) => EditorScene.GizmoSettings.GizmosEnabled = v
				);

				var b = AddButton(
					group.Layout,
					"Gizmo Settings...",
					"arrow_drop_down",
					() => SceneViewportWidget?.SceneView?.ViewportTools?.OpenGizmosMenu()
				);

				b.FixedWidth = Theme.RowHeight * 0.5f;
			}

			{
				var group = Layout.Add( AddGroup() );

				AddToggleButton(
					group.Layout,
					"Angle Snap",
					() => "rotate_90_degrees_cw",
					() => EditorScene.GizmoSettings.SnapToAngles,
					( v ) => EditorScene.GizmoSettings.SnapToAngles = v
				);

				var angleStep = new ViewportAngleStepWidget( so.GetProperty( nameof( EditorScene.GizmoSettings.AngleSpacing ) ) )
				{
					ToolTip = "Angle Step",
					FixedWidth = 65
				};
				group.Layout.Add( angleStep );
			}

			{
				var group = Layout.Add( AddGroup() );

				AddToggleButton(
					group.Layout,
					"Grid Snap",
					() => "grid_on",
					() => EditorScene.GizmoSettings.SnapToGrid,
					( v ) => EditorScene.GizmoSettings.SnapToGrid = v
				);

				var snapStep = new ViewportSnapStepWidget( so.GetProperty( nameof( EditorScene.GizmoSettings.GridSpacing ) ) )
				{
					Min = 0.125f,
					Max = 128.0f,
					ToolTip = "Grid Step",
					FixedWidth = 65
				};

				group.Layout.Add( snapStep );
			}

		}

		if ( SceneViewportWidget.State.RenderMode == SceneCameraDebugMode.Albedo )
		{
			var albedoButton = Layout.Add( new IconButton( "palette", ToggleAlbedoChart )
			{
				ToolTip = "Toggle Albedo Chart",
				IsToggle = true,
				Background = ButtonBackgroundColor
			} );
			albedoButton.OnPaintOverride = () =>
			{
				Paint.ClearPen();
				Paint.SetBrushAndPen( ButtonBackgroundColor, ButtonOutlineColor );
				Paint.DrawRect( albedoButton.LocalRect, Theme.ControlRadius );
				var iconColor = albedoButton.IsActive ? Theme.Text : Theme.TextButton.WithAlpha( 0.75f );
				if ( Paint.HasMouseOver ) iconColor = iconColor.Lighten( 0.1f );
				Paint.SetPen( iconColor );
				Paint.DrawIcon( albedoButton.LocalRect, "palette", albedoButton.IconSize, TextFlag.Center );
				return true;
			};
		}

		ViewSettingsButton = Layout.Add( new Button( "ViewSettingsButton.Text" )
		{
			ToolTip = "View Settings",
			FixedHeight = Theme.RowHeight,
			FixedWidth = 170,
			Clicked = OpenViewSettings,
			Tint = "#404040"
		} );

		if ( !showOnlyViewSettingsButton )
		{
			SceneViewButton = Layout.Add( new IconButton( "grid_view", () => SceneViewportWidget?.SceneView?.ViewportTools?.OpenSceneViewModeMenuForViewport( SceneViewButton ) )
			{
				ToolTip = "Layout",
				FixedHeight = Theme.RowHeight,
				FixedWidth = Theme.RowHeight,
				Background = ButtonBackgroundColor,
				IconSize = 14f
			} );
			SceneViewButton.OnPaintOverride = () =>
			{
				Paint.ClearPen();
				Paint.SetBrushAndPen( ButtonBackgroundColor, ButtonOutlineColor );
				Paint.DrawRect( SceneViewButton.LocalRect, Theme.ControlRadius );
				Paint.SetPen( Theme.TextButton.WithAlphaMultiplied( Paint.HasMouseOver ? 1.0f : 0.8f ) );
				Paint.DrawIcon( SceneViewButton.LocalRect, "grid_view", 14f, TextFlag.Center );
				return true;
			};
		}

		ViewSettingsButton.OnPaintOverride = () =>
		{
			Paint.ClearBrush();
			Paint.ClearPen();

			Paint.SetBrushAndPen( ButtonBackgroundColor, ButtonOutlineColor );
			Paint.DrawRect( ViewSettingsButton.LocalRect, 2.0f );

			Paint.ClearBrush();
			Paint.ClearPen();

			Paint.Pen = Theme.TextButton.WithAlphaMultiplied( Paint.HasMouseOver ? 1.0f : 0.7f );
			if ( !ViewSettingsButton.Enabled )
				Paint.Pen = Theme.TextButton.WithAlphaMultiplied( 0.25f );

			Paint.SetDefaultFont();

			var iconRect = ViewSettingsButton.LocalRect.Shrink( 6, 0 );
			iconRect.Left = iconRect.Right - 16;
			Paint.DrawIcon( iconRect, ViewSettingsButtonRightIcon, 14, TextFlag.Center );

			var textRect = ViewSettingsButton.LocalRect.Shrink( 8, 0 );
			textRect.Right -= 20;
			Paint.DrawText( textRect, ViewSettingsButton.Text, TextFlag.Center );
			return true;
		};

		LastViewMode = null;
		LastRenderMode = null;
		UpdateViewSettingsButtonText();
	}

	protected override void OnPaint()
	{

	}

	void OpenViewSettings()
	{
		var viewport = GetAncestor<SceneViewportWidget>();
		var so = viewport.State.GetSerialized();

		var menu = new ContextMenu( this );

		{
			// this whole menu should probably just be a popup

			var widget = new Widget( menu );
			widget.OnPaintOverride = () =>
			{
				Paint.SetBrushAndPen( Theme.WidgetBackground.WithAlpha( 0.5f ) );
				Paint.DrawRect( widget.LocalRect.Shrink( 2 ), 2 );
				return true;
			};
			var cs = new ControlSheet();

			cs.AddRow( so.GetProperty( nameof( SceneViewportWidget.ViewportState.View ) ) );
			cs.AddRow( so.GetProperty( nameof( SceneViewportWidget.ViewportState.WireframeMode ) ) );
			cs.AddRow( so.GetProperty( nameof( SceneViewportWidget.ViewportState.EnablePostProcessing ) ) );

			if ( viewport.SceneView.Session.Scene is PrefabScene )
			{
				cs.AddRow( so.GetProperty( nameof( SceneViewportWidget.ViewportState.EnablePrefabLighting ) ) );
			}

			cs.AddRow( so.GetProperty( nameof( SceneViewportWidget.ViewportState.ShowSkyIn2D ) ) );

			cs.AddRow( so.GetProperty( nameof( SceneViewportWidget.ViewportState.ShowGrid ) ) );
			cs.AddRow( so.GetProperty( nameof( SceneViewportWidget.ViewportState.GridOpacity ) ) );
			if ( viewport.State.View == SceneViewportWidget.ViewMode.Perspective )
			{
				cs.AddRow( so.GetProperty( nameof( SceneViewportWidget.ViewportState.GridAxis ) ) );
			}

			cs.AddProperty( this, x => x.CameraFieldOfView );
			cs.AddProperty( viewport.SceneView, x => x.EjectMode );
			//	cs.AddProperty( viewport.SceneView, x => x.ShowEjectCamera );

			widget.Layout = cs;

			widget.MaximumWidth = 400;

			menu.AddWidget( widget );
		}

		menu.AddSeparator();

		foreach ( var entry in EditorTypeLibrary.GetEnumDescription( typeof( SceneCameraDebugMode ) ) )
		{
			var val = (SceneCameraDebugMode)entry.ObjectValue;
			var o = menu.AddOption( entry.Title, entry.Icon, () => { viewport.State.RenderMode = val; Rebuild(); } );
			o.Checkable = true;
			o.Checked = viewport.State.RenderMode == val;
		}

		menu.OpenAt( ViewSettingsButton.ScreenRect.BottomLeft, false );
	}

	[EditorEvent.Frame]
	void Frame()
	{
		if ( !SceneViewportWidget.IsValid() )
			return;

		if ( LastUseSboxViewportToolbars != UseSboxViewportToolbars )
		{
			Rebuild();
			return;
		}

		if ( !ViewSettingsButton.IsValid() )
			return;

		var viewMode = SceneViewportWidget.State.View;
		var renderMode = SceneViewportWidget.State.RenderMode;

		if ( LastViewMode == viewMode && LastRenderMode == renderMode )
			return;

		LastViewMode = viewMode;
		LastRenderMode = renderMode;

		UpdateViewSettingsButtonText();
	}

	void UpdateViewSettingsButtonText()
	{
		if ( !ViewSettingsButton.IsValid() || !SceneViewportWidget.IsValid() )
			return;

		var viewMode = SceneViewportWidget.State.View;
		var renderMode = SceneViewportWidget.State.RenderMode;

		LastViewMode = viewMode;
		LastRenderMode = renderMode;

		var viewTitle = GetViewTitle( viewMode );
		var debugTitle = GetEnumTitle( renderMode );

		ViewSettingsButton.Text = $"{viewTitle}: {debugTitle}";
	}

	static string GetViewTitle( SceneViewportWidget.ViewMode viewMode )
	{
		return viewMode switch
		{
			SceneViewportWidget.ViewMode.Top2d => "Top",
			SceneViewportWidget.ViewMode.Front2d => "Front",
			SceneViewportWidget.ViewMode.Side2d => "Side",
			_ => GetEnumTitle( viewMode )
		};
	}

	static string GetEnumTitle<TEnum>( TEnum value ) where TEnum : struct, Enum
	{
		var entry = EditorTypeLibrary
			.GetEnumDescription( typeof( TEnum ) )
			.FirstOrDefault( x => Equals( x.ObjectValue, value ) );

		return entry.Title ?? value.ToString();
	}

	void ToggleAlbedoChart()
	{
		bool current = DebugOverlay.AlbedoChart;
		//		do the thing
		//		ConsoleSystem.SetValue( "r_albedo_chart", !current );
	}

	private Widget AddGroup()
	{
		var w = new Widget();
		w.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrushAndPen( ButtonBackgroundColor, ButtonOutlineColor );
			Paint.DrawRect( w.LocalRect, Theme.ControlRadius );
			return true;
		};

		w.FixedHeight = Theme.RowHeight;
		w.Layout = Layout.Row();
		w.Layout.Spacing = 6;
		w.Layout.Margin = new( 2, 0 );
		return w;
	}

	private EditorToolButton AddToggleButton( Layout layout, string tooltip, Func<string> getIcon, Func<bool> getVal, Action<bool> setVal )
	{
		var __getVal = () => { try { return getVal(); } catch ( System.Exception ) { return false; } };
		var __setVal = ( bool b ) => { try { setVal( b ); } catch ( System.Exception ) { } };

		var b = new EditorToolButton();
		b.GetIcon = getIcon;
		b.ToolTip = tooltip;
		b.Action = () => __setVal( !__getVal() );
		b.IsActive = () => __getVal();
		StyleToolbarButton( b, isToggle: true );

		layout.Add( b );
		return b;
	}

	private EditorToolButton AddButton( Layout layout, string tooltip, string getIcon, Action onClick )
	{
		var b = new EditorToolButton();
		b.GetIcon = () => getIcon;
		b.ToolTip = tooltip;
		b.Action = onClick;
		StyleToolbarButton( b, isToggle: false );

		layout.Add( b );
		return b;
	}

	void StyleToolbarButton( EditorToolButton button, bool isToggle )
	{
		button.OnPaintOverride = () =>
		{
			Paint.Antialiasing = true;
			Paint.TextAntialiasing = true;
			Paint.ClearPen();

			var active = button.IsActive?.Invoke() ?? false;
			var bg = active ? ButtonBackgroundColor.Lighten( 0.12f ) : ButtonBackgroundColor;
			if ( Paint.HasMouseOver )
				bg = bg.Lighten( 0.08f );

			Paint.SetBrushAndPen( bg, ButtonOutlineColor );
			Paint.DrawRect( button.LocalRect, Theme.ControlRadius );

			var iconColor = active
				? Theme.Text
				: (isToggle ? Theme.TextButton.WithAlpha( 0.4f ) : Theme.TextButton.WithAlpha( 0.75f ));
			if ( !button.Enabled ) iconColor = iconColor.WithAlpha( 0.35f );
			Paint.SetPen( iconColor );
			Paint.DrawIcon( button.LocalRect, button.GetIcon(), HeaderBarStyle.IconSize, TextFlag.Center );
			return true;
		};
	}
}

file class ViewportAngleStepWidget : ViewportSnapStepWidget
{
	private readonly float[] values =
	{
		0.25f,
		0.5f,
		1f,
		5f,
		15f,
		30f,
		45f,
		90f,
		180f
	};

	public ViewportAngleStepWidget( SerializedProperty property ) : base( property, "º" )
	{
	}

	public override void Decrease()
	{
		var value = SerializedProperty.GetValue<float>();
		var index = Array.IndexOf( values, values.OrderBy( a => MathF.Abs( value - a ) ).First() );
		if ( index > 0 ) index--;

		LineEdit.Blur();
		SerializedProperty.SetValue( values[index] );
	}

	public override void Increase()
	{
		var value = SerializedProperty.GetValue<float>();
		var index = Array.IndexOf( values, values.OrderBy( a => MathF.Abs( value - a ) ).First() );
		if ( index < values.Length - 1 ) index++;

		LineEdit.Blur();
		SerializedProperty.SetValue( values[index] );
	}
}

file class ViewportSnapStepWidget : ControlWidget
{
	protected LineEdit LineEdit;
	static readonly Color StepButtonBackgroundColor = "#404040";
	static readonly Color StepButtonOutlineColor = "#5b5b5b";

	public float Min { get; set; } = 0.25f;
	public float Max { get; set; } = 128f;

	public ViewportSnapStepWidget( SerializedProperty property, string suffix = null ) : base( property )
	{
		Layout = Layout.Row();
		OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrushAndPen( StepButtonBackgroundColor, StepButtonOutlineColor );
			Paint.DrawRect( LocalRect, Theme.ControlRadius );
			return true;
		};

		LineEdit = new LineEdit( this );
		LineEdit.TextEdited += ( text ) => property.SetValue<object>( float.TryParse( text, out float v ) ? v : text );
		LineEdit.MinimumSize = Theme.RowHeight;
		LineEdit.MaximumSize = new Vector2( 4096, Size.y );
		LineEdit.ReadOnly = ReadOnly;
		LineEdit.SetStyles( "background-color: transparent; vertical-align: middle; text-align: left;" );
		Layout.Add( LineEdit );

		if ( suffix is not null )
		{
			var label = new Label( this );
			label.Text = suffix;
			label.SetStyles( "background-color: transparent; vertical-align: middle; text-align: right;" );
			Layout.Add( label );
		}

		var buttons = Layout.AddColumn();

		var bIncrease = new IconButton( "keyboard_arrow_up", Increase );
		bIncrease.Background = "#404040";
		bIncrease.FixedHeight = Theme.ControlHeight / 2;
		bIncrease.FixedWidth = 20;
		bIncrease.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrushAndPen( StepButtonBackgroundColor, StepButtonOutlineColor );
			Paint.DrawRect( bIncrease.LocalRect, 2.0f );
			Paint.SetPen( Theme.TextButton.WithAlphaMultiplied( Paint.HasMouseOver ? 1.0f : 0.8f ) );
			Paint.DrawIcon( bIncrease.LocalRect, "keyboard_arrow_up", bIncrease.IconSize, TextFlag.Center );
			return true;
		};
		buttons.Add( bIncrease );

		var bDecrease = new IconButton( "keyboard_arrow_down", Decrease );
		bDecrease.Background = "#404040";
		bDecrease.FixedHeight = Theme.ControlHeight / 2;
		bDecrease.FixedWidth = 20;
		bDecrease.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrushAndPen( StepButtonBackgroundColor, StepButtonOutlineColor );
			Paint.DrawRect( bDecrease.LocalRect, 2.0f );
			Paint.SetPen( Theme.TextButton.WithAlphaMultiplied( Paint.HasMouseOver ? 1.0f : 0.8f ) );
			Paint.DrawIcon( bDecrease.LocalRect, "keyboard_arrow_down", bDecrease.IconSize, TextFlag.Center );
			return true;
		};
		buttons.Add( bDecrease );

		LineEdit.Text = property.GetValue<float>().ToString();
	}

	protected override void OnValueChanged()
	{
		base.OnValueChanged();

		if ( LineEdit.IsFocused )
			return;

		LineEdit.Text = SerializedProperty.GetValue<float>().ToString();
		LineEdit.CursorPosition = 0;
	}

	public virtual void Decrease()
	{
		var value = SerializedProperty.GetValue<float>();
		if ( value <= Min )
			return;

		LineEdit.Blur();
		SerializedProperty.SetValue( value / 2.0f );
	}

	public virtual void Increase()
	{
		var value = SerializedProperty.GetValue<float>();
		if ( value >= Max )
			return;

		LineEdit.Blur();
		SerializedProperty.SetValue( value * 2 );
	}
}
