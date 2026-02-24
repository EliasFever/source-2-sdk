
namespace Editor.MeshEditor;

class ActiveMaterialWidget : ControlWidget
{
	public override bool IsControlButton => !IsControlDisabled;

	readonly MaterialWidget _materialWidget = null;
	readonly MaterialPaletteWidget _paletteStrip;
	readonly Widget _previewRow;
	readonly Widget _materialPathLabel;
	Vector2 _lastContentSize;
	readonly bool _compactMode;

	const float LayoutGap = 1f;
	const float PaletteRows = 6f;
	const float PaletteCols = 2f;
	const float PaletteCellSpacing = 2f;
	const float BaseDisplayHeight = 260f;
	const float FooterFixedHeight = 220f;
	const float MaterialAspect = 1.0f;
	const float MinPaletteCellSize = 20f;
	const float MaxPaletteCellSize = 56f;

	public string Filename;
	public Color BaseColor = Theme.TextLight;

	public ActiveMaterialWidget( SerializedProperty property, bool compact = false ) : base( property )
	{
		_compactMode = compact;
		Layout = Layout.Column();
		Layout.Alignment = TextFlag.CenterBottom;
		Layout.Spacing = compact ? 2 : 4;
		ToolTip = "";

		_previewRow = Layout.Add( new Widget(), 1 );
		_previewRow.Layout = Layout.Row();
		_previewRow.Layout.Alignment = TextFlag.Center;
		_previewRow.HorizontalSizeMode = SizeMode.Flexible;
		_previewRow.VerticalSizeMode = SizeMode.Flexible;

		_materialWidget = _previewRow.Layout.Add( new MaterialWidget( this ) );
		_materialWidget.ToolTip = "Active Material";
		_materialWidget.ShowFilename = false;
		_materialWidget.HorizontalSizeMode = SizeMode.Flexible;
		_materialWidget.VerticalSizeMode = SizeMode.Flexible;
		_materialWidget.MinimumWidth = 0;

		_materialWidget.Cursor = CursorShape.Finger;

		_previewRow.Layout.AddSpacingCell( LayoutGap );

		_paletteStrip = _previewRow.Layout.Add( new MaterialPaletteWidget() );
		_paletteStrip.MaterialClicked += OnPaletteMaterialClicked;
		_paletteStrip.HorizontalSizeMode = SizeMode.Flexible;
		_paletteStrip.VerticalSizeMode = SizeMode.Flexible;
		_paletteStrip.MaximumWidth = 8192;
		_paletteStrip.GetActiveMaterial = () => _materialWidget.Material;

		_materialPathLabel = Layout.Add( new Widget(), 0 );
		_materialPathLabel.HorizontalSizeMode = SizeMode.Flexible;
		_materialPathLabel.FixedHeight = compact ? 16 : 20;

		_materialPathLabel.OnPaintOverride = () =>
		{
			var resource = SerializedProperty.GetValue<Resource>( null );
			var material = resource as Material;
			var filename = material?.ResourcePath;

			if ( string.IsNullOrEmpty( filename ) )
				return false;

			DrawFilename( _materialPathLabel.ContentRect, filename, TextFlag.LeftCenter, Theme.TextLight );

			return true;
		};

		if ( _compactMode )
		{
			ApplyFixedFooterSizing();
		}

		Frame();
	}

	protected override void OnPaint()
	{
		if ( _compactMode )
			return;

		UpdateResponsiveSizing();
	}

	void ApplyFixedFooterSizing()
	{
		var footerLabelHeight = _materialPathLabel.FixedHeight;
		var footerSpacing = Layout.Spacing;
		var previewHeight = MathF.Max( 1f, FooterFixedHeight - footerLabelHeight - footerSpacing );

		var baseCellSize = (previewHeight - ((PaletteRows - 1f) * PaletteCellSpacing)) / PaletteRows;
		baseCellSize = MathX.Clamp( baseCellSize, MinPaletteCellSize, MaxPaletteCellSize );

		var paletteWidth = (baseCellSize * PaletteCols) + ((PaletteCols - 1f) * PaletteCellSpacing);
		var materialWidth = previewHeight * MaterialAspect;
		var previewWidth = materialWidth + LayoutGap + paletteWidth;

		FixedHeight = FooterFixedHeight;
		MinimumHeight = FooterFixedHeight;
		MaximumHeight = FooterFixedHeight;
		VerticalSizeMode = SizeMode.Default;

		_previewRow.FixedWidth = previewWidth;
		_previewRow.FixedHeight = previewHeight;
		_previewRow.HorizontalSizeMode = SizeMode.Default;
		_previewRow.VerticalSizeMode = SizeMode.Default;

		_materialWidget.FixedWidth = materialWidth;
		_materialWidget.FixedHeight = previewHeight;
		_paletteStrip.FixedWidth = paletteWidth;
		_paletteStrip.FixedHeight = previewHeight;
	}

	void DrawFilename( Rect rect, string filename, TextFlag flags, Color color )
	{
		var dir = System.IO.Path.GetDirectoryName( filename );
		dir = string.IsNullOrEmpty( dir ) ? "" : dir + "/";

		var file = System.IO.Path.GetFileNameWithoutExtension( filename );
		var extension = System.IO.Path.GetExtension( filename );

		var size = Paint.MeasureText( rect, filename, flags );
		var overshoot = size.Width - rect.Width + 5;

		if ( overshoot > 0 )
		{
			overshoot += 10;
			var startIndex = (overshoot / 4).CeilToInt();

			dir = startIndex < dir.Length
				? string.Concat( "..", dir.AsSpan( startIndex ) )
				: "";
		}

		dir = dir.Replace( '\\', '/' );

		Paint.SetPen( color.Darken( 0.3f ) );
		var r = Paint.DrawText( rect, dir, flags );

		rect.Left += r.Width;

		Paint.SetPen( color );
		r = Paint.DrawText( rect, file, flags );

		rect.Left += r.Width;

		Paint.SetPen( color.Darken( 0.1f ) );
		Paint.DrawText( rect, extension, flags );
	}

	void UpdateResponsiveSizing()
	{
		var previewSize = _previewRow.ContentRect.Size;
		if ( previewSize == _lastContentSize )
			return;

		_lastContentSize = previewSize;

		var availableWidth = previewSize.x;
		var availableHeight = previewSize.y;

		var baseCellSize = (BaseDisplayHeight - ((PaletteRows - 1f) * PaletteCellSpacing)) / PaletteRows;
		baseCellSize = MathX.Clamp( baseCellSize, MinPaletteCellSize, MaxPaletteCellSize );
		var basePaletteWidth = (baseCellSize * PaletteCols) + ((PaletteCols - 1f) * PaletteCellSpacing);
		var baseMaterialWidth = BaseDisplayHeight * MaterialAspect;
		var baseTotalWidth = baseMaterialWidth + LayoutGap + basePaletteWidth;

		if ( baseTotalWidth <= 0f || BaseDisplayHeight <= 0f )
			return;

		var scaleFromHeight = availableHeight / BaseDisplayHeight;
		var scaleFromWidth = availableWidth / baseTotalWidth;
		var scale = MathF.Max( 0.1f, MathF.Min( scaleFromHeight, scaleFromWidth ) );

		var targetHeight = BaseDisplayHeight * scale;
		var materialWidth = baseMaterialWidth * scale;
		var paletteWidth = basePaletteWidth * scale;

		_materialWidget.FixedWidth = materialWidth;
		_materialWidget.FixedHeight = targetHeight;
		_paletteStrip.FixedWidth = paletteWidth;
		_paletteStrip.FixedHeight = targetHeight;
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		var m = new ContextMenu();

		var resource = SerializedProperty.GetValue<Resource>( null );
		var asset = (resource != null) ? AssetSystem.FindByPath( resource.ResourcePath ) : null;

		m.AddOption( "Open in Editor", "edit", () => asset?.OpenInEditor() ).Enabled = asset != null && !asset.IsProcedural;
		m.AddOption( "Find in Asset Browser", "search", () => LocalAssetBrowser.OpenTo( asset, true ) ).Enabled = asset is not null;
		m.AddSeparator();
		m.AddOption( "Copy", "file_copy", action: Copy ).Enabled = asset != null;
		m.AddOption( "Paste", "content_paste", action: Paste );
		m.AddSeparator();
		m.AddOption( "Select Faces Using Material", "texture", action: SelectFacesWithMaterial ).Enabled = resource is Material;
		m.AddOption( "Select Objects Using Material", "category", action: SelectObjectsWithMaterial ).Enabled = resource is Material;
		m.AddSeparator();
		m.AddOption( "Clear", "backspace", action: Clear ).Enabled = resource != null;

		m.OpenAtCursor( false );
		e.Accepted = true;
	}

	void Copy()
	{
		var resource = SerializedProperty.GetValue<Resource>( null );
		if ( resource == null ) return;

		var asset = AssetSystem.FindByPath( resource.ResourcePath );
		if ( asset == null ) return;

		EditorUtility.Clipboard.Copy( asset.Path );
	}

	void Paste()
	{
		var path = EditorUtility.Clipboard.Paste();
		var asset = AssetSystem.FindByPath( path );
		UpdateFromAsset( asset );
	}

	void Clear()
	{
		SerializedProperty.Parent.NoteStartEdit( SerializedProperty );
		SerializedProperty.SetValue( (Resource)null );
		SerializedProperty.Parent.NoteFinishEdit( SerializedProperty );
	}

	void SelectFacesWithMaterial()
	{
		var material = SerializedProperty.GetValue<Resource>( null ) as Material;
		if ( material is null ) return;

		var selection = SceneEditorSession.Active.Selection;
		var scene = SceneEditorSession.Active.Scene;

		if ( !Application.KeyboardModifiers.HasFlag( KeyboardModifiers.Shift ) )
			selection.Clear();

		foreach ( var component in scene.GetAllComponents<MeshComponent>() )
		{
			if ( !component.IsValid() ) continue;

			var mesh = component.Mesh;
			if ( mesh is null ) continue;

			foreach ( var face in mesh.FaceHandles )
			{
				var faceMaterial = mesh.GetFaceMaterial( face );

				if ( faceMaterial != null && material != null &&
					faceMaterial.ResourcePath == material.ResourcePath )
				{
					selection.Add( new MeshFace( component, face ) );
				}
			}
		}

		EditorToolManager.SetSubTool( nameof( FaceTool ) );
	}

	void SelectObjectsWithMaterial()
	{
		var material = SerializedProperty.GetValue<Resource>( null ) as Material;
		if ( material is null ) return;

		var selection = SceneEditorSession.Active.Selection;
		var scene = SceneEditorSession.Active.Scene;

		if ( !Application.KeyboardModifiers.HasFlag( KeyboardModifiers.Shift ) )
			selection.Clear();

		var objectsWithMaterial = new HashSet<GameObject>();

		foreach ( var component in scene.GetAllComponents<MeshComponent>() )
		{
			if ( !component.IsValid() ) continue;

			var mesh = component.Mesh;
			if ( mesh is null ) continue;

			foreach ( var face in mesh.FaceHandles )
			{
				var faceMaterial = mesh.GetFaceMaterial( face );

				if ( faceMaterial != null && material != null &&
					faceMaterial.ResourcePath == material.ResourcePath )
				{
					objectsWithMaterial.Add( component.GameObject );
					break;
				}
			}
		}

		foreach ( var obj in objectsWithMaterial )
		{
			selection.Add( obj );
		}

		EditorToolManager.SetSubTool( nameof( ObjectSelection ) );
	}

	private void UpdateFromAsset( Asset asset )
	{
		if ( asset is null ) return;

		var resource = asset.LoadResource( SerializedProperty.PropertyType );
		if ( resource is null ) return;

		SerializedProperty.Parent.NoteStartEdit( SerializedProperty );
		SerializedProperty.SetValue( resource );
		SerializedProperty.Parent.NoteFinishEdit( SerializedProperty );
	}

	public void UpdateFromMaterial( Material material )
	{
		if ( material is null ) return;

		SerializedProperty.Parent.NoteStartEdit( SerializedProperty );
		SerializedProperty.SetValue( material );
		SerializedProperty.Parent.NoteFinishEdit( SerializedProperty );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		// If we are selecting the Material Widget continue. (Probably better way of doing this)
		if ( !_materialWidget.ContentRect.IsInside( e.LocalPosition ) )
			return;

		if ( ReadOnly ) return;

		var resource = SerializedProperty.GetValue<Resource>( null );
		var asset = resource != null ? AssetSystem.FindByPath( resource.ResourcePath ) : null;

		var assetType = AssetType.FromType( resource.IsValid() ? resource.GetType() : SerializedProperty.PropertyType );

		PropertyStartEdit();

		var picker = AssetPicker.Create( null, assetType, new AssetPicker.PickerOptions()
		{
			EnableMultiselect = false
		} );
		picker.Title = $"Select {SerializedProperty.DisplayName}";
		picker.OnAssetHighlighted = ( o ) => UpdateFromAsset( o.FirstOrDefault() );
		picker.OnAssetPicked = ( o ) =>
		{
			UpdateFromAsset( o.FirstOrDefault() );
			PropertyFinishEdit();
		};

		picker.Show();

		picker.SetSelection( asset );
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		var resource = SerializedProperty.GetValue<Resource>( null );
		var material = resource as Material;

		_materialWidget.Material = material;
	}

	void OnPaletteMaterialClicked( Material material )
	{
		if ( ReadOnly || material is null ) return;

		SerializedProperty.Parent.NoteStartEdit( SerializedProperty );
		SerializedProperty.SetValue( material );
		SerializedProperty.Parent.NoteFinishEdit( SerializedProperty );
	}
}
