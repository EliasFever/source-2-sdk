namespace Editor.MeshEditor;

/// <summary>
/// Shared active material state for mesh tools/dock.
/// </summary>
public sealed class MeshActiveMaterialState
{
	public static MeshActiveMaterialState Instance { get; } = new();

	private Material _activeMaterial;

	public Material ActiveMaterial
	{
		get => _activeMaterial;
		set
		{
			if ( _activeMaterial == value )
				return;

			_activeMaterial = value;
			Save();
		}
	}

	private MeshActiveMaterialState()
	{
		Load();
	}

	private void Save()
	{
		if ( _activeMaterial != null && _activeMaterial.IsValid() )
		{
			ProjectCookie.Set( "MeshTool.ActiveMaterial", _activeMaterial.ResourcePath );
		}
	}

	private void Load()
	{
		var savedPath = ProjectCookie.Get( "MeshTool.ActiveMaterial", string.Empty );

		if ( !string.IsNullOrEmpty( savedPath ) )
		{
			var material = Material.Load( savedPath );
			if ( material != null && material.IsValid() )
			{
				_activeMaterial = material;
				return;
			}
		}

		_activeMaterial = Material.Load( "materials/dev/reflectivity_30.vmat" );
	}
}
