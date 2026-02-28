namespace Sandbox;

public partial class DebugOverlaySystem
{
	List<Entry> entries = new();

	class Entry : IDisposable
	{
		public bool CreatedDuringFixed;
		public bool SingleFrame = true;
		public float life;
		public SceneObject sceneObject;

		public Entry( float duration, bool fixedUpdate, SceneObject so )
		{
			CreatedDuringFixed = fixedUpdate;
			sceneObject = so;

			if ( duration > 0 )
			{
				life = duration;
				SingleFrame = false;
			}
		}

		public void Dispose()
		{
			sceneObject?.Delete();
			sceneObject = default;
		}
	}

	/// <summary>
	/// Add an entry manually
	/// </summary>
	void Add( float duration, SceneObject so )
	{
		// common flags for debug overlays
		so.Flags.IncludeInCubemap = false;
		so.Tags.Add( "debugoverlay" );

		var entry = new Entry( duration, inFixedUpdate, so );
		entries.Add( entry );
	}

	/// <summary>
	/// Remove and dispose an entry manually
	/// </summary>
	void Remove( Entry entry )
	{
		if ( entry == null )
			return;

		entry.Dispose();
		entries.Remove( entry );
	}

	/// <summary>
	/// Finds the first entry whose scene object matches the given condition.
	/// </summary>
	Entry FindEntryFor( Func<SceneObject, bool> match )
	{
		return entries.FirstOrDefault( e =>
			e.sceneObject != null &&
			match( e.sceneObject )
		);
	}


	//
	// Public API
	//

	/// <summary>
	/// Try to get the first entry matching the given condition.
	/// </summary>
	public bool TryGetEntry(
		Func<SceneObject, bool> match,
		out SceneObject sceneObject )
	{
		var entry = entries.FirstOrDefault( e =>
			e.sceneObject != null &&
			match( e.sceneObject )
		);

		sceneObject = entry?.sceneObject;
		return entry != null;
	}

	/// <summary>
	/// Check if any entry matches the given condition.
	/// </summary>
	public bool Exists( Func<SceneObject, bool> match )
	{
		return entries.Any( e =>
			e.sceneObject != null &&
			match( e.sceneObject )
		);
	}

	/// <summary>
	/// Remove and dispose entries matching the given condition.
	/// </summary>
	public int RemoveWhere( Func<SceneObject, bool> match )
	{
		int removed = 0;

		for ( int i = entries.Count - 1; i >= 0; i-- )
		{
			var e = entries[i];
			if ( e.sceneObject != null && match( e.sceneObject ) )
			{
				e.Dispose();
				entries.RemoveAt( i );
				removed++;
			}
		}

		return removed;
	}

	public int RemoveDebugTextForComponent( Component comp )
	{
		return RemoveWhere( so =>
			so is DebugTextSceneObject d &&
			d.component == comp
		);
	}

	/// <summary>
	/// Remove and dispose all entries
	/// </summary>
	public void ClearAllEntries()
	{
		foreach ( Entry entry in entries )
			entry.Dispose();

		entries.Clear();
	}

}
