namespace Editor.ProjectSettingPages;

internal sealed partial class ObjectPlacerCategory
{
	const int DetailLabelWidth = 110;

	void AddTextRow( Layout parent, string label, string value, Action<string> setter )
	{
		var row = AddLabeledRow( parent, label, spacing: 6 );
		var edit = row.Add( new LineEdit() { Text = value ?? "", MinimumWidth = 0, MaximumHeight = Theme.RowHeight }, 1 );
		edit.TextEdited += text =>
		{
			setter?.Invoke( text?.Trim() );
			StateHasChanged();
		};
	}

	void AddChoiceRow( Layout parent, string label, IReadOnlyList<DetailChoice> choices )
	{
		var row = AddLabeledRow( parent, label, spacing: 6 );
		var strip = row.AddRow( 1 );
		strip.Spacing = 1;

		foreach ( var choice in choices )
		{
			var button = strip.Add( new Button( choice.Text, choice.Icon )
			{
				FixedHeight = Theme.RowHeight,
				Clicked = () =>
				{
					choice.Select?.Invoke();
					RebuildDetails();
				}
			}, 1 );
			StyleStateButton( button, choice.Selected );
		}
	}

	void AddInlineCheckbox( Layout parent, string text, bool value, Action<bool> setter )
	{
		var checkbox = parent.Add( new Checkbox( text )
		{
			Value = value,
			MaximumHeight = Theme.RowHeight,
			ToolTip = text
		} );
		checkbox.SetStyles( $"color: {Theme.Text.Hex}; padding-right: 4px;" );
		checkbox.Toggled += () =>
		{
			setter?.Invoke( checkbox.Value );
			StateHasChanged();
		};
	}

	Widget VerticalDivider()
	{
		var divider = new Widget( null )
		{
			FixedWidth = 1,
			MinimumHeight = Theme.RowHeight - 4,
			MaximumHeight = Theme.RowHeight - 4
		};
		divider.OnPaintOverride += () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.TextControl.WithAlpha( 0.18f ) );
			Paint.DrawRect( divider.LocalRect, 1 );
			return true;
		};
		return divider;
	}

	void AddGroupRow( Layout parent, ObjectPlacerEntry entry )
	{
		var row = AddLabeledRow( parent, "Group", spacing: 6 );
		var so = entry.GetSerialized();
		so.OnPropertyChanged += changed =>
		{
			if ( changed?.Name != nameof( ObjectPlacerEntry.GroupId ) )
				return;

			RebuildMaster();
			StateHasChanged();
		};
		row.Add( new ObjectPlacerGroupDropdownWidget( so.GetProperty( nameof( ObjectPlacerEntry.GroupId ) ), _settings.Groups ), 1 ).FixedHeight = Theme.RowHeight;
	}

	void AddPropertyDropdownRow( Layout parent, string label, SerializedProperty property )
	{
		var row = AddLabeledRow( parent, label, spacing: 8 );
		AddPropertyDropdown( row, property );
	}

	void AddPropertyDropdown( Layout parent, SerializedProperty property, float minWidth = 0, float maxWidth = 0 )
	{
		var widget = ControlWidget.Create( property );
		widget.MinimumWidth = minWidth;
		widget.MaximumHeight = Theme.RowHeight;
		widget.HorizontalSizeMode = SizeMode.CanShrink | SizeMode.CanGrow;
		if ( maxWidth > 0 )
			widget.MaximumWidth = maxWidth;

		parent.Add( widget, 1 ).FixedHeight = Theme.RowHeight;
	}

	void AddPrefabRow( Layout parent, ObjectPlacerEntry entry )
	{
		var so = entry.GetSerialized();
		so.OnPropertyChanged += _ => StateHasChanged();

		var row = AddLabeledRow( parent, "Prefab", spacing: 8 );

		var widget = ControlWidget.Create( so.GetProperty( nameof( ObjectPlacerEntry.Prefab ) ) );
		widget.MinimumWidth = 0;
		widget.HorizontalSizeMode = SizeMode.CanShrink | SizeMode.CanGrow;
		row.Add( widget, 1 ).FixedHeight = Theme.RowHeight;
	}

	void AddComponentSelectorRow( Layout parent, string label, IEnumerable<TypeDescription> types, ObjectPlacerEntry entry, TypeDescription currentType, Func<TypeDescription, bool> ownsCurrentType )
	{
		var row = AddLabeledRow( parent, label, spacing: 8 );
		var typeArray = types.ToArray();
		var ownsSelection = currentType is not null && ownsCurrentType( currentType );
		var button = row.Add( new Button( ownsSelection ? currentType.Title : "None", ownsSelection ? currentType.Icon : "arrow_drop_down" )
		{
			MinimumWidth = 0,
			MaximumHeight = Theme.RowHeight
		}, 1 );
		StylePickerButton( button );

		if ( typeArray.Length == 0 )
		{
			button.Enabled = false;
			return;
		}

		button.Clicked = () => OpenComponentMenu( button, typeArray, type =>
		{
			if ( type is null )
			{
				if ( !ownsSelection && currentType is not null )
					return;

				entry.ComponentTypeName = null;
			}
			else
			{
				entry.ComponentTypeName = GetTypeName( type );
			}

			entry.PropertyOverrides.Clear();
			RebuildMaster();
			RebuildDetails();
			StateHasChanged();
		}, includeNone: true );
	}

	void OpenComponentMenu( Widget source, IEnumerable<TypeDescription> types, Action<TypeDescription> selectType, bool includeNone )
	{
		var menu = new ContextMenu( source );

		if ( includeNone )
		{
			menu.AddOption( "None", "block", () => selectType?.Invoke( null ) );
			menu.AddSeparator();
		}

		foreach ( var group in types.GroupBy( x => string.IsNullOrWhiteSpace( x.Group ) ? "Uncategorized" : x.Group ).OrderBy( x => x.Key ) )
		{
			var groupMenu = menu.AddMenu( group.Key, "folder" );
			foreach ( var type in group.OrderBy( x => x.Title ) )
				groupMenu.AddOption( type.Title, type.Icon, () => selectType?.Invoke( type ) );
		}

		menu.OpenAt( source.ScreenRect.BottomLeft );
		menu.ConstrainToScreen();
	}

	Layout AddLabeledRow( Layout parent, string label, int spacing )
	{
		var row = parent.AddRow();
		row.Spacing = spacing;
		row.Add( new Label( label ) { MinimumWidth = DetailLabelWidth, Alignment = TextFlag.LeftCenter } );
		return row;
	}

	void ConfigureCompactCombo( ComboBox comboBox )
	{
		comboBox.FixedHeight = Theme.RowHeight;
		comboBox.MaximumHeight = Theme.RowHeight;
		comboBox.VerticalSizeMode = SizeMode.CanShrink;
	}

	static void StyleStateButton( Button button, bool active )
	{
		button.IsToggle = true;
		button.IsChecked = active;

		if ( active )
		{
			button.Tint = Theme.Blue.Darken( 0.25f );
			button.SetStyles( "font-weight: 800;" );
			return;
		}

		button.Tint = Theme.WidgetBackground.WithAlpha( 0.85f );
		button.SetStyles( $"color: {Theme.TextControl.WithAlpha( 0.7f ).Hex};" );
	}

	Button StyledButton( string text, string icon, Action clicked, bool primary = false )
	{
		var button = new Button( text, icon )
		{
			Clicked = clicked,
			FixedHeight = Theme.RowHeight
		};
		StyleActionButton( button, primary );
		return button;
	}

	static void StyleActionButton( Button button, bool primary = false )
	{
		button.Tint = primary ? Theme.Blue.Darken( 0.25f ) : Theme.WidgetBackground.WithAlpha( 0.9f );
		button.SetStyles( $"font-weight: 700; color: {Color.White.Hex};" );
	}

	static void StylePickerButton( Button button )
	{
		button.Tint = Theme.WidgetBackground;
		button.SetStyles( $"color: {Theme.Text.Hex}; font-weight: 600;" );
	}

	Widget CreateCard()
	{
		var card = new Widget( null );
		card.Layout = Layout.Column();
		card.Layout.Spacing = 3;
		card.Layout.Margin = new Margin( 8, 4, 8, 4 );
		card.OnPaintOverride += () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground.WithAlpha( 0.5f ) );
			Paint.DrawRect( card.LocalRect, Theme.ControlRadius );
			return true;
		};
		return card;
	}

	Widget EmptyLabel( string text )
	{
		return new Label( text )
		{
			Margin = 16,
			Color = Theme.TextControl.WithAlpha( 0.5f )
		};
	}

	Widget IconPreview( string icon )
	{
		var widget = new Widget( null )
		{
			FixedWidth = 32,
			FixedHeight = 32,
			TransparentForMouseEvents = true
		};
		widget.OnPaintOverride += () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( widget.LocalRect, 3 );

			var iconRect = widget.LocalRect.Shrink( 7 );
			if ( !ObjectPlacerPreviewIcons.DrawConfiguredIcon( iconRect, icon, 18 ) )
			{
				Paint.SetPen( Theme.Text.WithAlpha( 0.8f ) );
				Paint.DrawIcon( iconRect, string.IsNullOrWhiteSpace( icon ) ? "category" : icon, 18, TextFlag.Center );
			}

			return true;
		};
		return widget;
	}

	IconButton DeleteButton( string tooltip, Action action )
	{
		return new IconButton( "close" )
		{
			ToolTip = tooltip,
			IconSize = 14,
			Background = Color.Transparent,
			MouseLeftPress = action
		};
	}
}
