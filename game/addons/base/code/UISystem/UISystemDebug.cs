using Sandbox;
using Sandbox.Internal;
using System.Reflection;
using System.Linq;

public static class UISystemDebug
{
	[ConVar( "base_uiexists", Help = "1 if IUISystem.Current is non-null, else 0 (refreshed by base_uidump)." )]
	public static int UIExists { get; set; }

	[ConCmd( "base_uidump", Help = "Print basic IUISystem + root panel status." )]
	public static void Dump()
	{
		var current = GetIUISystemCurrent();
		UIExists = current is null ? 0 : 1;
		Log.Info( $"base_uiexists = {UIExists}" );
		Log.Info( $"IUISystem.Current = {(current is null ? "<null>" : current.GetType().FullName)}" );
		if ( current is not null )
		{
			Log.Info( $" - asm = {current.GetType().Assembly.GetName().Name}" );
		}

		try
		{
			var ipanelType = typeof( Sandbox.Internal.IPanel );
			var field = ipanelType.GetField( "InspectablePanels", BindingFlags.NonPublic | BindingFlags.Static );
			var rootsObj = field?.GetValue( null ) as System.Collections.IEnumerable;

			if ( rootsObj is null )
			{
				Log.Info( "Inspectable root panels = <unavailable>" );
				return;
			}

			var roots = rootsObj.Cast<object>().OfType<Sandbox.Internal.IPanel>().ToArray();
			Log.Info( $"Inspectable root panels = {roots.Length}" );

			foreach ( var p in roots.Take( 10 ) )
				Log.Info( $" - <{p.ElementName}> id='{p.Id}' classes='{p.Classes}'" );
		}
		catch ( System.Exception e )
		{
			Log.Warning( e, "Failed to dump root panels" );
		}
	}

	[ConCmd( "base_uistatus", Help = "Print base MenuSystem/UI system status." )]
	public static void Status()
	{
		var current = GetIUISystemCurrent();
		var iuiType = typeof( Sandbox.Internal.IUISystem );

		Log.Info( $"IUISystem.Current = {(current is null ? "<null>" : current.GetType().FullName)}" );
		if ( current is not null )
		{
			Log.Info( $"IUISystem.Current asm = {current.GetType().Assembly.FullName}" );
			var inst = current.GetType().GetField( "Instance", BindingFlags.Public | BindingFlags.Static )?.GetValue( null );
			Log.Info( $"IUISystem.Current.Instance = {(inst is null ? "<null>" : inst.ToString())}" );
		}
		Log.Info( $"global::UISystem.Instance = {(global::UISystem.Instance is null ? "<null>" : global::UISystem.Instance.GetType().FullName)}" );
		Log.Info( $"IUISystem asm = {iuiType.Assembly.FullName}" );

		var engineAsms = AppDomain.CurrentDomain.GetAssemblies().Where( a => a.GetName().Name == "Sandbox.Engine" ).ToArray();
		Log.Info( $"Sandbox.Engine loaded count = {engineAsms.Length}" );
		foreach ( var a in engineAsms )
		{
			Log.Info( $" - {a.FullName}" );
		}

		var tl = Sandbox.Internal.GlobalGameNamespace.TypeLibrary;

		try
		{
			var td = tl?.GetType( typeof( Sandbox.Internal.IUISystem ), "UISystem", preferAddonAssembly: true, exactFullName: false );
			Log.Info( $"TypeLibrary.GetType(IUISystem, \"UISystem\") = {(td?.IsValid ?? false ? td.FullName : "<missing>")}" );
		}
		catch ( System.Exception e )
		{
			Log.Warning( e, "TypeLibrary.GetType failed" );
		}

		try
		{
			var created = tl?.Create( "UISystem", typeof( Sandbox.Internal.IUISystem ), Array.Empty<object>() );
			Log.Info( $"TypeLibrary.Create(\"UISystem\", IUISystem) = {(created is null ? "<null>" : created.GetType().FullName)}" );
		}
		catch ( System.Exception e )
		{
			Log.Warning( e, "TypeLibrary.Create failed" );
		}
	}

	static object GetIUISystemCurrent()
	{
		var iuiType = typeof( Sandbox.Internal.IUISystem );
		var prop = iuiType.GetProperty( "Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static );
		return prop?.GetValue( null );
	}

	[ConCmd( "base_styles" )]
	public static void Styles()
	{
		Log.Info( $"StyleSheet.Loaded = {Sandbox.UI.StyleSheet.Loaded.Count}" );
		foreach ( var sheet in Sandbox.UI.StyleSheet.Loaded.Take( 25 ) )
		{
			Log.Info( $" - {sheet.FileName}" );
		}

		DumpType( typeof( Sandbox.UI.Dev.DeveloperMode ) );
		DumpType( typeof( Sandbox.UI.Dev.DevLayer ) );
		DumpType( typeof( Sandbox.UI.Overlays.LoadingOverlay ) );
	}

	static void DumpType( Type t )
	{
		var typeDesc = Game.TypeLibrary?.GetType( t );
		Log.Info( $"TypeLibrary.GetType({t.FullName}) = {(typeDesc is null ? "<missing>" : typeDesc.Name)}" );

		var loc = typeDesc?.GetAttributes<Sandbox.Internal.ClassFileLocationAttribute>()?.MinBy( x => x.Path.Length );
		Log.Info( $" - ClassFileLocation = {(loc is null ? "<missing>" : loc.Path)}" );
	}
}
