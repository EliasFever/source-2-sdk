namespace Editor;

using Sandbox.UI;
using System;
using System.Linq;

public partial class ObjectPlacerTool
{
	private ObjectPlacerBrowser _browser;
	private Button _classPickerButton;
	internal static int _selectedViewMode;
	internal static readonly Dictionary<string, bool> _categoryVisibility = [];

	private const int SidebarSpacing = 6;
	internal const int BrowserSpacing = 6;
	internal const int GroupSpacing = 2;
	internal static readonly Color BrowserBackground = Theme.TabBackground;
	internal static readonly Color HeaderBackground = Color.Black.WithAlpha( 0.65f );

	/// <summary>
	/// Create the sidebar widget for the entity placer tool
	/// </summary>
	public override Widget CreateToolSidebar()
	{
		var widget = new ToolSidebarWidget( null )
		{
			MinimumWidth = 260
		};
		widget.Layout.Spacing = SidebarSpacing;
		widget.AddTitle( "Create Object", "lightbulb" );

		SerializedProperty yawRot = this?.GetSerialized().GetProperty( nameof( YawRotation ) );
		SerializedProperty useNormal = this?.GetSerialized().GetProperty( nameof( UseSurfaceNormal ) );
		SerializedProperty distOff = this?.GetSerialized().GetProperty( nameof( DistanceOffset ) );
		SerializedProperty selectPlaced = this?.GetSerialized().GetProperty( nameof( SelectWhenPlaced ) );

		var placementSheet = new ControlSheet();
		placementSheet.SetMinimumColumnWidth( 0, 118 );
		placementSheet.AddRow( yawRot );
		placementSheet.AddRow( distOff );
		placementSheet.AddRow( selectPlaced );
		placementSheet.AddRow( useNormal );

		var placementGroup = widget.AddGroup( "Placement Settings", collapsible: true );
		placementGroup.Add( placementSheet );

		var browserGroup = ToolSidebarWidget.CreateGroupWidget( "Object Browser", SizeMode.Flexible );
		widget.Layout.Add( browserGroup, 1 );

		var browserPanel = browserGroup.ContentLayout.Add( new Widget( null ) { Layout = Layout.Column() }, 1 );
		browserPanel.Layout.Spacing = BrowserSpacing;
		browserPanel.OnPaintOverride += () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( BrowserBackground );
			Paint.DrawRect( browserPanel.LocalRect, 0 );
			return true;
		};

		var browserToolbar = browserPanel.Layout.AddRow();
		browserToolbar.Spacing = 6;
		browserToolbar.Margin = new Margin( 0, 0, 0, 0 );

		var componentTypes = GetSelectableComponentTypes().ToArray();
		if ( componentTypes.Length > 0 )
		{
			browserToolbar.Add( new Label( "Class:" )
			{
				MinimumWidth = 48,
				MaximumWidth = 48,
				Alignment = TextFlag.LeftCenter
			} );

			var selectedType = GetDisplayedPickerType();
			var picker = browserToolbar.Add( new Button( selectedType?.Title ?? "None", selectedType?.Icon ?? "lightbulb" )
			{
				MinimumWidth = 0,
				MaximumHeight = Theme.RowHeight
			}, 1 );
			_classPickerButton = picker;
			StylePickerButton( picker );
			picker.Clicked = () => OpenClassPickerMenu( picker, componentTypes );
		}

		browserToolbar.AddSpacingCell( 8 );

		var viewSlider = browserToolbar.Add( new ObjectPlacerPippedSlider( null )
		{
			Minimum = 0,
			Maximum = 2,
			Step = 1,
			Value = _selectedViewMode,
			StepIcons = ["format_list_bulleted", "grid_on", "grid_view"],
			MinimumWidth = 68,
			MaximumWidth = 86
		} );
		viewSlider.OnValueEdited += () =>
		{
			var newValue = Math.Clamp( (int)MathF.Round( viewSlider.Value ), 0, 2 );
			if ( _selectedViewMode == newValue )
				return;

			_selectedViewMode = newValue;
			viewSlider.Value = newValue;
			PersistPlacementSettings();
			_browser?.Rebuild();
		};

		_browser = browserPanel.Layout.Add( new ObjectPlacerBrowser( this ) { VerticalSizeMode = SizeMode.CanGrow }, 1 );
		_browser.Rebuild();

		return widget;
	}

	private void OpenClassPickerMenu( Button source, IEnumerable<TypeDescription> types )
	{
		var typeArray = types.ToArray();
		if ( typeArray.Length == 0 )
			return;

		var menu = new ContextMenu( source );
		menu.AddOption( "None", "lightbulb", () => SetSelectedType( source, null ) );
		menu.AddSeparator();

		foreach ( var group in typeArray.GroupBy( x => string.IsNullOrWhiteSpace( x.Group ) ? "Uncategorized" : x.Group ).OrderBy( x => x.Key ) )
		{
			var groupMenu = menu.AddMenu( group.Key, "folder" );
			foreach ( var type in group.OrderBy( x => x.Title ) )
				groupMenu.AddOption( type.Title, type.Icon, () => SetSelectedType( source, type ) );
		}

		menu.OpenAt( source.ScreenRect.BottomLeft );
		menu.ConstrainToScreen();
	}

	private void SetSelectedType( Button source, TypeDescription type )
	{
		SelectedType = type?.TargetType;
		UpdateClassPickerButton();
	}

	private TypeDescription GetDisplayedPickerType()
	{
		if ( _selectedEntry is not null && _selectedEntry.Kind == ObjectPlacerEntryKind.Component )
			return FindComponentType( _selectedEntry.ComponentTypeName );

		return SelectedType is null ? null : EditorTypeLibrary.GetType( SelectedType );
	}

	private void UpdateClassPickerButton()
	{
		if ( !_classPickerButton.IsValid() )
			return;

		var displayedType = GetDisplayedPickerType();
		_classPickerButton.Text = displayedType?.Title ?? "None";
		_classPickerButton.Icon = displayedType?.Icon ?? "lightbulb";
		_classPickerButton.Update();
	}

	private static void StylePickerButton( Button button )
	{
		button.Tint = Theme.WidgetBackground;
		button.SetStyles( $"color: {Theme.Text.Hex}; font-weight: 600;" );
	}

	private bool IsVisibleEntry( ObjectPlacerEntry entry )
	{
		if ( entry is null )
			return false;

		if ( entry.HideInTool )
			return false;

		if ( !IsValidEntry( entry ) )
			return false;

		var groups = _settings?.Groups ?? [];
		if ( string.IsNullOrWhiteSpace( entry.GroupId ) )
			return _settings?.HideUngroupedEntries != true;

		var group = groups.FirstOrDefault( x => x.Id == entry.GroupId );
		if ( group is null )
			return _settings?.HideEntriesWithUnknownGroups != true;

		return !group.HideInTool;
	}

	private bool IsValidEntry( ObjectPlacerEntry entry )
	{
		return entry.Kind switch
		{
			ObjectPlacerEntryKind.Prefab => entry.Prefab.IsValid(),
			_ => !string.IsNullOrWhiteSpace( entry.ComponentTypeName )
		};
	}

	internal ObjectPlacerEntryGroupInfo[] GetVisibleEntryGroups()
	{
		var entries = _settings?.Entries?.Where( IsVisibleEntry ).ToArray() ?? [];
		return GetEntryGroups( entries ).ToArray();
	}

	internal bool IsSelectedEntry( ObjectPlacerEntry entry )
	{
		return entry is not null && ReferenceEquals( _selectedEntry, entry );
	}

	internal object GetSelectedEntryForList()
	{
		return _selectedEntry;
	}

	private IEnumerable<ObjectPlacerEntryGroupInfo> GetEntryGroups( ObjectPlacerEntry[] entries )
	{
		var groups = _settings?.Groups ?? [];

		foreach ( var group in groups.Where( x => !x.HideInTool ) )
		{
			var groupedEntries = entries.Where( x => x.GroupId == group.Id ).ToArray();
			if ( groupedEntries.Length == 0 )
				continue;

			yield return new ObjectPlacerEntryGroupInfo( group.Id,
				string.IsNullOrWhiteSpace( group.Name ) ? "Objects" : group.Name,
				string.IsNullOrWhiteSpace( group.Icon ) ? "folder" : group.Icon,
				group.CollapsedByDefault,
				groupedEntries );
		}

		var ungrouped = entries.Where( x => string.IsNullOrWhiteSpace( x.GroupId ) || !groups.Any( g => g.Id == x.GroupId ) ).ToArray();
		if ( ungrouped.Length > 0 )
			yield return new ObjectPlacerEntryGroupInfo( "ungrouped", "Objects", "folder", false, ungrouped );
	}

	private IEnumerable<TypeDescription> GetSelectableComponentTypes()
	{
		var baseType = FindComponentType( _settings?.BaseComponentTypeName );
		var componentTypes = EditorTypeLibrary.GetTypes<Component>()
			.Where( t => !t.IsAbstract )
			.Where( t => IsBaseComponent( t ) || baseType is null || baseType.TargetType.IsAssignableFrom( t.TargetType ) )
			.GroupBy( GetTypeName )
			.Select( g => g.First() )
			.OrderBy( t => t.Group )
			.ThenBy( t => t.Title );

		foreach ( var type in componentTypes )
			yield return type;
	}

	private static bool IsBaseComponent( TypeDescription type )
	{
		var fullName = type?.TargetType?.FullName ?? type?.FullName ?? "";
		return fullName.StartsWith( "Sandbox.", StringComparison.Ordinal );
	}

	private static string GetTypeName( TypeDescription type )
	{
		return type?.TargetType?.FullName
			?? type?.FullName
			?? type?.Name
			?? type?.ClassName;
	}

	internal static bool GetCategoryVisibility( string id, bool defaultValue )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return defaultValue;

		return _categoryVisibility.TryGetValue( id, out var visible ) ? visible : defaultValue;
	}

	internal static void SetCategoryVisibility( string id, bool visible )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return;

		_categoryVisibility[id] = visible;
	}


}

internal readonly record struct ObjectPlacerEntryGroupInfo( string Id, string Name, string Icon, bool CollapsedByDefault, ObjectPlacerEntry[] Entries );

file sealed class ObjectPlacerPippedSlider : FloatSlider
{
	public int Steps => Math.Max( 1, (int)(Maximum - Minimum) + 1 );
	public string[] StepIcons { get; set; }

	private static readonly Color GrooveBase = Color.Parse( "#1C1C1C" )!.Value;
	private static readonly Color GrooveHighlight = Color.Parse( "#4C4C4C" )!.Value;
	private static readonly Color GrooveShadow = Color.Parse( "#0D0D0D" )!.Value;
	private static readonly Color HandleColor = Color.Parse( "#616161ff" )!.Value;
	private static readonly Color PipColor = Color.Gray.WithAlpha( 0.75f );

	public ObjectPlacerPippedSlider( Widget parent ) : base( parent )
	{
		MinimumHeight = 60;
		MaximumHeight = 62;
		MinimumWidth = 60;
		Step = 1;
	}

	protected override void OnPaint()
	{
		var rect = LocalRect;
		var centerY = rect.Center.y;
		var grooveLeft = rect.Left + 5;
		var grooveWidth = rect.Width - 16;
		var grooveHeight = 3f;
		var grooveY = centerY - 10f;
		var grooveRect = new Rect( grooveLeft, grooveY - grooveHeight / 2, grooveWidth, grooveHeight );

		Paint.ClearPen();
		Paint.SetBrush( GrooveBase );
		Paint.DrawRect( grooveRect );
		Paint.SetPen( GrooveHighlight, 2 );
		Paint.DrawLine( grooveRect.BottomLeft, grooveRect.BottomRight );
		Paint.SetPen( GrooveShadow, 1 );
		Paint.DrawLine( grooveRect.TopLeft, grooveRect.TopRight );

		if ( Steps > 1 )
		{
			var spacing = grooveRect.Width / (Steps - 1);
			var pipBaseY = grooveRect.Bottom + 12f;
			for ( var i = 0; i < Steps; i++ )
			{
				var x = grooveRect.Left + i * spacing;
				var pipRect = new Rect( x - 0.5f, pipBaseY - 2.5f, 1, 5 );
				Paint.SetBrushAndPen( PipColor, PipColor );
				Paint.DrawRect( pipRect );

				if ( StepIcons is not null && i < StepIcons.Length )
				{
					var iconSize = 15f;
					var iconRect = new Rect( x - iconSize / 2, pipRect.Bottom + 7f, iconSize, iconSize );
					Paint.DrawIcon( iconRect, StepIcons[i], iconSize, TextFlag.Center );
				}
			}
		}

		var t = Maximum.AlmostEqual( Minimum ) ? 0f : (Value - Minimum) / (Maximum - Minimum);
		var handleX = grooveLeft + grooveWidth * t;
		var handleTop = centerY - 17f;
		var handleBottom = handleTop + 20f;
		var halfWidth = 5f;
		Vector2[] handlePolygon =
		[
			new( handleX - halfWidth, handleTop ),
			new( handleX + halfWidth, handleTop ),
			new( handleX + halfWidth, handleBottom - 6 ),
			new( handleX, handleBottom ),
			new( handleX - halfWidth, handleBottom - 5.5f )
		];

		Paint.ClearPen();
		Paint.SetBrush( HandleColor );
		Paint.DrawPolygon( handlePolygon );
		Paint.SetPen( HandleColor.Lighten( 0.25f ), 1 );
		Paint.DrawLine( handlePolygon[0], handlePolygon[1] );
		Paint.DrawLine( handlePolygon[0], handlePolygon[4] );
		Paint.SetPen( HandleColor.Darken( 0.65f ), 1 );
		Paint.DrawLine( handlePolygon[1], handlePolygon[2] );
		Paint.DrawLine( handlePolygon[2], handlePolygon[3] );
		Paint.DrawLine( handlePolygon[3], handlePolygon[4] );
	}
}

internal sealed class ObjectPlacerBrowser : Widget
{
	private readonly ObjectPlacerTool _tool;

	public ObjectPlacerBrowser( ObjectPlacerTool tool ) : base( null )
	{
		_tool = tool;
		Layout = Layout.Column();
		Layout.Spacing = ObjectPlacerTool.BrowserSpacing;
		VerticalSizeMode = SizeMode.CanGrow;
	}

	public void Rebuild()
	{
		Layout.Clear( true );

		var groups = _tool.GetVisibleEntryGroups();
		if ( groups.Length == 0 )
		{
			Layout.Add( new Label( "No Object Placer entries configured for this project." )
			{
				Margin = 8,
				Color = Theme.TextControl.WithAlpha( 0.5f )
			} );
			return;
		}

		BuildFlatView( groups );
	}

	private void BuildFlatView( ObjectPlacerEntryGroupInfo[] groups )
	{
		var scroll = Layout.Add( new ScrollArea( null )
		{
			Canvas = new Widget( null ) { Layout = Layout.Column() }
		}, 1 );
		scroll.Canvas.Layout.Margin = new Margin( 0, 0, 14, 0 );
		scroll.Canvas.Layout.Spacing = ObjectPlacerTool.BrowserSpacing;

		foreach ( var group in groups )
		{
			var visible = ObjectPlacerTool.GetCategoryVisibility( group.Id, !group.CollapsedByDefault );
			Widget buttonsContainer = null;

			AddHeader( scroll.Canvas.Layout, group.Name, group.Icon, () =>
			{
				visible = !visible;
				ObjectPlacerTool.SetCategoryVisibility( group.Id, visible );
				if ( buttonsContainer.IsValid() )
					buttonsContainer.Visible = visible;
			}, visible );

			buttonsContainer = scroll.Canvas.Layout.Add( new Widget( null ) { Layout = Layout.Column() } );
			buttonsContainer.Layout.Spacing = ObjectPlacerTool.GroupSpacing;
			buttonsContainer.Visible = visible;

			var entryList = buttonsContainer.Layout.Add( new ObjectPlacerEntryList( _tool, group.Icon, ObjectPlacerTool._selectedViewMode ) );
			entryList.SetItems( group.Entries.Cast<object>() );
		}

		scroll.Canvas.Layout.AddStretchCell();
	}

	private static void AddHeader( Layout parent, string title, string icon, Action toggle = null, bool expanded = true )
	{
		var row = parent.AddRow();
		row.Margin = new Margin( 0, 1, 0, 0 );

		var leftSpacer = row.Add( new Widget( null ) { MinimumWidth = toggle is null ? 0 : 20, MaximumWidth = toggle is null ? 0 : 20 } );
		leftSpacer.OnPaintOverride += () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( ObjectPlacerTool.HeaderBackground );
			Paint.DrawRect( leftSpacer.LocalRect, 0 );
			return true;
		};

		var label = row.Add( new Label.Subtitle( title )
		{
			Alignment = TextFlag.Center,
			Color = Theme.Blue,
			MinimumHeight = 25,
			MaximumHeight = 25,
			ToolTip = title
		}, 1 );
		label.OnPaintOverride += () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( ObjectPlacerTool.HeaderBackground );
			Paint.DrawRect( label.LocalRect, 0 );
			return false;
		};

		if ( toggle is null )
			return;

		Button button = null;
		button = row.Add( new Button( expanded ? "-" : "+" )
		{
			MinimumWidth = 20,
			MaximumWidth = 20,
			MinimumHeight = 20,
			MaximumHeight = 20,
			Clicked = () =>
			{
				toggle();
				button.Text = button.Text == "-" ? "+" : "-";
			}
		} );
	}
}

file sealed class ObjectPlacerEntryList : ListView
{
	private readonly ObjectPlacerTool _tool;
	private readonly string _fallbackIcon;
	private readonly int _viewMode;
	private static readonly Dictionary<string, string> EditorVisualPathCache = [];
	private static readonly Dictionary<string, Texture> EditorVisualTextureCache = [];
	private static readonly Dictionary<string, Pixmap> EditorVisualPixmapCache = [];
	private static Pixmap ThumbnailBackgroundPixmap;

	public ObjectPlacerEntryList( ObjectPlacerTool tool, string fallbackIcon, int viewMode ) : base( null )
	{
		_tool = tool;
		_fallbackIcon = fallbackIcon;
		_viewMode = viewMode;
		MultiSelect = false;
		ItemSpacing = viewMode == 0 ? new Vector2( 1, 2 ) : new Vector2( 0, 1 );
		ItemSize = viewMode switch
		{
			1 => new Vector2( 88, 112 ),
			2 => new Vector2( 120, 150 ),
			_ => new Vector2( 0, 24 )
		};
		Margin = 0;
		ItemAlign = Align.FlexStart;
		HorizontalScrollbarMode = ScrollbarMode.Off;
		VerticalScrollbarMode = ScrollbarMode.Off;
		HorizontalSizeMode = SizeMode.CanShrink | SizeMode.CanGrow;
		VerticalSizeMode = SizeMode.CanShrink;
		ItemSelected = OnItemSelected;
		ItemScrollEnter = OnItemScrollEnter;
		ItemScrollExit = OnItemScrollExit;
		SelectionOverride = _tool.GetSelectedEntryForList;
	}

	protected override void DoLayout()
	{
		base.DoLayout();
		BuildLayout();
	}

	private void BuildLayout()
	{
		var rect = CanvasRect;
		var width = Math.Max( 1, rect.Width );
		var itemWidth = _viewMode == 0 ? width : ItemSize.x;
		var itemsPerRow = _viewMode == 0 ? 1 : Math.Max( 1, ((width + ItemSpacing.x) / (itemWidth + ItemSpacing.x)).FloorToInt() );
		var rowCount = MathX.CeilToInt( Items.Count() / (float)itemsPerRow );
		FixedHeight = rowCount * ItemSize.y + Math.Max( 0, rowCount - 1 ) * ItemSpacing.y + Margin.EdgeSize.y;
	}

	protected override void OnMouseWheel( WheelEvent e )
	{
		// The containing ScrollArea owns vertical wheel scrolling in the flat browser.
	}

	private void OnItemSelected( object value )
	{
		if ( value is ObjectPlacerEntry entry )
		{
			_tool.SelectEntry( entry );
			SelectItem( entry, false, true );
			Update();
		}
	}

	private void OnItemScrollEnter( object obj )
	{
		if ( obj is ObjectPlacerEntry entry )
			GetPreviewAsset( entry )?.GetAssetThumb( true );
	}

	private void OnItemScrollExit( object obj )
	{
		if ( obj is ObjectPlacerEntry entry )
			GetPreviewAsset( entry )?.CancelThumbBuild();
	}

	protected override string GetTooltip( object obj )
	{
		if ( obj is not ObjectPlacerEntry entry )
			return string.Empty;

		return entry.Kind == ObjectPlacerEntryKind.Prefab
			? CombineTooltip( entry.Description, entry.Prefab?.ResourcePath )
			: entry.PropertyOverrides is null || entry.PropertyOverrides.Count == 0
				? CombineTooltip( entry.Description, entry.ComponentTypeName )
				: CombineTooltip( entry.Description, $"{entry.ComponentTypeName}\n{string.Join( "\n", entry.PropertyOverrides.Select( x => $"{x.PropertyName}: {x.ResourcePath ?? x.Value}" ) )}" );
	}

	private static string CombineTooltip( string description, string details )
	{
		if ( string.IsNullOrWhiteSpace( description ) )
			return details ?? string.Empty;

		if ( string.IsNullOrWhiteSpace( details ) )
			return description;

		return $"{description}\n{details}";
	}

	protected override void PaintItem( VirtualWidget item )
	{
		if ( item.Object is not ObjectPlacerEntry entry )
			return;

		if ( _viewMode == 0 )
		{
			PaintListItem( item, entry );
			return;
		}

		PaintThumbnailItem( item, entry );
	}

	private void PaintListItem( VirtualWidget item, ObjectPlacerEntry entry )
	{
		var selected = _tool.IsSelectedEntry( entry );
		var rect = item.Rect.Shrink( 0, 1 );
		var c = (selected ? Theme.Primary : Color.Parse( "#48494c" )!.Value).ToHsv();
		var bg = c;

		if ( item.Hovered )
			bg = c with { Value = c.Value + 0.2f };

		const float radius = 3f;
		Paint.Antialiasing = true;
		Paint.ClearPen();
		Paint.SetBrush( bg with { Value = bg.Value + 0.04f, Saturation = c.Saturation * 0.8f } );
		Paint.DrawRect( rect, radius );
		Paint.SetBrushLinear( rect.TopLeft, rect.BottomRight, bg, bg with { Value = bg.Value - 0.03f } );
		Paint.DrawRect( rect.Shrink( 1, 1, 1, 1 ), radius );

		Paint.SetPen( c with { Value = 0.99f, Saturation = c.Saturation * 0.20f } );
		Paint.SetDefaultFont();
		Paint.DrawText( rect.Shrink( 8, 0 ), string.IsNullOrWhiteSpace( entry.Name ) ? entry.ComponentTypeName : entry.Name, TextFlag.Center | TextFlag.SingleLine );
	}

	private void PaintThumbnailItem( VirtualWidget item, ObjectPlacerEntry entry )
	{
		var selected = _tool.IsSelectedEntry( entry );
		var rect = item.Rect.Shrink( 1 );

		if ( selected || Paint.HasMouseOver )
		{
			Paint.ClearPen();
			Paint.SetBrush( selected ? Theme.Primary.WithAlpha( 0.28f ) : Color.White.WithAlpha( 0.035f ) );
			Paint.DrawRect( rect, 4 );
		}

		var labelBand = _viewMode == 1 ? 26f : 30f;
		var thumbRect = rect.Shrink( 3, 5, 3, labelBand );
		thumbRect.Height = Math.Min( thumbRect.Height, thumbRect.Width );
		thumbRect.Left = rect.Center.x - thumbRect.Width * 0.5f;

		Paint.ClearPen();
		Paint.Draw( thumbRect, GetThumbnailBackgroundPixmap() );

		var customIcon = ObjectPlacerPreviewIcons.GetConfiguredIconPixmap( entry.Icon );
		var asset = customIcon is null ? GetPreviewAsset( entry ) : null;
		var pixmap = customIcon ?? asset?.GetAssetThumb( true );
		if ( pixmap is not null )
		{
			Paint.BilinearFiltering = true;
			Paint.Draw( thumbRect.Shrink( customIcon is null ? 2 : 8 ), pixmap );
			Paint.BilinearFiltering = false;
		}
		else
		{
			var editorVisual = GetEditorVisualPath( entry );
			var fallbackPixmap = GetEditorVisualPixmap( editorVisual );
			if ( fallbackPixmap is not null )
			{
				Paint.BilinearFiltering = true;
				Paint.Draw( thumbRect.Shrink( 8 ), fallbackPixmap );
				Paint.BilinearFiltering = false;
			}
			else
			{
				var fallbackIcon = string.IsNullOrWhiteSpace( entry.Icon )
					? entry.Kind == ObjectPlacerEntryKind.Prefab ? "dataset" : _fallbackIcon ?? "extension"
					: entry.Icon;
				var fallbackCustomIcon = ObjectPlacerPreviewIcons.GetConfiguredIconPixmap( fallbackIcon );
				if ( fallbackCustomIcon is not null )
				{
					Paint.BilinearFiltering = true;
					Paint.Draw( thumbRect.Shrink( 8 ), fallbackCustomIcon );
					Paint.BilinearFiltering = false;
				}
				else
				{
					Paint.SetPen( Theme.Text.WithAlpha( 0.45f ) );
					Paint.DrawIcon( thumbRect.Shrink( 18 ), fallbackIcon, 36, TextFlag.Center );
				}
			}
		}

		Paint.ClearBrush();
		Paint.SetPen( Color.Black.WithAlpha( 0.25f ) );
		Paint.DrawRect( thumbRect, 0 );

		var textRect = new Rect( rect.Left + 6, thumbRect.Bottom + 6, rect.Width - 12, 14 );
		Paint.SetDefaultFont( 7 );
		Paint.SetPen( selected ? Theme.TextButton : Theme.Text.WithAlpha( 0.8f ) );
		var name = string.IsNullOrWhiteSpace( entry.Name ) ? entry.ComponentTypeName : entry.Name;
		name = Paint.GetElidedText( name ?? "Unnamed", textRect.Width, ElideMode.Middle );
		Paint.DrawText( textRect, name, TextFlag.Center | TextFlag.SingleLine );
	}

	private static Asset GetPreviewAsset( ObjectPlacerEntry entry )
	{
		if ( entry is null )
			return null;

		if ( entry.Kind == ObjectPlacerEntryKind.Prefab && entry.Prefab.IsValid() )
			return AssetSystem.FindByPath( entry.Prefab.ResourcePath );

		var resourcePath = entry.PropertyOverrides?
			.Select( x => x.ResourcePath ?? (x.Kind == ObjectPlacerPropertyOverrideKind.Resource ? x.Value : null) )
			.FirstOrDefault( x => !string.IsNullOrWhiteSpace( x ) );

		if ( !string.IsNullOrWhiteSpace( resourcePath ) )
			return AssetSystem.FindByPath( resourcePath );

		var editorVisual = GetEditorVisualPath( entry );
		return string.IsNullOrWhiteSpace( editorVisual ) ? null : AssetSystem.FindByPath( editorVisual );
	}

	private static Pixmap GetThumbnailBackgroundPixmap()
	{
		if ( ThumbnailBackgroundPixmap is not null )
			return ThumbnailBackgroundPixmap;

		using var bitmap = new Bitmap( 64, 64, false );
		bitmap.SetLinearGradient( new Vector2( 0, 0 ), new Vector2( 0, 64 ), Gradient.FromColors( Color.Parse( "#303030" )!.Value, Color.Parse( "#515151" )!.Value ) );
		bitmap.DrawRect( bitmap.Rect );
		ThumbnailBackgroundPixmap = Pixmap.FromBitmap( bitmap );
		return ThumbnailBackgroundPixmap;
	}

	private static string GetEditorVisualPath( ObjectPlacerEntry entry )
	{
		if ( entry is null || entry.Kind != ObjectPlacerEntryKind.Component || string.IsNullOrWhiteSpace( entry.ComponentTypeName ) )
			return null;

		if ( EditorVisualPathCache.TryGetValue( entry.ComponentTypeName, out var cachedPath ) )
			return cachedPath;

		var type = EditorTypeLibrary.GetTypes<Component>().FirstOrDefault( x =>
			string.Equals( x.FullName, entry.ComponentTypeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( x.Name, entry.ComponentTypeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( x.ClassName, entry.ComponentTypeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( x.TargetType?.FullName, entry.ComponentTypeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( x.TargetType?.Name, entry.ComponentTypeName, StringComparison.OrdinalIgnoreCase ) );
		var componentType = type?.TargetType;
		if ( componentType is null )
		{
			EditorVisualPathCache[entry.ComponentTypeName] = null;
			return null;
		}

		var explicitPath = GetKnownComponentPreviewPath( componentType );
		if ( !string.IsNullOrWhiteSpace( explicitPath ) )
		{
			EditorVisualPathCache[entry.ComponentTypeName] = explicitPath;
			return explicitPath;
		}

		var property = componentType.GetProperty( "EditorVis", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic );
		if ( property is not null && property.PropertyType == typeof( string ) )
		{
			try
			{
				var scene = Scene.CreateEditorScene();
				try
				{
					using ( scene.Push() )
					{
						var gameObject = new GameObject( false, "preview" );
						var component = gameObject.Components.Create( type );
						var path = property.GetValue( component ) as string;
						if ( !string.IsNullOrWhiteSpace( path ) )
						{
							EditorVisualPathCache[entry.ComponentTypeName] = path;
							return path;
						}
					}
				}
				finally
				{
					scene.Destroy();
				}
			}
			catch
			{
				// Fall through to attribute/default fallbacks.
			}
		}

		var handleAttribute = componentType.GetCustomAttributes( typeof( EditorHandleAttribute ), true ).OfType<EditorHandleAttribute>().FirstOrDefault();
		if ( !string.IsNullOrWhiteSpace( handleAttribute?.Texture ) )
		{
			EditorVisualPathCache[entry.ComponentTypeName] = handleAttribute.Texture;
			return handleAttribute.Texture;
		}

		EditorVisualPathCache[entry.ComponentTypeName] = null;
		return null;
	}

	private static string GetKnownComponentPreviewPath( Type componentType )
	{
		if ( componentType is null )
			return null;

		return componentType.Name switch
		{
			"KelvinDirectionalLight" => "models/editor/sun.vmdl",
			"KelvinPointLight" => "models/editor/omni.vmdl",
			"KelvinSpotLight" => "models/editor/spot.vmdl",
			_ when typeof( EnvmapProbe ).IsAssignableFrom( componentType ) => "model:sphere",
			_ => null
		};
	}

	private static Pixmap GetEditorVisualPixmap( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
			return null;

		if ( EditorVisualPixmapCache.TryGetValue( path, out var cachedPixmap ) )
			return cachedPixmap;

		Pixmap pixmap = null;

		if ( path.EndsWith( ".vmdl", StringComparison.OrdinalIgnoreCase ) || path == "model:sphere" )
		{
			pixmap = RenderModelPreview( path );
			EditorVisualPixmapCache[path] = pixmap;
			return pixmap;
		}

		if ( EditorVisualTextureCache.TryGetValue( path, out var cachedTexture ) )
			return cachedTexture is null ? null : Pixmap.FromTexture( cachedTexture );

		if ( !FileSystem.Mounted.FileExists( path ) && !FileSystem.Mounted.FileExists( path + "_c" ) )
		{
			EditorVisualPixmapCache[path] = null;
			return null;
		}

		try
		{
			var texture = Texture.Load( path );
			EditorVisualTextureCache[path] = texture;
			pixmap = texture is null ? null : Pixmap.FromTexture( texture );
			EditorVisualPixmapCache[path] = pixmap;
			return pixmap;
		}
		catch
		{
			EditorVisualTextureCache[path] = null;
			EditorVisualPixmapCache[path] = null;
			return null;
		}
	}

	private static Pixmap RenderModelPreview( string path )
	{
		try
		{
			var model = path == "model:sphere" ? Model.Sphere : Model.Load( path );
			if ( model is null )
				return null;

			var scene = Scene.CreateEditorScene();
			try
			{
				using ( scene.Push() )
				{
					var cameraObject = new GameObject( true, "camera" );
					var camera = cameraObject.AddComponent<CameraComponent>();
					camera.BackgroundColor = Color.Transparent;
					camera.WorldRotation = new Angles( 20, 225, 0 );
					camera.FieldOfView = 30;
					camera.ZFar = 100000;
					camera.ZNear = 0.1f;

					var sunObject = new GameObject( true, "sun" );
					var sun = sunObject.AddComponent<DirectionalLight>();
					sun.WorldRotation = new Angles( 45, 45, 0 );
					sun.LightColor = Color.White * 1.5f;

					var ambientObject = new GameObject( true, "ambient" );
					var ambient = ambientObject.AddComponent<AmbientLight>();
					ambient.Color = Color.White * 0.25f;

					var modelObject = new GameObject( true, "model" );
					var renderer = modelObject.AddComponent<ModelRenderer>();
					renderer.Model = model;
					renderer.MaterialOverride = path == "model:sphere" ? Material.Load( "materials/dev/dev_metal_rough00.vmat" ) : null;

					var bounds = model.RenderBounds;
					var size = Math.Max( 1f, bounds.Size.Length );
					var distance = MathX.SphereCameraDistance( size * 0.5f, camera.FieldOfView );
					camera.WorldPosition = bounds.Center + camera.WorldRotation.Forward * -distance;

					var pixmap = new Pixmap( 256, 256 );
					camera.RenderToPixmap( pixmap );
					return pixmap;
				}
			}
			finally
			{
				scene.Destroy();
			}
		}
		catch
		{
			return null;
		}
	}
}

internal static class ObjectPlacerPreviewIcons
{
	private static readonly Dictionary<string, Pixmap> ConfiguredIconPixmapCache = [];

	public static bool DrawConfiguredIcon( Rect rect, string icon, float iconSize )
	{
		var pixmap = GetConfiguredIconPixmap( icon );
		if ( pixmap is null )
			return false;

		var size = Math.Min( rect.Width, rect.Height );
		var imageRect = new Rect( rect.Center.x - size * 0.5f, rect.Center.y - size * 0.5f, size, size ).Shrink( Math.Max( 0, (size - iconSize) * 0.5f ) );
		Paint.BilinearFiltering = true;
		Paint.Draw( imageRect, pixmap );
		Paint.BilinearFiltering = false;
		return true;
	}

	public static Pixmap GetConfiguredIconPixmap( string icon )
	{
		if ( string.IsNullOrWhiteSpace( icon ) || !LooksLikeAssetPath( icon ) )
			return null;

		if ( ConfiguredIconPixmapCache.TryGetValue( icon, out var cached ) )
			return cached;

		var pixmap = LoadIconPixmap( icon );
		ConfiguredIconPixmapCache[icon] = pixmap;
		return pixmap;
	}

	public static bool IsConfiguredIconPath( string icon )
	{
		return LooksLikeAssetPath( icon );
	}

	private static Pixmap LoadIconPixmap( string icon )
	{
		try
		{
			if ( IsTexturePath( icon ) )
			{
				var directTexture = Texture.Load( icon );
				if ( directTexture is not null )
					return Pixmap.FromTexture( directTexture );
			}

			var asset = AssetSystem.FindByPath( icon );
			var assetThumb = asset?.GetAssetThumb( true );
			if ( assetThumb is not null )
				return assetThumb;

			if ( IsTexturePath( icon ) && (FileSystem.Mounted.FileExists( icon ) || FileSystem.Mounted.FileExists( icon + "_c" )) )
			{
				var texture = Texture.Load( icon );
				return texture is null ? null : Pixmap.FromTexture( texture );
			}

			var fullPath = FileSystem.Content.GetFullPath( icon );
			if ( !string.IsNullOrWhiteSpace( fullPath ) && System.IO.File.Exists( fullPath ) )
				return Pixmap.FromFile( fullPath );
		}
		catch
		{
			return null;
		}

		return null;
	}

	private static bool LooksLikeAssetPath( string icon )
	{
		if ( string.IsNullOrWhiteSpace( icon ) )
			return false;

		return icon.Contains( '/' ) || icon.Contains( '\\' ) || IsTexturePath( icon );
	}

	private static bool IsTexturePath( string icon )
	{
		return icon.EndsWith( ".vtex", StringComparison.OrdinalIgnoreCase )
			|| icon.EndsWith( ".png", StringComparison.OrdinalIgnoreCase )
			|| icon.EndsWith( ".jpg", StringComparison.OrdinalIgnoreCase )
			|| icon.EndsWith( ".jpeg", StringComparison.OrdinalIgnoreCase )
			|| icon.EndsWith( ".tga", StringComparison.OrdinalIgnoreCase )
			|| icon.EndsWith( ".tif", StringComparison.OrdinalIgnoreCase )
			|| icon.EndsWith( ".tiff", StringComparison.OrdinalIgnoreCase );
	}
}
