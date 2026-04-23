namespace Sandbox;

public class SceneLoadOptions
{
	SceneFile scene;

	/// <summary>
	/// Internal property to mark this scene as being a system scene. It should only be set in
	/// <see cref="Scene.AddSystemScene"/>.
	/// </summary>
	internal bool IsSystemScene { get; set; }

	public bool ShowLoadingScreen { get; set; } = true;
	public bool IsAdditive { get; set; } = false;

	/// <summary>
	/// Minimum duration (in seconds) to keep the loading screen visible for this scene change.
	/// By default, set to a negative value to use <see cref="LoadingScreen.DefaultMinimumVisibleSeconds"/>.
	/// Only applies when <see cref="ShowLoadingScreen"/> is true.
	/// </summary>
	public float MinimumLoadingScreenSeconds { get; set; } = -1.0f;

	/// <summary>
	/// If true, once the scene has finished loading we will keep the loading screen visible
	/// until the player provides input to continue.
	/// Only applies when <see cref="ShowLoadingScreen"/> is true.
	/// </summary>
	public bool RequireInputToContinue { get; set; } = false;

	/// <summary>
	/// Optional input action name used when <see cref="RequireInputToContinue"/> is true.
	/// If null or whitespace, any key or input action press will continue.
	/// </summary>
	public string ContinueInputAction { get; set; }

	/// <summary>
	/// Optional override for requiring input to continue. If null, defaults will be used.
	/// This allows project-wide defaults to be enabled while selectively disabling them for a specific load.
	/// </summary>
	public bool? RequireInputToContinueOverride { get; set; }

	/// <summary>
	/// Optional override for which input action continues a gated load. If null, defaults will be used.
	/// </summary>
	public string ContinueInputActionOverride { get; set; }

	/// <summary>
	/// If true, on load we'll even delete objects that are marked as DontDelete
	/// </summary>
	public bool DeleteEverything { get; set; } = false;

	/// <summary>
	/// Optional fully qualified panel type name to use for the loading overlay visuals
	/// during this scene load. If null/empty, project defaults (per context) will be used.
	/// </summary>
	public string LoadingOverlayPanelTypeNameOverride { get; set; }
	public Transform Offset { get; set; } = Transform.Zero;

	public SceneFile GetSceneFile() => scene;

	public bool SetScene( SceneFile sceneFile )
	{
		scene = sceneFile;
		return true;
	}

	public bool SetScene( string sceneFileName )
	{
		var file = ResourceLibrary.Get<SceneFile>( sceneFileName );
		if ( file is null )
		{
			Log.Warning( $"LoadFromFile: Couldn't find {sceneFileName}" );
			return false;
		}

		SetScene( file );
		return true;
	}
}
