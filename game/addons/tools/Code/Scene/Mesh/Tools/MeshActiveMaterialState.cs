namespace Editor.MeshEditor;

/// <summary>
/// Shared active material state for mesh tools/dock.
/// </summary>
public sealed class MeshActiveMaterialState
{
	public static MeshActiveMaterialState Instance { get; } = new();
	private const string ActiveMaterialCookieKey = "MeshTool.ActiveMaterial";
	private const string DefaultMaterialPath = "materials/dev/reflectivity_30.vmat";

	private Material _activeMaterial;
	private string _savedPath;
	private bool _initialized;

	public Material ActiveMaterial
	{
		get
		{
			EnsureInitialized();
			return _activeMaterial;
		}
		set
		{
			EnsureInitialized();

			if ( _activeMaterial == value )
				return;

			_activeMaterial = value;
			_savedPath = (value != null && value.IsValid()) ? value.ResourcePath : string.Empty;
			Save();
		}
	}

	private MeshActiveMaterialState()
	{
		// Intentionally empty: loading materials here can be too early in editor startup
		// and may cache an error/checker material globally.
	}

	private void Save()
	{
		ProjectCookie.Set( ActiveMaterialCookieKey, _savedPath ?? string.Empty );
	}

	private void EnsureInitialized()
	{
		if ( _initialized )
			return;

		_savedPath = ProjectCookie.Get( ActiveMaterialCookieKey, string.Empty );
		_initialized = true;

		if ( !string.IsNullOrEmpty( _savedPath ) )
		{
			var material = Material.Load( _savedPath );
			if ( material != null && material.IsValid() )
			{
				_activeMaterial = material;
				return;
			}
		}

		_activeMaterial = Material.Load( DefaultMaterialPath );
	}
}
