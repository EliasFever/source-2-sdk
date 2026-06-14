namespace Editor;


using Editor.TerrainEditor;
using Sandbox.UI;
using System;


public partial class EntityTool
{
	/// <summary>
	/// Create the sidebar widget for the entity placer tool
	/// </summary>
	public override Widget CreateToolSidebar()
	{

		var widget = new ToolSidebarWidget()
		{
			MinimumWidth = 500
		};
		
		widget.AddTitle( "Add Entities", "lightbulb" );

		
		// --- Placement Settings ---
		SerializedProperty yawRot = this?.GetSerialized().GetProperty( nameof( YawRotation ) );
		SerializedProperty useNormal = this?.GetSerialized().GetProperty( nameof( UseSurfaceNormal ) );
		SerializedProperty distOff = this?.GetSerialized().GetProperty( nameof( DistanceOffset ) );
		SerializedProperty selectPlaced = this?.GetSerialized().GetProperty( nameof( SelectWhenPlaced ) );


		var group = widget.AddGroup( "Placement settings", collapsible:true );
		group.Add( ControlSheet.CreateRow( yawRot ) );
		group.Add( ControlSheet.CreateRow( distOff ) );
		group.Add( ControlSheet.CreateRow( selectPlaced ) );
		group.Add( ControlSheet.CreateRow( useNormal ) );


		var entityGroup = widget.AddGroup("");
		

		// --- Entity Class Selector ---
		Widget classGroup = new( null ) { Layout = Layout.Row() };
		classGroup.Layout.Spacing = 10;
		classGroup.Layout.Margin = new Margin( 10, 10, 0, 0 );
		
		Label classLabel = new( "Entity Class:" )
		{
			MinimumWidth = 80,
			Alignment = TextFlag.LeftCenter
		};




		classGroup.Layout.Add( classLabel );

		entityGroup.Add( classGroup );

	
		widget.Layout.AddSpacingCell( 5 );
		widget.Layout.Margin = 4;



		return widget;
	}

	

	
}
