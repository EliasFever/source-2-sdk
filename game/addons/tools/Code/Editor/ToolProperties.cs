namespace Editor;

[Dock( "Editor", "Tool Properties", "build" )]
public class ToolPropertiesWindow : Widget
{
	Layout Root;

	private Layout _content;
	private Widget _currentToolWidget;

	private EditorTool _activeTool;
	private int _selectionHash;

	private enum ToolWidgetSizing
	{
		Fill,
		FixedWidthLeft,
		Natural
	}


	public ToolPropertiesWindow( Widget parent ) : base( parent )
	{
		Root = Layout.Column();
		Layout = Root;

		Root.AddSeparator();

		this.MinimumWidth = 240f;

		var scroll = new ScrollArea( this )
		{
			VerticalSizeMode = SizeMode.Flexible, 
			HorizontalSizeMode = SizeMode.Flexible, 
			VerticalScrollbarMode = ScrollbarMode.Auto,
			HorizontalScrollbarMode = ScrollbarMode.Off
		};

		var scrollContentWidget = new Widget( scroll )
		{
			Layout = Layout.Column(),
			VerticalSizeMode = SizeMode.CanGrow,
			HorizontalSizeMode = SizeMode.Flexible
		};

		// Assign the layout to _content for later updates
		_content = scrollContentWidget.Layout;
		_content.Spacing = 5;
		_content.AddStretchCell();

		scroll.Canvas = scrollContentWidget;
		scroll.Canvas.MinimumWidth = 0f;
		Root.Add( scroll, 1 );
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		var activeView = SceneViewWidget.Current;
		if ( activeView is null ) return;

		var tool = activeView.Tools.CurrentSubTool ?? activeView.Tools.CurrentTool;

		if ( tool is null )
		{
			ShowNoToolSelected();
			return;
		}

		// Compute selection hash
		int newSelectionHash = tool.Selection?.GetHashCode() ?? 0;
		bool selectionChanged = newSelectionHash != _selectionHash;

		bool shouldRebuild =
			tool != _activeTool ||
			(selectionChanged && (tool.RebuildSidebarOnSelectionChange));

		if ( shouldRebuild )
		{
			_activeTool = tool;
			_selectionHash = newSelectionHash;
			RebuildForTool( tool );
		}
	}

	private void ShowNoToolSelected()
	{
		_currentToolWidget?.Destroy();
		_currentToolWidget = null;

		using var x = SuspendUpdates.For( this );
		_content.Clear( true );

		_content.Add( new Label( "No tool selected" ) );
		_content.AddStretchCell();
	}

	private void RebuildForTool( EditorTool tool )
	{
		using var x = SuspendUpdates.For( this );

		_content.Clear( true );
		
		// Edge case: We need to pass Terrain Tool a specific way here
		// In the future they might add a new subtool/handle a certain existing one diff
		// So might need to update this in the future (?)
		if ( tool is TerrainEditor.BaseBrushTool || tool is TerrainEditor.PaintTextureTool )
		{
			var activeView = SceneViewWidget.Current;
			var terrainRootTool = activeView?.Tools?.CurrentTool as TerrainEditor.TerrainEditorTool;

			var terrainWidget =
				terrainRootTool?.CreateToolSidebar()
				?? terrainRootTool?.CreateToolFooter()
				?? terrainRootTool?.CreateShortcutsWidget();

			AddWidgetToContent( terrainWidget );
			_content.AddStretchCell();
			
			return;
		}

		// Edge case: If FaceTool is active, show TextureTool as well
		// I don't care what Garry says, face and texture tools come hand in hand
		if ( tool is MeshEditor.FaceTool faceTool )
		{
			// FaceTool comes first
			var faceWidget =
				faceTool.CreateToolSidebar()
				?? faceTool.CreateToolFooter()
				?? faceTool.CreateShortcutsWidget();

			AddWidgetToContent( faceWidget );

			var activeView = SceneViewWidget.Current;
			if ( activeView != null )
			{
				// TextureTool comes after
				var textureTool = faceTool.ParentTool.Tools
					.OfType<MeshEditor.TextureTool>()
					.FirstOrDefault();

				if ( textureTool != null )
				{
					var texWidget =
						textureTool.CreateToolSidebar()
						?? textureTool.CreateToolFooter()
						?? textureTool.CreateShortcutsWidget();

					AddWidgetToContent( texWidget );
				}
			}

			return;
		}

		var widget =
			tool.CreateToolSidebar()
			?? tool.CreateToolFooter()
			?? tool.CreateShortcutsWidget();

		AddWidgetToContent( widget );
		_content.AddStretchCell();
	}

	private void AddWidgetToContent( Widget widget )
	{
		if ( widget == null || !widget.IsValid() )
		{
			var fallback = new Label( $"This tool has no properties." )
			{
				HorizontalSizeMode = SizeMode.Flexible,
				MinimumWidth = 0
			};
			_content.Margin = 16;
			_content.Add( fallback );
			_currentToolWidget = fallback;
			return;
		}

		widget.MinimumWidth = 240f;
		widget.HorizontalSizeMode = SizeMode.Expand | SizeMode.CanGrow;
		widget.VerticalSizeMode = SizeMode.Default;
		
		_content.Margin = 0;
		_content.Add( widget, 1 );
		_currentToolWidget = widget;
	}

}
