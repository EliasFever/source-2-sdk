using Editor.MeshEditor;

[Dock( "Editor", "Active Material", "active_material" )]
public class ActiveMaterialDock : Widget
{
	readonly MaterialWidget _materialWidget;
	readonly MaterialPaletteWidget _paletteStrip;

	public Material ActiveMaterial { get; private set; }

	public ActiveMaterialDock( Widget parent ) : base( parent )
	{
		FixedHeight = 220;

		Layout = Layout.Row();
		Layout.Margin = 8;
		Layout.Spacing = 5;

		// Material display
		_materialWidget = Layout.Add( new MaterialWidget() );
		_materialWidget.ToolTip = "Active Material";
		_materialWidget.FixedSize = FixedHeight - 26;
		_materialWidget.Cursor = CursorShape.Finger;

		Layout.AddStretchCell( 1 );

		// Palette
		_paletteStrip = Layout.Add( new MaterialPaletteWidget() );
		_paletteStrip.MaterialClicked += OnPaletteMaterialClicked;
		_paletteStrip.FixedHeight = FixedHeight - 26;
		_paletteStrip.GetActiveMaterial = () => _materialWidget.Material;

		Layout.AddStretchCell( 1 );
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		// Update the material widget every frame
		_materialWidget.Material = ActiveMaterial;
	}

	/// <summary>
	/// Set the active material directly.
	/// Updates the material widget and the palette.
	/// </summary>
	public void SetMaterial( Material material )
	{
		ActiveMaterial = material;
		_materialWidget.Material = material;
		_paletteStrip.PushMaterial( material );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		// Only handle clicks on the main material widget
		if ( !_materialWidget.LocalRect.IsInside( e.LocalPosition ) ) return;

		var currentMaterial = ActiveMaterial;
		var asset = currentMaterial != null ? AssetSystem.FindByPath( currentMaterial.ResourcePath ) : null;
		var assetType = AssetType.Material;

		var picker = AssetPicker.Create( null, assetType, new AssetPicker.PickerOptions()
		{
			EnableMultiselect = false
		} );

		picker.Title = "Select Active Material";
		picker.OnAssetPicked = assets =>
		{
			var mat = assets.FirstOrDefault()?.LoadResource( typeof( Material ) ) as Material;
			if ( mat != null )
			{
				SetMaterial( mat );
			}
		};

		picker.Show();

		picker.SetSelection( asset );
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		var m = new ContextMenu();
		var material = ActiveMaterial;
		var asset = material != null ? AssetSystem.FindByPath( material.ResourcePath ) : null;

		m.AddOption( "Open in Editor", "edit", () => asset?.OpenInEditor() )
			.Enabled = asset != null && !asset.IsProcedural;
		m.AddOption( "Find in Asset Browser", "search", () => LocalAssetBrowser.OpenTo( asset, true ) )
			.Enabled = asset != null;
		m.AddSeparator();
		m.AddOption( "Copy", "file_copy", () => CopyMaterial() )
			.Enabled = material != null;
		m.AddOption( "Paste", "content_paste", () => PasteMaterial() );
		m.AddSeparator();
		m.AddOption( "Clear", "backspace", () => SetMaterial( null ) )
			.Enabled = material != null;

		m.OpenAtCursor( false );
		e.Accepted = true;
	}

	void CopyMaterial()
	{
		var material = ActiveMaterial;
		if ( material == null ) return;
		var asset = AssetSystem.FindByPath( material.ResourcePath );
		if ( asset == null ) return;
		EditorUtility.Clipboard.Copy( asset.Path );
	}

	void PasteMaterial()
	{
		var path = EditorUtility.Clipboard.Paste();
		var asset = AssetSystem.FindByPath( path );
		if ( asset == null ) return;

		var mat = asset.LoadResource( typeof( Material ) ) as Material;
		if ( mat != null )
		{
			SetMaterial( mat );
		}
	}

	void OnPaletteMaterialClicked( Material material )
	{
		if ( material != null )
			SetMaterial( material );
	}
}
