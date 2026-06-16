namespace Editor.ProjectSettingPages;

internal sealed class ObjectPlacerGroupDropdownWidget : ControlWidget
{
	readonly IReadOnlyList<ObjectPlacerGroup> _groups;
	PopupWidget _menu;

	public override bool IsControlActive => base.IsControlActive || _menu.IsValid();
	public override bool IsControlButton => true;
	public override bool IsControlHovered => base.IsControlHovered || _menu.IsValid();

	public ObjectPlacerGroupDropdownWidget( SerializedProperty property, IReadOnlyList<ObjectPlacerGroup> groups ) : base( property )
	{
		_groups = groups;
		Cursor = CursorShape.Finger;
		MinimumWidth = 0;
		MaximumHeight = Theme.RowHeight;
		HorizontalSizeMode = SizeMode.CanShrink | SizeMode.CanGrow;
	}

	protected override void PaintControl()
	{
		var color = IsControlHovered ? Theme.Blue : Theme.TextControl;
		if ( IsControlDisabled )
			color = color.WithAlpha( 0.5f );

		var rect = LocalRect.Shrink( 8, 0 );
		var group = GetSelectedGroup();
		var icon = group?.Icon ?? "folder_off";

		Paint.SetPen( color.WithAlpha( 0.5f ) );
		var iconRect = Paint.DrawIcon( rect, icon, 16, TextFlag.LeftCenter );
		rect.Left += iconRect.Width + 8;

		Paint.SetPen( color );
		Paint.DrawText( rect, group?.Name ?? "None", TextFlag.LeftCenter | TextFlag.SingleLine );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}

	public override void StartEditing()
	{
		if ( IsControlDisabled )
			return;

		if ( !_menu.IsValid )
			OpenMenu();
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		if ( IsControlDisabled )
			return;

		if ( e.LeftMouseButton && !_menu.IsValid() )
			OpenMenu();
	}

	protected override void OnDoubleClick( MouseEvent e )
	{
		// nothing
	}

	void OpenMenu()
	{
		PropertyStartEdit();

		_menu = new PopupWidget( null );
		_menu.Layout = Layout.Column();
		_menu.MinimumWidth = ScreenRect.Width;
		_menu.MaximumWidth = ScreenRect.Width;
		_menu.OnLostFocus += PropertyFinishEdit;
		_menu.VerticalSizeMode = SizeMode.CanGrow | SizeMode.Expand;

		var scroller = _menu.Layout.Add( new ScrollArea( this ), 1 );
		scroller.NoSystemBackground = true;
		scroller.TranslucentBackground = true;
		scroller.Canvas = new Widget( scroller )
		{
			Layout = Layout.Column(),
			VerticalSizeMode = SizeMode.CanGrow | SizeMode.Expand,
			MaximumWidth = ScreenRect.Width
		};

		var height = 0f;
		AddOption( scroller.Canvas, null, "None", "folder_off", ref height );
		foreach ( var group in _groups )
			AddOption( scroller.Canvas, group.Id, string.IsNullOrWhiteSpace( group.Name ) ? "Unnamed Group" : group.Name, string.IsNullOrWhiteSpace( group.Icon ) ? "folder" : group.Icon, ref height );

		scroller.Canvas.AdjustSize();
		_menu.Position = ScreenRect.BottomLeft;
		_menu.Visible = true;
		_menu.AdjustSize();
		_menu.ConstrainToScreen();
		_menu.OnPaintOverride = PaintMenuBackground;

		if ( height < 200 )
		{
			scroller.FixedHeight = height;
			_menu.FixedHeight = height;
		}

		if ( scroller.VerticalScrollbar.Minimum != scroller.VerticalScrollbar.Maximum )
			scroller.Canvas.MaximumWidth -= 8;
	}

	void AddOption( Widget canvas, string groupId, string title, string icon, ref float height )
	{
		var option = canvas.Layout.Add( new ObjectPlacerGroupMenuOption( groupId, title, icon, SerializedProperty ) );
		option.MouseLeftPress = () =>
		{
			SerializedProperty.SetValue( groupId );
			SignalValuesChanged();
			_menu.Close();
		};
		height += option.FixedHeight;
	}

	bool PaintMenuBackground()
	{
		Paint.SetBrushAndPen( Theme.ControlBackground, Theme.WidgetBackground, 1 );
		Paint.DrawRect( Paint.LocalRect.Shrink( 1 ), 4 );
		return true;
	}

	ObjectPlacerGroup GetSelectedGroup()
	{
		var groupId = SerializedProperty.GetValue<string>();
		return string.IsNullOrWhiteSpace( groupId )
			? null
			: _groups.FirstOrDefault( group => group.Id == groupId );
	}
}

file sealed class ObjectPlacerGroupMenuOption : Widget
{
	readonly string _groupId;
	readonly SerializedProperty _property;

	public ObjectPlacerGroupMenuOption( string groupId, string title, string icon, SerializedProperty property ) : base( null )
	{
		_groupId = groupId;
		_property = property;

		Layout = Layout.Row();
		Layout.Margin = 0;
		VerticalSizeMode = SizeMode.Default;
		FixedHeight = Theme.RowHeight;
		Cursor = CursorShape.Finger;

		if ( !string.IsNullOrWhiteSpace( icon ) )
		{
			Layout.AddSpacingCell( 4 );
			Layout.Add( new ObjectPlacerGroupMenuIcon( icon ) { FixedSize = Theme.RowHeight } );
		}

		var column = Layout.AddColumn();
		column.Margin = new Sandbox.UI.Margin( 8, 4 );
		column.Add( new Label( title ) { Color = Theme.Text } );
	}

	bool HasValue()
	{
		var current = _property.GetValue<string>();
		return string.Equals( current, _groupId, StringComparison.Ordinal );
	}

	protected override void OnPaint()
	{
		if ( Paint.HasMouseOver || HasValue() )
		{
			Paint.SetBrushAndPen( Theme.Blue.WithAlpha( HasValue() ? 0.5f : 0.1f ) );
			Paint.DrawRect( LocalRect );
		}
	}
}

file sealed class ObjectPlacerGroupMenuIcon : Widget
{
	readonly string _icon;

	public ObjectPlacerGroupMenuIcon( string icon ) : base( null )
	{
		_icon = icon;
		TransparentForMouseEvents = true;
	}

	protected override void OnPaint()
	{
		var rect = LocalRect.Shrink( 2 );
		if ( ObjectPlacerPreviewIcons.DrawConfiguredIcon( rect, _icon, 12 ) )
			return;

		Paint.SetPen( Theme.Text.WithAlpha( 0.85f ) );
		Paint.DrawIcon( rect, string.IsNullOrWhiteSpace( _icon ) ? "folder" : _icon, 12, TextFlag.Center );
	}
}
