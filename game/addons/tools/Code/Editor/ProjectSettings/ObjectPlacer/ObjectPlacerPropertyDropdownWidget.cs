namespace Editor.ProjectSettingPages;

internal readonly record struct ObjectPlacerEditablePropertyInfo( string Name, string Title );

internal sealed class ObjectPlacerPropertyDropdownWidget : ControlWidget
{
	readonly IReadOnlyList<ObjectPlacerEditablePropertyInfo> _properties;
	PopupWidget _menu;

	public override bool IsControlActive => base.IsControlActive || _menu.IsValid();
	public override bool IsControlButton => true;
	public override bool IsControlHovered => base.IsControlHovered || _menu.IsValid();

	public ObjectPlacerPropertyDropdownWidget( SerializedProperty property, IReadOnlyList<ObjectPlacerEditablePropertyInfo> properties ) : base( property )
	{
		_properties = properties;
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
		Paint.SetPen( color );
		Paint.DrawText( rect, GetSelectedTitle(), TextFlag.LeftCenter | TextFlag.SingleLine );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}

	public override void StartEditing()
	{
		if ( IsControlDisabled )
			return;

		if ( !_menu.IsValid() )
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
		foreach ( var property in _properties )
			AddOption( scroller.Canvas, property, ref height );

		scroller.Canvas.AdjustSize();
		_menu.Position = ScreenRect.BottomLeft;
		_menu.Visible = true;
		_menu.AdjustSize();
		_menu.ConstrainToScreen();
		_menu.OnPaintOverride = PaintMenuBackground;

		if ( height < 240 )
		{
			scroller.FixedHeight = height;
			_menu.FixedHeight = height;
		}

		if ( scroller.VerticalScrollbar.Minimum != scroller.VerticalScrollbar.Maximum )
			scroller.Canvas.MaximumWidth -= 8;
	}

	void AddOption( Widget canvas, ObjectPlacerEditablePropertyInfo property, ref float height )
	{
		var option = canvas.Layout.Add( new ObjectPlacerPropertyMenuOption( property, SerializedProperty ) );
		option.MouseLeftPress = () =>
		{
			SerializedProperty.SetValue( property.Name );
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

	string GetSelectedTitle()
	{
		var name = SerializedProperty.GetValue<string>();
		var property = _properties.FirstOrDefault( x => string.Equals( x.Name, name, StringComparison.Ordinal ) );
		return string.IsNullOrWhiteSpace( property.Title ) ? "Property" : property.Title;
	}
}

file sealed class ObjectPlacerPropertyMenuOption : Widget
{
	readonly ObjectPlacerEditablePropertyInfo _propertyInfo;
	readonly SerializedProperty _property;

	public ObjectPlacerPropertyMenuOption( ObjectPlacerEditablePropertyInfo propertyInfo, SerializedProperty property ) : base( null )
	{
		_propertyInfo = propertyInfo;
		_property = property;

		Layout = Layout.Row();
		Layout.Margin = 0;
		VerticalSizeMode = SizeMode.Default;
		FixedHeight = Theme.RowHeight;
		Cursor = CursorShape.Finger;

		var column = Layout.AddColumn();
		column.Margin = new Sandbox.UI.Margin( 8, 4 );
		column.Add( new Label( propertyInfo.Title ) { Color = Theme.Text } );
	}

	bool HasValue()
	{
		var current = _property.GetValue<string>();
		return string.Equals( current, _propertyInfo.Name, StringComparison.Ordinal );
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
