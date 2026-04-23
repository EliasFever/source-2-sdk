using Sandbox.Engine;
using Sandbox.Engine.Settings;
using Sandbox.Modals;
using Sandbox.Services;
using System;
using System.Net;

namespace Sandbox;


public static partial class MenuUtility
{
	public static Action Tick { get; set; }

	public static void SetModalSystem( IModalSystem system )
	{
		IModalSystem.Current = system;
	}

	public static void AddLogger( Action<LogEvent> logger )
	{
		Sandbox.Diagnostics.Logging.OnMessage += logger;
	}

	public static void RemoveLogger( Action<LogEvent> logger )
	{
		Sandbox.Diagnostics.Logging.OnMessage -= logger;
	}

	public static ConCmdAttribute.AutoCompleteResult[] AutoComplete( string text, int maxCount )
	{
		return ConVarSystem.GetAutoComplete( text, maxCount );
	}

	public static SceneWorld CreateSceneWorld()
	{
		return new SceneWorld { IsTransient = false };
	}

#nullable enable
	/// <summary>
	/// Open an 'open file' dialog
	/// </summary>
	public static string? OpenFileDialog()
	{
		var r = NativeEngine.WindowsGlue.FindFile();
		if ( string.IsNullOrEmpty( r ) ) return null;
		return r;
	}
#nullable disable

	/// <summary>
	/// Open a folder 
	/// </summary>
	public static void OpenFolder( string path )
	{
		System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo()
		{
			FileName = path,
			UseShellExecute = true,
			Verb = "open"
		} );
	}

	/// <summary>
	/// Open a url
	/// </summary>
	public static void OpenUrl( string path )
	{
		System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo()
		{
			FileName = path,
			UseShellExecute = true,
			Verb = "open"
		} );
	}

	static List<Friend> _friendList;

	/// <summary>
	/// Get all friends.
	/// </summary>
	public static IEnumerable<Friend> Friends
	{
		get
		{
			//
			// querying this once should be enough, unless they add a new friend or something
			//
			if ( _friendList is null )
			{
				_friendList = Steamworks.SteamFriends.GetFriends().Select( x => new Friend( x ) ).ToList();
			}

			return _friendList;
		}
	}


	/// <summary>
	/// Number of seconds escape has been held down
	/// </summary>
	public static float EscapeTime => InputRouter.EscapeTime;

	/// <summary>
	/// Join the game a friend is in
	/// </summary>
	public static void JoinFriendGame( Friend friend )
	{
		var connectString = friend.GetRichPresence( "connect" );
		if ( string.IsNullOrWhiteSpace( connectString ) ) return;

		connectString = connectString.Replace( "+connect", "" );
		connectString = connectString.Replace( " ", "" );

		// Should be left with a Steam Id but otherwise try connecting by IP.
		if ( ulong.TryParse( connectString, out ulong lobbySteamId ) )
		{
			ConsoleSystem.Run( $"connect {lobbySteamId}" );
		}
		else
		{
			var ipAddress = IPEndPoint.Parse( connectString );
			ConsoleSystem.Run( $"connect {ipAddress}" );
		}
	}

	/// <summary>
	/// This is called when the cancel button is pressed when loading. 
	/// We should disconnect and leave the game.
	/// </summary>
	public static void CancelLoading()
	{
		// Close the game
		//	CloseGame();

		// Close the loading screen
		LoadingScreen.IsVisible = false;
	}

	/// <summary>
	/// Set a console variable. Unlike ConsoleSystem.*, this is unprotected and allows any console variable to be changed.
	/// </summary>
	public static void SetConsoleVariable( string name, object value )
	{
		ConVarSystem.SetValue( name, value?.ToString(), true );
	}
}
