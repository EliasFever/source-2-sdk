namespace Editor;

using System;


[Title( "Entity Tool" )]
[Icon( "playlist_add" )]
[Alias( "tools.entity-tool" )]
[Group( "Tools" )]
public partial class EntityTool : EditorTool
{
	

	public Type SelectedType
	{
		get => _selectType;
		set => _selectType = value;
	}

	[Range( 0, 360 ), Step( 1 )]
	public float YawRotation
	{
		get => _yawRotation;
		set => _yawRotation = SnapToStep( value, EditorScene.GizmoSettings.AngleSpacing );
	}
	//Temp solution, use rotation snap when changing that value
	private static float SnapToStep( float value, float step )
	{
		if ( step <= 0f ) return value;
		return MathF.Round( value / step ) * step;
	}

	[Description( "Offset distance from surface normal if 'use surface normal' is selected, otherwise uses vertical offset" )]
	public float DistanceOffset
	{
		get => _distanceOffset;
		set => _distanceOffset = value;
	}

	public bool SelectWhenPlaced
	{
		get => _selectWhenPlaced;
		set => _selectWhenPlaced = value;
	}

	[Description( "Uses surface normal for distance offset only" )]
	public bool UseSurfaceNormal
	{
		get => _useSurfaceNormal;
		set => _useSurfaceNormal = value;
	}
	private static bool _hasInitializedOnce = false;
	private static float _yawRotation = 0f;
	private static float _distanceOffset = 0f;
	public static bool _selectWhenPlaced;
	private static bool _useSurfaceNormal = false;
	private static Type _selectType = null;


	/// <summary>
	/// Remember which category was last selected
	/// </summary>
	public static string LastSelectedCategoryId { get; set; }  // TODO: NEed to utilize to save selections nicely if we switch between groupped/flat mode a lot

	/// <summary>
	/// Remember expand/collapse state for categories in flat view
	/// </summary>
	public static Dictionary<string, bool> CategoryVisibility { get; } = new();

	/// <summary>
	///  0 = Group Mode (brand new, category based, user extendable), 1 = Float (Similar to HL2/HL:A Hammer)
	/// </summary>
	private static int selectedListMode = 0;


	/// <summary>
	///  0 = list, 1 = small grid, 2 = big grid
	/// </summary>
	private static int selectedViewMode = 0;

	/// <summary>
	///  0 = list, 1 = small grid, 2 = big grid
	/// </summary>
	private static int selectedCategoryIndex = 0;



	private readonly HashSet<string> createdPages = [];




	private static readonly string basedir = "scripts/";

	public static readonly string[] icons = ["list", "grid_on", "grid_view"];
	public static readonly string[] layoutOptions = ["Group Mode", "Flat"];

	public override void OnEnabled()
	{
		base.OnEnabled();
		AllowGameObjectSelection = false;
		Selection.Clear();

		if ( !_hasInitializedOnce )
		{
			_hasInitializedOnce = true;


		}

	}
}

