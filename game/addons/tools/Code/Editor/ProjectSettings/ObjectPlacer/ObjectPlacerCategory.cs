namespace Editor.ProjectSettingPages;

[Title( "Object Placer" ), Icon( "playlist_add" )]
internal sealed partial class ObjectPlacerCategory : ProjectSettingsWindow.Category
{
	ObjectPlacerConfig _settings;
	ObjectPlacerCatalogListView _masterList;
	Widget _detailCard;
	Layout _detailLayout;
	object _selectedItem;
	readonly List<GameObject> _previewObjects = [];
	static readonly HashSet<string> CollapsedGroups = [];
	static readonly HashSet<string> CollapsedEntries = [];

	public override void OnInit( Project project )
	{
		base.OnInit( project );

		_settings = ObjectPlacerConfig.Load();

		BodyLayout.Add( new InformationBox(
			"""
			<p>Define the objects that appear in Object Placer for this project.</p>
			<p>Groups define ordering and visibility. Entries can create a component with defaults, instantiate a prefab, or be hidden from the placement catalog.</p>
			""" ) );

		BodyLayout.AddSpacingCell( 10 );
		AddProjectOptionsRow();

		BodyLayout.AddSpacingCell( 12 );
		AddMasterDetailEditor();
	}

	void AddMasterDetailEditor()
	{
		_selectedItem ??= _settings.Entries.FirstOrDefault() as object ?? _settings.Groups.FirstOrDefault();

		var splitHost = BodyLayout.Add( new Widget( null )
		{
			MinimumHeight = 680,
			FixedHeight = 680,
			VerticalSizeMode = SizeMode.CanGrow
		} );
		splitHost.Layout = Layout.Row();

		var split = splitHost.Layout;
		split.Spacing = 10;

		var masterCard = CreateCard();
		masterCard.MinimumWidth = 340;
		masterCard.MaximumWidth = 430;
		masterCard.MinimumHeight = 660;
		masterCard.FixedHeight = 660;
		masterCard.VerticalSizeMode = SizeMode.CanGrow;
		split.Add( masterCard, 0 );

		var masterHeader = masterCard.Layout.AddRow();
		masterHeader.Spacing = 8;
		masterHeader.Add( new Label.Header( "Catalog" ), 1 );
		var addButton = StyledButton( "Add", "add", OpenAddMenu, primary: true );
		addButton.ToolTip = "Add group or entry";
		masterHeader.Add( addButton );

		_masterList = masterCard.Layout.Add( new ObjectPlacerCatalogListView( this, masterCard )
		{
			ItemSize = new Vector2( 0, 34 ),
			ItemSpacing = 1,
			Margin = 0,
			MultiSelect = false,
			AcceptDrops = true
		}, 1 );

		var detailCard = CreateCard();
		_detailCard = detailCard;
		detailCard.MinimumWidth = 0;
		detailCard.MinimumHeight = 660;
		detailCard.FixedHeight = 660;
		detailCard.VerticalSizeMode = SizeMode.CanGrow;
		split.Add( detailCard, 1 );
		_detailLayout = detailCard.Layout;
		_detailLayout.Spacing = 6;

		RebuildMaster();
		RebuildDetails();
	}

	void AddProjectOptionsRow()
	{
		var card = CreateCard();
		BodyLayout.Add( card );

		var row = card.Layout.AddRow();
		row.Spacing = 10;

		row.Add( new Label( "Catalog" )
		{
			MinimumWidth = 58,
			Alignment = TextFlag.LeftCenter,
			Color = Theme.TextControl.WithAlpha( 0.65f )
		} );
		AddInlineCheckbox( row, "Hide Ungrouped", _settings.HideUngroupedEntries, value => _settings.HideUngroupedEntries = value );
		AddInlineCheckbox( row, "Hide Unknown", _settings.HideEntriesWithUnknownGroups, value => _settings.HideEntriesWithUnknownGroups = value );
		row.Add( VerticalDivider() );
		AddInlineBaseClassPicker( row );
	}

	void AddInlineBaseClassPicker( Layout row )
	{
		row.Add( new Label( "Class Picker Base" ) { MinimumWidth = 114, Alignment = TextFlag.LeftCenter } );

		var selectedType = FindComponentType( _settings.BaseComponentTypeName );
		var selectButton = row.Add( new Button( selectedType?.Title ?? "None", selectedType?.Icon ?? "arrow_drop_down" ), 1 );
		selectButton.MinimumWidth = 0;
		selectButton.MaximumHeight = Theme.RowHeight;
		StylePickerButton( selectButton );

		var projectTypes = GetProjectComponentTypes().ToArray();
		if ( projectTypes.Length == 0 )
		{
			selectButton.Text = "No project components found";
			selectButton.Enabled = false;
			return;
		}

		selectButton.Clicked = () =>
		{
			OpenComponentMenu( selectButton, projectTypes, type =>
			{
				_settings.BaseComponentTypeName = type is null ? null : GetTypeName( type );
				selectButton.Text = type?.Title ?? "None";
				selectButton.Icon = type?.Icon ?? "arrow_drop_down";
				StateHasChanged();
			}, includeNone: true );
		};
	}

	void AddNewGroup()
	{
		var group = new ObjectPlacerGroup
		{
			Name = "New Group",
			Icon = "folder"
		};
		_settings.Groups.Add( group );
		_selectedItem = group;
		RebuildMaster();
		RebuildDetails();
		StateHasChanged();
	}

	void OpenAddMenu()
	{
		var menu = new ContextMenu( null );
		menu.AddOption( "Group", "create_new_folder", AddNewGroup );
		menu.AddOption( "Component Entry", "extension", () => AddNewEntry( ObjectPlacerEntryKind.Component ) );
		menu.AddOption( "Prefab Entry", "dataset", () => AddNewEntry( ObjectPlacerEntryKind.Prefab ) );
		menu.OpenAtCursor();
		menu.ConstrainToScreen();
	}

	void AddNewEntry( ObjectPlacerEntryKind kind )
	{
		var entry = new ObjectPlacerEntry
		{
			Name = kind == ObjectPlacerEntryKind.Prefab ? "New Prefab" : "New Component",
			Icon = kind == ObjectPlacerEntryKind.Prefab ? "dataset" : "extension",
			Kind = kind
		};
		_settings.Entries.Add( entry );
		_selectedItem = entry;
		RebuildMaster();
		RebuildDetails();
		StateHasChanged();
	}

	void ClearPreviewObjects()
	{
		foreach ( var go in _previewObjects )
			go?.Destroy();

		_previewObjects.Clear();
	}

	public override void OnDestroyed()
	{
		ClearPreviewObjects();
		base.OnDestroyed();
	}

	public override void OnSave()
	{
		_settings.Groups ??= [];
		_settings.Entries ??= [];

		foreach ( var entry in _settings.Entries )
			entry.PropertyOverrides ??= [];

		EditorUtility.SaveProjectSettings( _settings, "ObjectPlacer.config" );
		base.OnSave();
	}

}
