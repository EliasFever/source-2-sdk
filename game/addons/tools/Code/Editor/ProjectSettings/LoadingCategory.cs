namespace Editor.ProjectSettingPages;

[Title( "Loading" ), Icon( "hourglass_empty" )]
internal sealed class LoadingCategory : ProjectSettingsWindow.Category
{
	LoadingSettings settings;

	public override void OnInit( Project project )
	{
		base.OnInit( project );

		settings = EditorUtility.LoadProjectSettings<LoadingSettings>( "Loading.config" );

		BodyLayout.Add( new InformationBox( """
		Configure loading screen behavior per context for your project. 

		Scene transitions can also be overridden per load via SceneLoadOptions - minimum seconds, require input, continue action, overlay panel type. 

		By default, the initial native splash to first scene load does not use the UI loading overlay (optional in Startup settings).
		"""
		 ) );

		var so = EditorTypeLibrary.GetSerializedObject( settings );
		BodyLayout.Add( ControlSheet.Create( so ) );

		ListenForChanges( so );
	}

	public override void OnSave()
	{
		EditorUtility.SaveProjectSettings( settings, "Loading.config" );
		base.OnSave();
	}
}
