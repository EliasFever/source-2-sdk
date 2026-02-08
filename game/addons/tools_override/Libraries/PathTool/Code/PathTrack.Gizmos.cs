namespace Core;

public partial class PathTrack
{
	private List<(Vector3 position, string label)> textCache = new();
	private bool isTextCacheDirty = true;
	private TimeSince timeSinceLastTextDraw = 0;
	private const float textDrawInterval = 0.0026f;
	private float lodCheckTimer = 0f;
	private int previousLODLevel = -1;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( !Game.IsEditor ) return;

		EditorCamera = Gizmo.Camera;

		if ( isDirty )
		{
			UpdatePath();
			InvalidateTextCache();
		}

		Gizmo.Draw.LineThickness = 2.5f;

		bool isPathSelected = Gizmo.IsSelected;

		// Draw path lines
		if ( splinePoints.Count >= 2 )
		{
			DrawPathLines( isPathSelected );
		}
	}

	private void DrawPathLines( bool isPathSelected )
	{
		Gizmo.Draw.LineThickness = 2.5f;

		bool shouldDrawText = timeSinceLastTextDraw > textDrawInterval;

		for ( int i = 0; i < splinePoints.Count - 1; i++ )
		{
			var a = splinePoints[i];
			var b = splinePoints[i + 1];
			Color lineColor = Color.White;

			if ( isPathSelected )
			{
				lineColor = Color.Yellow;

				if ( shouldDrawText )
				{
					if ( isTextCacheDirty )
						RebuildTextCache();

					foreach ( var entry in textCache )
					{
						Gizmo.Draw.Color = Color.White;
						Gizmo.Draw.Text(
							entry.label,
							new Transform( entry.position, Rotation.Identity ),
							font: "Roboto",
							size: 12f
						);
					}
				}
			}

			if ( shouldDrawText )
				timeSinceLastTextDraw = 0;

			Gizmo.Draw.Color = lineColor;
			Gizmo.Draw.Line( a, b );
		}

		if ( Game.IsEditor && splinePoints.Count >= 2 && lodCheckTimer > 0.2f )
		{
			Vector3 cameraPos = Gizmo.Camera.Position;
			float distance = splinePoints.Min( p => Vector3.DistanceBetween( p, cameraPos ) );
			int lodLevel = GetLODLevel( distance );

			if ( lodLevel != previousLODLevel )
			{
				previousLODLevel = lodLevel;
				GenerateCableMesh( cameraPos );
			}
		}
	}

	private void InvalidateTextCache()
	{
		isTextCacheDirty = true;
	}

	private void RebuildTextCache()
	{
		if ( isTextCacheDirty )
		{
			textCache.Clear();

			for ( int i = 0; i < PathPointPositions.Count; i++ )
			{
				var pos = PathPointPositions[i] + Vector3.Up * 4f;
				textCache.Add( (pos, i.ToString()) );
			}

			isTextCacheDirty = false;
		}
	}

}
