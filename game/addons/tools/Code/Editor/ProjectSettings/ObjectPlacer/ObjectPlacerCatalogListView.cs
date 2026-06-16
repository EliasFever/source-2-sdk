namespace Editor.ProjectSettingPages;

internal sealed record ObjectPlacerCatalogDragData( object Item );

internal sealed record ObjectPlacerCatalogItem( object Item, int Indent, string Icon, string Title, string Subtitle, string FallbackIcon );

internal sealed class ObjectPlacerUngroupedCatalogItem
{
	public static readonly ObjectPlacerUngroupedCatalogItem Instance = new();

	ObjectPlacerUngroupedCatalogItem()
	{
	}
}

internal sealed record DetailChoice( string Text, string Icon, bool Selected, Action Select );

internal sealed class ObjectPlacerCatalogListView : ListView
{
	readonly ObjectPlacerCategory _category;
	object _dropPreviewItem;
	bool _dropPreviewAfter;

	public ObjectPlacerCatalogListView( ObjectPlacerCategory category, Widget parent ) : base( parent )
	{
		_category = category;
		ItemPaint = item => _category.PaintCatalogItem( item, this );
	}

	public bool TryGetDropPreview( object item, out bool insertAfter )
	{
		insertAfter = _dropPreviewAfter;
		return item is not null && ReferenceEquals( _dropPreviewItem, item );
	}

	protected override bool OnItemPressed( VirtualWidget pressedItem, MouseEvent e )
	{
		if ( pressedItem.Object is not ObjectPlacerCatalogItem catalogItem )
			return true;

		if ( catalogItem.Item is null )
			return false;

		if ( _category.CanEditCatalogItem( catalogItem.Item ) && ObjectPlacerCategory.GetCatalogDeleteRect( pressedItem.Rect ).IsInside( e.LocalPosition ) )
		{
			_category.DeleteCatalogItem( catalogItem.Item );
			return false;
		}

		if ( _category.CanEditCatalogItem( catalogItem.Item ) && ObjectPlacerCategory.GetCatalogDuplicateRect( pressedItem.Rect ).IsInside( e.LocalPosition ) )
		{
			_category.DuplicateCatalogItem( catalogItem.Item );
			return false;
		}

		if ( _category.CanCollapseCatalogItem( catalogItem.Item ) && ObjectPlacerCategory.GetCatalogCollapseRect( pressedItem.Rect, catalogItem.Indent ).IsInside( e.LocalPosition ) )
		{
			_category.ToggleCatalogItem( catalogItem.Item );
			return false;
		}

		if ( !_category.CanEditCatalogItem( catalogItem.Item ) )
			return false;

		_category.SelectCatalogItem( catalogItem.Item );
		return true;
	}

	protected override bool OnDragItem( VirtualWidget item )
	{
		if ( item.Object is not ObjectPlacerCatalogItem catalogItem || !_category.CanEditCatalogItem( catalogItem.Item ) )
			return false;

		var drag = new Drag( this )
		{
			Data = { Object = new ObjectPlacerCatalogDragData( catalogItem.Item ), Text = catalogItem.Title }
		};
		drag.Execute();
		return true;
	}

	protected override DropAction OnItemDrag( ItemDragEvent e )
	{
		if ( !TryGetDragData( e, out var dragData ) || e.Item.Object is not ObjectPlacerCatalogItem target || target.Item is null || ReferenceEquals( dragData.Item, target.Item ) )
		{
			ClearDropPreview();
			return DropAction.Ignore;
		}

		var insertAfter = e.DropEdge.HasFlag( ItemEdge.Bottom ) || e.LocalPosition.y > e.Item.Rect.Height * 0.5f;

		if ( e.IsDrop )
		{
			_category.MoveCatalogItem( dragData.Item, target.Item, insertAfter );
			ClearDropPreview();
			return DropAction.Move;
		}

		SetDropPreview( target.Item, insertAfter );
		return DropAction.Move;
	}

	void SetDropPreview( object item, bool insertAfter )
	{
		if ( ReferenceEquals( _dropPreviewItem, item ) && _dropPreviewAfter == insertAfter )
			return;

		_dropPreviewItem = item;
		_dropPreviewAfter = insertAfter;
		Update();
	}

	void ClearDropPreview()
	{
		if ( _dropPreviewItem is null )
			return;

		_dropPreviewItem = null;
		_dropPreviewAfter = false;
		Update();
	}

	static bool TryGetDragData( ItemDragEvent ev, out ObjectPlacerCatalogDragData data )
	{
		data = ev.Data.OfType<ObjectPlacerCatalogDragData>().FirstOrDefault();
		if ( data is not null )
			return true;

		data = ev.Data.Object as ObjectPlacerCatalogDragData;
		return data is not null;
	}
}
