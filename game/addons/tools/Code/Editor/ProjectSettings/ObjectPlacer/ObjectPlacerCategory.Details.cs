namespace Editor.ProjectSettingPages;

internal sealed partial class ObjectPlacerCategory
{
	void RebuildDetails()
	{
		ClearPreviewObjects();

		if ( _detailLayout is null )
			return;

		_detailLayout.Clear( true );

		if ( _selectedItem is ObjectPlacerGroup group && _settings.Groups.Contains( group ) )
		{
			AddGroupDetails( group );
			RefreshDetails();
			return;
		}

		if ( _selectedItem is ObjectPlacerEntry entry && _settings.Entries.Contains( entry ) )
		{
			AddEntryDetails( entry );
			RefreshDetails();
			return;
		}

		_selectedItem = _settings.Entries.FirstOrDefault() as object ?? _settings.Groups.FirstOrDefault();
		if ( _selectedItem is not null )
		{
			RebuildDetails();
			return;
		}

		_detailLayout.Add( EmptyLabel( "Select a group or entry to edit it." ) );
		RefreshDetails();
	}

	void RefreshDetails()
	{
		_detailCard?.UpdateGeometry();
		_detailCard?.AdjustSize();
		_detailCard?.Update();
	}

	void AddGroupDetails( ObjectPlacerGroup group )
	{
		var header = _detailLayout.AddRow();
		header.Spacing = 8;
		header.Add( IconPreview( group.Icon ) );
		header.Add( new Label.Header( string.IsNullOrWhiteSpace( group.Name ) ? "Group" : group.Name ), 1 );

		AddTextRow( _detailLayout, "Name", group.Name, value =>
		{
			group.Name = value;
			RebuildMaster();
		} );
		AddTextRow( _detailLayout, "Icon", group.Icon, value =>
		{
			group.Icon = value;
			RebuildMaster();
		} );
		AddTextRow( _detailLayout, "Tooltip", group.Description, value => group.Description = value );
		AddVisibilityRow( group );
		AddChoiceRow( _detailLayout, "Default State",
		[
			new DetailChoice( "Expanded", "unfold_more", !group.CollapsedByDefault, () =>
			{
				group.CollapsedByDefault = false;
				StateHasChanged();
			} ),
			new DetailChoice( "Collapsed", "unfold_less", group.CollapsedByDefault, () =>
			{
				group.CollapsedByDefault = true;
				StateHasChanged();
			} )
		] );
		_detailLayout.AddStretchCell();
	}

	void AddEntryDetails( ObjectPlacerEntry entry )
	{
		entry.PropertyOverrides ??= [];
		var serializedEntry = entry.GetSerialized();
		serializedEntry.OnPropertyChanged += changed => OnEntryPropertyChanged( entry, changed );

		var header = _detailLayout.AddRow();
		header.Spacing = 8;
		header.Add( IconPreview( entry.Icon ) );
		header.Add( new Label.Header( string.IsNullOrWhiteSpace( entry.Name ) ? "Entry" : entry.Name ), 1 );
		AddPropertyDropdown( header, serializedEntry.GetProperty( nameof( ObjectPlacerEntry.Kind ) ), minWidth: 142, maxWidth: 176 );

		AddTextRow( _detailLayout, "Name", entry.Name, value =>
		{
			entry.Name = value;
			RebuildMaster();
		} );
		AddTextRow( _detailLayout, "Icon", entry.Icon, value =>
		{
			entry.Icon = value;
			RebuildMaster();
		} );
		AddTextRow( _detailLayout, "Tooltip", entry.Description, value => entry.Description = value );
		AddTextRow( _detailLayout, "Search Text", entry.SearchText, value => entry.SearchText = value );
		AddVisibilityRow( entry );
		AddGroupRow( _detailLayout, entry );

		if ( entry.Kind == ObjectPlacerEntryKind.Prefab )
		{
			AddPrefabRow( _detailLayout, entry );
			_detailLayout.AddStretchCell();
			return;
		}

		var componentType = FindComponentType( entry.ComponentTypeName );
		AddPropertyDropdownRow( _detailLayout, "Component Source", serializedEntry.GetProperty( nameof( ObjectPlacerEntry.ComponentSource ) ) );
		if ( entry.ComponentSource == ObjectPlacerComponentSource.Base )
			AddComponentSelectorRow( _detailLayout, "Component", GetBaseComponentTypes(), entry, componentType, type => !IsProjectComponent( type ) );
		else
			AddComponentSelectorRow( _detailLayout, "Component", GetProjectComponentTypes(), entry, componentType, type => IsProjectComponent( type ) );
		AddComponentRows( _detailLayout, entry );
		_detailLayout.AddStretchCell();
	}

	void AddVisibilityRow( ObjectPlacerGroup group )
	{
		AddChoiceRow( _detailLayout, "Visibility",
		[
			new DetailChoice( "Visible", "visibility", !group.HideInTool, () => SetHidden( group, false ) ),
			new DetailChoice( "Hidden", "visibility_off", group.HideInTool, () => SetHidden( group, true ) )
		] );
	}

	void AddVisibilityRow( ObjectPlacerEntry entry )
	{
		AddChoiceRow( _detailLayout, "Visibility",
		[
			new DetailChoice( "Visible", "visibility", !entry.HideInTool, () => SetHidden( entry, false ) ),
			new DetailChoice( "Hidden", "visibility_off", entry.HideInTool, () => SetHidden( entry, true ) )
		] );
	}

	void SetHidden( ObjectPlacerGroup group, bool hidden )
	{
		group.HideInTool = hidden;
		RebuildMaster();
		StateHasChanged();
	}

	void SetHidden( ObjectPlacerEntry entry, bool hidden )
	{
		entry.HideInTool = hidden;
		RebuildMaster();
		StateHasChanged();
	}

	void OnEntryPropertyChanged( ObjectPlacerEntry entry, SerializedProperty property )
	{
		if ( property?.Name == nameof( ObjectPlacerEntry.ComponentSource ) )
		{
			entry.ComponentTypeName = null;
			entry.PropertyOverrides.Clear();
		}

		RebuildMaster();
		RebuildDetails();
		StateHasChanged();
	}
}
