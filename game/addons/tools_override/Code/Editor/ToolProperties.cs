namespace Editor;

[Dock( "Editor", "Tool Properties", "tool_properties" )]
public class ToolPropertiesWindow : Widget
{
	Layout Root;

	private Layout _content;
	private Widget _currentToolWidget;

	private EditorTool _activeTool;
	private int _selectionHash;

	public ToolPropertiesWindow( Widget parent ) : base( parent )
	{
		Root = Layout.Column();
		Layout = Root;

		Root.AddSeparator();

		var scroll = new ScrollArea( this )
		{
			VerticalSizeMode = SizeMode.Flexible, 
			HorizontalSizeMode = SizeMode.Flexible, 
			VerticalScrollbarMode = ScrollbarMode.Auto,
			HorizontalScrollbarMode = ScrollbarMode.Off
		};

		var scrollContentWidget = new Widget( scroll )
		{
			Layout = Layout.Column()
		};

		// Assign the layout to _content for later updates
		_content = scrollContentWidget.Layout;
		_content.Spacing = 5;

		scroll.Canvas = scrollContentWidget;

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
		if ( _currentToolWidget is not null )
		{
			_currentToolWidget.Destroy();
			_currentToolWidget = null;
		}

		using var x = SuspendUpdates.For( this );
		_content.Clear( true );

		_content.Add( new Label( "No tool selected" ) );
	}

	private void RebuildForTool( EditorTool tool )
	{
		using var x = SuspendUpdates.For( this );

		_content.Clear( true );

		// Edge case: If FaceTool is active, show TextureTool as well
		if ( tool is MeshEditor.FaceTool faceTool )
		{
			var activeView = SceneViewWidget.Current;
			if ( activeView != null )
			{
				// TextureTool comes first
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

			// FaceTool comes afterwards
			var faceWidget =
				faceTool.CreateToolSidebar()
				?? faceTool.CreateToolFooter()
				?? faceTool.CreateShortcutsWidget();

			AddWidgetToContent( faceWidget );

			return;
		}

		// Only show the tool's widget
		var widget =
			tool.CreateToolSidebar()
			?? tool.CreateToolFooter()
			?? tool.CreateShortcutsWidget();

		AddWidgetToContent( widget );
	}

	private void AddWidgetToContent( Widget widget )
	{
		if ( widget == null || !widget.IsValid() )
		{
			var fallback = new Label( "This tool has no properties." );
			fallback.HorizontalSizeMode = SizeMode.Flexible;
			fallback.MinimumWidth = 0;

			_content.Add( fallback );
			_currentToolWidget = fallback;
			return;
		}

		widget.MinimumWidth = 0; // Allow shrinking
		widget.HorizontalSizeMode = SizeMode.Flexible;
		widget.VerticalSizeMode = SizeMode.Default;

		widget.ForceFlexibleWidth();		// Make sure it fits our panel


		_content.Add( widget, 0 );
		_currentToolWidget = widget;
	}

}
