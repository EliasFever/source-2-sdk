namespace Editor.Preferences;

internal partial class PageInterface : Widget
{
	private static readonly List<Action<Layout>> GroupBuilders = [];

	public static void RegisterGroup( Action<Layout> builder )
	{
		if ( builder is null ) return;
		if ( GroupBuilders.Contains( builder ) ) return;
		GroupBuilders.Add( builder );
	}

	private static void BuildGroups( Layout layout )
	{
		RegisterGroups();

		foreach ( var builder in GroupBuilders )
		{
			builder( layout );
		}
	}

	public PageInterface( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.Margin = 32;

		{
			var scrollbox = new ScrollArea( this );
			scrollbox.Canvas = new Widget( scrollbox )
			{
				Layout = Layout.Column(),
				VerticalSizeMode = SizeMode.Flexible,
				HorizontalSizeMode = SizeMode.Flexible,
				ContentMargins = new Sandbox.UI.Margin( 0, 0, 16, 0 )
			};

			Layout.Add( scrollbox, 1 );

			scrollbox.Canvas.Layout.Add( new Label.Subtitle( "User Interface" ) );
			scrollbox.Canvas.Layout.AddSpacingCell( 25 );
			scrollbox.Canvas.Layout.Add( new Label.Subtitle( "Experimental" ) );
			scrollbox.Canvas.Layout.AddSpacingCell( 8 );

			BuildGroups( scrollbox.Canvas.Layout );

			scrollbox.Canvas.Layout.AddSpacingCell( 25 );

			EditorEvent.Run( "editor.preferences.page.interface", this );

			scrollbox.Canvas.Layout.AddStretchCell();

			var warningbox = Layout.Add( new WarningBox( "<b>Source 2 SDK: These are experimental UI additions we are doing for the editor.</b>" +
			"<p>These can be highly unstable and most of the time are work-in-progress, so expect:</p> " +
			"<ul style=\"-qt-list-indent: 0; margin-left: 10px;\" >" +
			"<li>Unexpected behavior" +
			"<li>Unfinished styling" +
			"<li>Proof of concept additions that are yet to be polished" +
			"<li>Errors that can lead to console spam, crashes however are not expected so if you get those report them immediately!" +
			"</ul>" ) );
			
			warningbox.HorizontalSizeMode = SizeMode.Expand | SizeMode.CanGrow;
		}
	}

	internal static void AddControlSheetBoolRow( Layout layout, string label, Func<bool> getter, Action<bool> setter )
	{
		var row = new PreferenceRow
		{
			ContentMargins = new Sandbox.UI.Margin( 6, 0, 6, 0 )
		};

		var grid = Layout.Grid();
		grid.HorizontalSpacing = 8;
		row.Layout = grid;

		var text = new Label( label )
		{
			MinimumWidth = 140f,
			HorizontalSizeMode = SizeMode.Flexible,
			VerticalSizeMode = SizeMode.CanShrink,
			Color = Theme.TextControl.WithAlpha( 0.7f )
		};

		var checkbox = new Checkbox( "" )
		{
			FixedHeight = Theme.RowHeight,
			HorizontalSizeMode = SizeMode.CanShrink,
			VerticalSizeMode = SizeMode.CanGrow
		};
		checkbox.Bind( "Value" ).From( getter, setter );

		grid.AddCell( 0, 0, text, alignment: TextFlag.LeftCenter );
		grid.AddCell( 1, 0, checkbox, alignment: TextFlag.RightCenter );
		grid.SetColumnStretch( 0, 0 );
		grid.SetColumnStretch( 1, 1 );
		grid.SetMinimumColumnWidth( 0, 140 );

		layout.Add( row );
	}

	internal static void AddSegmentedRow(
		Layout layout,
		string label,
		IReadOnlyList<string> options,
		int selectedIndex,
		Action<int> onSelected,
		IReadOnlyList<string> icons = null )
	{
		var row = new PreferenceRow
		{
			ContentMargins = new Sandbox.UI.Margin( 6, 0, 6, 0 )
		};

		var grid = Layout.Grid();
		grid.HorizontalSpacing = 8;
		row.Layout = grid;

		var text = new Label( label )
		{
			MinimumWidth = 100f,
			HorizontalSizeMode = SizeMode.Flexible,
			VerticalSizeMode = SizeMode.CanShrink,
			Color = Theme.TextControl.WithAlpha( 0.7f )
		};

		var segmented = new SegmentedControl
		{
			HorizontalSizeMode = SizeMode.Flexible,
			MinimumWidth = 280f
		};
		for ( var i = 0; i < options.Count; i++ )
		{
			var icon = icons is not null && i < icons.Count ? icons[i] : null;
			segmented.AddOption( options[i], icon );
		}

		selectedIndex = selectedIndex.Clamp( 0, Math.Max( options.Count - 1, 0 ) );
		if ( options.Count > 0 )
		{
			segmented.Selected = options[selectedIndex];
		}

		segmented.OnSelectedChanged += value =>
		{
			for ( var i = 0; i < options.Count; i++ )
			{
				if ( options[i] == value )
				{
					onSelected?.Invoke( i );
					break;
				}
			}
		};

		grid.AddCell( 0, 0, text, alignment: TextFlag.LeftCenter );
		grid.AddCell( 1, 0, segmented, alignment: TextFlag.RightCenter );
		grid.SetColumnStretch( 0, 0 );
		grid.SetColumnStretch( 1, 1 );
		grid.SetMinimumColumnWidth( 0, 100 );

		layout.Add( row );
	}

	internal static void AddActionRow( Layout layout, string label, string icon, Action onClick )
	{
		var row = new PreferenceRow
		{
			ContentMargins = new Sandbox.UI.Margin( 6, 0, 6, 0 )
		};

		var grid = Layout.Grid();
		grid.HorizontalSpacing = 8;
		row.Layout = grid;

		var text = new Label( label )
		{
			MinimumWidth = 140f,
			HorizontalSizeMode = SizeMode.Flexible,
			VerticalSizeMode = SizeMode.CanShrink,
			Color = Theme.TextControl.WithAlpha( 0.7f )
		};

		var button = new Button( label, icon )
		{
			Clicked = onClick,
			VerticalSizeMode = SizeMode.CanGrow
		};

		grid.AddCell( 0, 0, text, alignment: TextFlag.LeftCenter );
		grid.AddCell( 1, 0, button, alignment: TextFlag.RightCenter );
		grid.SetColumnStretch( 0, 0 );
		grid.SetColumnStretch( 1, 1 );
		grid.SetMinimumColumnWidth( 0, 140 );

		layout.Add( row );
	}

	private class PreferenceRow : Widget
	{
		int _index = -1;

		public PreferenceRow()
		{
			MinimumHeight = Theme.RowHeight + 8;
			HorizontalSizeMode = SizeMode.Flexible;
			VerticalSizeMode = SizeMode.CanGrow;
		}

		protected override void OnPaint()
		{
			if ( _index == -1 )
				_index = Parent?.Children?.ToList()?.IndexOf( this ) ?? -1;

			if ( _index % 2 == 1 )
				Paint.SetBrushAndPen( Theme.WidgetBackground.Darken( 0.1f ) );
			else
				Paint.SetBrushAndPen( Theme.WidgetBackground );
			Paint.DrawRect( LocalRect );

			base.OnPaint();
		}
	}
}
