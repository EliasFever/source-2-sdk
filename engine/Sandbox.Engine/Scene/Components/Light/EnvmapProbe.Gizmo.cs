namespace Sandbox;

public partial class EnvmapProbe
{
	protected override void DrawGizmos()
	{
		Gizmo.Hitbox.Sphere( new Sphere( 0, 10 ) );

		bool hovered = Gizmo.IsHovered;
		bool selected = Gizmo.IsSelected;

		var mdl = Gizmo.Draw.Model( Model.Sphere );
		mdl.Transform = mdl.Transform.WithScale( (hovered && !selected) ? 0.3f : 0.25f );
		mdl.SetMaterialOverride( Material.Load( "materials/dev/dev_metal_rough00.vmat" ) );

		// TODO - We could probably do this with a sprite. And we could force the current envmap texture onto it.

		if ( Gizmo.IsSelected )
		{
			Gizmo.Draw.Color = TintColor.WithAlpha( 1 );
			Gizmo.Draw.LineBBox( Bounds );

			Gizmo.Draw.Color = Color.Cyan.WithAlpha( 0.1f );
			Gizmo.Draw.SolidBox( Bounds );

			Gizmo.Draw.Color = TintColor.WithAlpha( 0.2f );
			Gizmo.Draw.LineBBox( Bounds.Grow( Feathering ) );
		}
	}
}
