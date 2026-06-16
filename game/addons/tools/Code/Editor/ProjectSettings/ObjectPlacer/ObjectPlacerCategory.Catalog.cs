namespace Editor.ProjectSettingPages;

internal sealed partial class ObjectPlacerCategory
{
	void RebuildMaster()
	{
		if ( _masterList is null )
			return;

		var items = new List<ObjectPlacerCatalogItem>();

		foreach ( var group in _settings.Groups.ToArray() )
		{
			items.Add( CreateCatalogItem( group, 0, group.Icon, group.Name, $"{_settings.Entries.Count( x => x.GroupId == group.Id )} entries", "folder" ) );

			if ( CollapsedGroups.Contains( group.Id ) )
				continue;

			items.AddRange( _settings.Entries.Where( x => x.GroupId == group.Id ).ToArray().Select( entry => CreateEntryCatalogItem( entry, 1 ) ) );
		}

		var ungrouped = _settings.Entries.Where( x => string.IsNullOrWhiteSpace( x.GroupId ) || !_settings.Groups.Any( g => g.Id == x.GroupId ) ).ToArray();
		items.Add( CreateCatalogItem( ObjectPlacerUngroupedCatalogItem.Instance, 0, "folder_off", "Ungrouped", $"{ungrouped.Length} entries", "folder" ) );
		if ( !CollapsedGroups.Contains( UngroupedCatalogId ) )
			items.AddRange( ungrouped.Select( entry => CreateEntryCatalogItem( entry, 1 ) ) );

		_masterList.SetItems( items );
	}

	ObjectPlacerCatalogItem CreateEntryCatalogItem( ObjectPlacerEntry entry, int indent )
	{
		var group = _settings.Groups.FirstOrDefault( x => x.Id == entry.GroupId );
		var detail = entry.Kind == ObjectPlacerEntryKind.Prefab
			? "Prefab"
			: $"{(entry.ComponentSource == ObjectPlacerComponentSource.Project ? "Project" : "Base")} Component";

		return CreateCatalogItem( entry, indent, entry.Icon, entry.Name, $"{group?.Name ?? "Ungrouped"} - {detail}", entry.Kind == ObjectPlacerEntryKind.Prefab ? "dataset" : "extension" );
	}

	static ObjectPlacerCatalogItem CreateCatalogItem( object item, int indent, string icon, string title, string subtitle, string fallbackIcon )
	{
		return new ObjectPlacerCatalogItem( item, indent, string.IsNullOrWhiteSpace( icon ) ? fallbackIcon : icon, title, subtitle, fallbackIcon );
	}

	internal void SelectCatalogItem( object item )
	{
		if ( item is null )
			return;

		_selectedItem = item;
		RebuildMaster();
		RebuildDetails();
	}

	internal void ToggleCatalogGroup( ObjectPlacerGroup group )
	{
		if ( group is null )
			return;

		SetCollapsed( CollapsedGroups, group.Id, !CollapsedGroups.Contains( group.Id ) );
		RebuildMaster();
	}

	internal bool CanEditCatalogItem( object item ) => item is ObjectPlacerGroup or ObjectPlacerEntry;

	internal bool CanCollapseCatalogItem( object item ) => item is ObjectPlacerGroup or ObjectPlacerUngroupedCatalogItem;

	internal void ToggleCatalogItem( object item )
	{
		if ( item is ObjectPlacerGroup group )
		{
			ToggleCatalogGroup( group );
			return;
		}

		if ( item is ObjectPlacerUngroupedCatalogItem )
		{
			SetCollapsed( CollapsedGroups, UngroupedCatalogId, !CollapsedGroups.Contains( UngroupedCatalogId ) );
			RebuildMaster();
		}
	}

	internal void DuplicateCatalogItem( object item )
	{
		if ( item is ObjectPlacerGroup group )
		{
			var copy = DuplicateGroup( group );
			_settings.Groups.Insert( _settings.Groups.IndexOf( group ) + 1, copy );
			_selectedItem = copy;
		}
		else if ( item is ObjectPlacerEntry entry )
		{
			var copy = DuplicateEntry( entry );
			_settings.Entries.Insert( _settings.Entries.IndexOf( entry ) + 1, copy );
			_selectedItem = copy;
		}

		RebuildMaster();
		RebuildDetails();
		StateHasChanged();
	}

	internal void DeleteCatalogItem( object item )
	{
		if ( item is ObjectPlacerGroup group )
		{
			_settings.Groups.Remove( group );
			foreach ( var entry in _settings.Entries.Where( x => x.GroupId == group.Id ) )
				entry.GroupId = null;
		}
		else if ( item is ObjectPlacerEntry entry )
		{
			_settings.Entries.Remove( entry );
		}

		if ( ReferenceEquals( _selectedItem, item ) )
			_selectedItem = null;

		RebuildMaster();
		RebuildDetails();
		StateHasChanged();
	}

	internal void PaintCatalogItem( VirtualWidget item, ObjectPlacerCatalogListView list )
	{
		if ( item.Object is not ObjectPlacerCatalogItem catalogItem )
			return;

		var rect = item.Rect;
		var isSelectable = CanEditCatalogItem( catalogItem.Item );
		var selected = isSelectable && ReferenceEquals( _selectedItem, catalogItem.Item );
		var bg = selected ? Theme.Primary.WithAlpha( 0.28f ) : Theme.ControlBackground.WithAlpha( 0.45f );
		if ( item.Hovered )
			bg = selected ? Theme.Primary.WithAlpha( 0.36f ) : Theme.ControlBackground.WithAlpha( 0.7f );

		Paint.ClearPen();
		Paint.SetBrush( bg );
		Paint.DrawRect( rect, 3 );

		if ( list.TryGetDropPreview( catalogItem.Item, out var insertAfter ) )
		{
			var lineRect = rect;
			lineRect.Left = 0;
			lineRect.Right = list.Width;
			lineRect.Top = insertAfter ? lineRect.Bottom - 1 : lineRect.Top - 1;
			lineRect.Height = 2;
			Paint.SetBrush( Theme.TextHighlight );
			Paint.DrawRect( lineRect, 2 );
		}

		var dragGutter = new Rect( rect.Left, rect.Top, 22, rect.Height );
		Paint.SetBrush( item.Hovered ? Theme.ControlBackground.WithAlpha( 0.9f ) : Theme.ControlBackground.WithAlpha( 0.55f ) );
		Paint.DrawRect( dragGutter, 3 );
		Paint.SetBrush( Theme.WidgetBackground.WithAlpha( 0.75f ) );
		Paint.DrawRect( new Rect( dragGutter.Right, rect.Top + 4, 1, rect.Height - 8 ) );

		var dragRect = new Rect( rect.Left + 2, rect.Top + 6, 18, 22 );
		if ( isSelectable )
		{
			Paint.SetPen( Theme.TextControl.WithAlpha( item.Hovered ? 0.95f : 0.65f ) );
			Paint.DrawIcon( dragRect, "drag_indicator", 14, TextFlag.Center );
		}

		var x = dragGutter.Right + 2 + catalogItem.Indent * 18;

		var collapseRect = new Rect( x, rect.Top + 6, 22, 22 );
		if ( CanCollapseCatalogItem( catalogItem.Item ) )
		{
			Paint.SetPen( Theme.TextControl.WithAlpha( item.Hovered ? 1.0f : 0.75f ) );
			Paint.DrawIcon( collapseRect, IsCatalogItemCollapsed( catalogItem.Item ) ? "chevron_right" : "expand_more", 13, TextFlag.Center );
		}
		x += 24;

		var iconRect = new Rect( x, rect.Top + 1, 32, 32 );
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( iconRect, 3 );
		var innerIconRect = iconRect.Shrink( 7 );
		if ( !ObjectPlacerPreviewIcons.DrawConfiguredIcon( innerIconRect, catalogItem.Icon, 18 ) )
		{
			Paint.SetPen( Theme.Text.WithAlpha( 0.8f ) );
			Paint.DrawIcon( innerIconRect, string.IsNullOrWhiteSpace( catalogItem.Icon ) ? "category" : catalogItem.Icon, 18, TextFlag.Center );
		}
		x += 38;

		var deleteRect = GetCatalogDeleteRect( rect );
		var duplicateRect = GetCatalogDuplicateRect( rect );
		if ( CanEditCatalogItem( catalogItem.Item ) )
		{
			Paint.SetPen( Theme.TextControl.WithAlpha( item.Hovered ? 1.0f : 0.85f ) );
			Paint.DrawIcon( duplicateRect, "content_copy", 13, TextFlag.Center );
			Paint.DrawIcon( deleteRect, "close", 13, TextFlag.Center );
		}

		var textRight = isSelectable ? duplicateRect.Left - 6 : rect.Right - 6;
		var titleRect = new Rect( x, rect.Top + 2, Math.Max( 0, textRight - x ), 17 );
		var subtitleRect = new Rect( x, rect.Top + 18, Math.Max( 0, textRight - x ), 14 );
		Paint.SetPen( Theme.Text );
		Paint.DrawText( titleRect, string.IsNullOrWhiteSpace( catalogItem.Title ) ? "Unnamed" : catalogItem.Title, TextFlag.LeftCenter | TextFlag.SingleLine );
		Paint.SetPen( Theme.TextControl.WithAlpha( 0.55f ) );
		Paint.DrawText( subtitleRect, catalogItem.Subtitle ?? "", TextFlag.LeftCenter | TextFlag.SingleLine );
	}

	internal static Rect GetCatalogDeleteRect( Rect rowRect ) => new( rowRect.Right - 28, rowRect.Top + 6, 22, 22 );
	internal static Rect GetCatalogDuplicateRect( Rect rowRect ) => new( rowRect.Right - 56, rowRect.Top + 6, 22, 22 );
	internal static Rect GetCatalogCollapseRect( Rect rowRect, int indent ) => new( rowRect.Left + 24 + indent * 18, rowRect.Top + 6, 22, 22 );

	bool IsCatalogItemCollapsed( object item )
	{
		if ( item is ObjectPlacerGroup group )
			return CollapsedGroups.Contains( group.Id );

		return item is ObjectPlacerUngroupedCatalogItem && CollapsedGroups.Contains( UngroupedCatalogId );
	}

	internal void MoveCatalogItem( object dragged, object target, bool insertAfter )
	{
		if ( dragged is null || target is null || ReferenceEquals( dragged, target ) )
			return;

		if ( dragged is ObjectPlacerGroup draggedGroup )
		{
			var targetGroup = target as ObjectPlacerGroup;
			if ( target is ObjectPlacerEntry targetEntry )
				targetGroup = _settings.Groups.FirstOrDefault( x => x.Id == targetEntry.GroupId );

			if ( targetGroup is null || ReferenceEquals( draggedGroup, targetGroup ) )
				return;

			MoveObjectInList( _settings.Groups, draggedGroup, targetGroup, insertAfter );
			_selectedItem = draggedGroup;
			RebuildMaster();
			RebuildDetails();
			StateHasChanged();
			return;
		}

		if ( dragged is ObjectPlacerEntry draggedEntry )
		{
			_settings.Entries.Remove( draggedEntry );

			if ( target is ObjectPlacerUngroupedCatalogItem )
			{
				draggedEntry.GroupId = null;
				var insertIndex = GetUngroupedEntryInsertIndex( insertAfter );
				_settings.Entries.Insert( Math.Clamp( insertIndex, 0, _settings.Entries.Count ), draggedEntry );
			}
			else if ( target is ObjectPlacerGroup targetGroup )
			{
				draggedEntry.GroupId = targetGroup.Id;
				var insertIndex = GetGroupEntryInsertIndex( targetGroup.Id, insertAfter );
				_settings.Entries.Insert( Math.Clamp( insertIndex, 0, _settings.Entries.Count ), draggedEntry );
			}
			else if ( target is ObjectPlacerEntry targetEntry )
			{
				draggedEntry.GroupId = targetEntry.GroupId;
				var targetIndex = _settings.Entries.IndexOf( targetEntry );
				if ( targetIndex < 0 )
					targetIndex = _settings.Entries.Count;
				else if ( insertAfter )
					targetIndex++;

				_settings.Entries.Insert( Math.Clamp( targetIndex, 0, _settings.Entries.Count ), draggedEntry );
			}
			else
			{
				_settings.Entries.Add( draggedEntry );
			}

			_selectedItem = draggedEntry;
			RebuildMaster();
			RebuildDetails();
			StateHasChanged();
		}
	}

	static void MoveObjectInList<T>( List<T> list, T dragged, T target, bool insertAfter )
	{
		var oldIndex = list.IndexOf( dragged );
		var targetIndex = list.IndexOf( target );
		if ( oldIndex < 0 || targetIndex < 0 )
			return;

		list.RemoveAt( oldIndex );
		if ( oldIndex < targetIndex )
			targetIndex--;

		if ( insertAfter )
			targetIndex++;

		list.Insert( Math.Clamp( targetIndex, 0, list.Count ), dragged );
	}

	int GetGroupEntryInsertIndex( string groupId, bool insertAfter )
	{
		var indexes = _settings.Entries
			.Select( ( entry, index ) => (entry, index) )
			.Where( x => x.entry.GroupId == groupId )
			.Select( x => x.index )
			.ToArray();

		if ( indexes.Length == 0 )
			return _settings.Entries.Count;

		return insertAfter ? indexes.Max() + 1 : indexes.Min();
	}

	int GetUngroupedEntryInsertIndex( bool insertAfter )
	{
		var groupIds = _settings.Groups.Select( x => x.Id ).ToHashSet();
		var indexes = _settings.Entries
			.Select( ( entry, index ) => (entry, index) )
			.Where( x => string.IsNullOrWhiteSpace( x.entry.GroupId ) || !groupIds.Contains( x.entry.GroupId ) )
			.Select( x => x.index )
			.ToArray();

		if ( indexes.Length == 0 )
			return _settings.Entries.Count;

		return insertAfter ? indexes.Max() + 1 : indexes.Min();
	}

	static void SetCollapsed( HashSet<string> set, string id, bool collapsed )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return;

		if ( collapsed )
			set.Add( id );
		else
			set.Remove( id );
	}

	const string UngroupedCatalogId = "__ungrouped";

	static ObjectPlacerGroup DuplicateGroup( ObjectPlacerGroup source )
	{
		return new ObjectPlacerGroup
		{
			Id = Guid.NewGuid().ToString(),
			Name = string.IsNullOrWhiteSpace( source.Name ) ? "New Group Copy" : $"{source.Name} Copy",
			Icon = source.Icon,
			Description = source.Description,
			CollapsedByDefault = source.CollapsedByDefault,
			HideInTool = source.HideInTool
		};
	}

	static ObjectPlacerEntry DuplicateEntry( ObjectPlacerEntry source )
	{
		return new ObjectPlacerEntry
		{
			Id = Guid.NewGuid().ToString(),
			Name = string.IsNullOrWhiteSpace( source.Name ) ? "New Object Copy" : $"{source.Name} Copy",
			Icon = source.Icon,
			Description = source.Description,
			SearchText = source.SearchText,
			HideInTool = source.HideInTool,
			GroupId = source.GroupId,
			Kind = source.Kind,
			ComponentSource = source.ComponentSource,
			ComponentTypeName = source.ComponentTypeName,
			Prefab = source.Prefab,
			PropertyOverrides = source.PropertyOverrides?.Select( x => new ObjectPlacerPropertyOverride
			{
				PropertyName = x.PropertyName,
				Kind = x.Kind,
				Value = x.Value,
				ResourcePath = x.ResourcePath
			} ).ToList() ?? []
		};
	}
}
