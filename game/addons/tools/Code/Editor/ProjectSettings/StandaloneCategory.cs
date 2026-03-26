namespace Editor.ProjectSettingPages;

[Title( "Game Exporting" ), Icon( "publish" )]
internal sealed class StandaloneCategory : ProjectSettingsWindow.Category
{

	public override void OnInit( Project project )
	{
		//
		// Standalone games
		//
		if ( project.Config.Type == "game" )
		{
			var so = this.GetSerialized();

			ListenForChanges( so );
		}
	}
}
