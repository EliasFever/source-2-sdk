namespace Editor;

using System;

public partial class EntityTool
{

	[Shortcut( "entity-tool", "Shift+E", typeof( SceneViewportWidget ) )]
	public static void ActivateTool()
	{
		EditorToolManager.SetTool( nameof( EntityTool ) );
	}


	private static T LoadSingleResource<T>( string path, bool recursive = true ) where T : Resource
	{
		try
		{
			var result = ResourceLibrary.GetAll<T>( path, recursive )?.FirstOrDefault();
			if ( !result.IsValid() )
				Log.Warning( $"No {typeof( T ).Name} resource found at '{path}'" );
			return result;
		}
		catch ( Exception e )
		{
			Log.Warning( $"Failed to load {typeof( T ).Name} resources at '{path}': {e}" );
			return null;
		}
	}

	private static List<T> LoadResourceList<T>( string path, bool recursive = true ) where T : Resource
	{
		try
		{
			var list = ResourceLibrary.GetAll<T>( path, recursive )?.ToList();
			if ( list is null || list.Count == 0 )
			{
				Log.Warning( $"No {typeof( T ).Name} resources found at '{path}'" );
				return [];
			}
			return list;
		}
		catch ( Exception e )
		{
			Log.Warning( $"Failed to load {typeof( T ).Name} resources at '{path}': {e}" );
			return [];
		}
	}

	public static void SetCategoryVisibility( string categoryId, bool visible )
	{
		if ( string.IsNullOrWhiteSpace( categoryId ) )
			return;

		CategoryVisibility[categoryId] = visible;
	}

	public static bool GetCategoryVisibility( string categoryId, bool defaultVisible = true )
	{
		if ( string.IsNullOrWhiteSpace( categoryId ) )
			return defaultVisible;

		return CategoryVisibility.TryGetValue( categoryId, out var visible )
			? visible
			: defaultVisible;
	}

	// --- Gizmo stuff ---
	private float clickPulseTimer = -1f;

	private static Color startColor = Color.Parse( "#ffee00FF" ).Value!;
	private static Color endColor = Color.Parse( "#ffee0079" ).Value!;

	private static void DrawPulsingCircle( Vector3 position, Vector3 normal, float baseRadius, int sections = 32 )
	{
		float t = (float)Time.Now * 0.75f; // speed of the pulse
		float rawPulse = 0.5f + 0.5f * MathF.Sin( t * MathF.PI * 2 );
		//float easedPulse = EasingPlus.EaseInOutSine( rawPulse );
		//float radius = baseRadius * (1.05f + 0.5f * easedPulse);

		//Color currentColor = Color.Lerp( startColor, endColor, easedPulse );
		//Gizmo.Draw.Color = currentColor;

		Rotation rotation = Rotation.LookAt( normal );
		using ( Gizmo.Scope( "pulsing_circle", new Transform( position, rotation ) ) )
		{
		//	Gizmo.Draw.LineCircle( 0, radius, sections: sections );
		}
	}

	private static void DrawClickPulse( Vector3 position, Vector3 normal, float baseRadius, ref float timer, float duration = 0.2f, int sections = 32 )
	{
		if ( timer < 0f ) return;

		timer += Time.Delta;
		float t = MathF.Min( timer / duration, 1f );

		float radiusMultiplier = 1f + 0.5f * (1f - t);

		Color color = Color.Lerp( startColor, endColor, 1f - t );

		Rotation rotation = Rotation.LookAt( normal );
		using ( Gizmo.Scope( "click_pulse", new Transform( position, rotation ) ) )
		{
			Gizmo.Draw.Color = color;
			Gizmo.Draw.LineCircle( 0, baseRadius * radiusMultiplier, sections: sections );
		}

		if ( t >= 1f )
			timer = -1f; // animation is done
	}

}
