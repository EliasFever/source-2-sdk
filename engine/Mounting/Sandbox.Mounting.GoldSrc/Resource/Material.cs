
class MaterialLoader( string texturePath ) : ResourceLoader<GameMount>
{
	protected override object Load()
	{
		var material = Material.Create( "model", "goldsrc" );
		material?.Set( "Color", Texture.Load( texturePath ) );

		return material;
	}
}
