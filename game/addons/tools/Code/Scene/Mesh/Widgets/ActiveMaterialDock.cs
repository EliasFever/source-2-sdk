namespace Editor.MeshEditor;

[Dock( "Editor", "Active Material", "texture" )]
public class ActiveMaterialDock : Widget
{
	private Layout _content;
	private Widget _currentWidget;

	public ActiveMaterialDock( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.AddSeparator();

		MinimumWidth = 160f;
		MinimumHeight = 210f;

		var body = Layout.Add( new Widget(), 1 );
		body.Layout = Layout.Column();
		body.HorizontalSizeMode = SizeMode.Flexible;
		body.VerticalSizeMode = SizeMode.Flexible;

		_content = body.Layout;
		_content.Margin = 8;
		_content.Spacing = 6;
		_content.Alignment = TextFlag.Center;
		
		Rebuild();
	}

	private void Rebuild()
	{
		using var x = SuspendUpdates.For( this );

		_currentWidget?.Destroy();
		_currentWidget = null;

		_content.Clear( true );

		var materialProperty = MeshActiveMaterialState.Instance.GetSerialized().GetProperty( nameof( MeshActiveMaterialState.ActiveMaterial ) );
		var swatch = new ActiveMaterialWidget( materialProperty );
		swatch.HorizontalSizeMode = SizeMode.Flexible;
		swatch.VerticalSizeMode = SizeMode.Flexible;

		_content.Add( swatch, 1 );
		_currentWidget = swatch;
	}
}
