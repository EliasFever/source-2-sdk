namespace Editor;

public sealed class ObjectPlacerConfig : ConfigData
{
	public string BaseComponentTypeName { get; set; }
	public bool HideUngroupedEntries { get; set; }
	public bool HideEntriesWithUnknownGroups { get; set; }
	public List<ObjectPlacerGroup> Groups { get; set; } = [];
	public List<ObjectPlacerEntry> Entries { get; set; } = [];

	public static ObjectPlacerConfig Load()
	{
		var settings = EditorUtility.LoadProjectSettings<ObjectPlacerConfig>( "ObjectPlacer.config" );
		settings.Groups ??= [];
		settings.Entries ??= [];
		foreach ( var group in settings.Groups )
		{
			if ( string.IsNullOrWhiteSpace( group.Id ) )
				group.Id = Guid.NewGuid().ToString();
		}

		foreach ( var entry in settings.Entries )
		{
			if ( string.IsNullOrWhiteSpace( entry.Id ) )
				entry.Id = Guid.NewGuid().ToString();

			entry.PropertyOverrides ??= [];
		}

		return settings;
	}
}

public sealed class ObjectPlacerGroup
{
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string Name { get; set; } = "New Group";
	public string Icon { get; set; } = "folder";
	public string Description { get; set; }
	public bool CollapsedByDefault { get; set; }
	public bool HideInTool { get; set; }
}

public sealed class ObjectPlacerEntry
{
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string Name { get; set; } = "New Object";
	public string Icon { get; set; } = "extension";
	public string Description { get; set; }
	public string SearchText { get; set; }
	public bool HideInTool { get; set; }
	public string GroupId { get; set; }
	[EnumDropdown] public ObjectPlacerEntryKind Kind { get; set; } = ObjectPlacerEntryKind.Component;
	[EnumDropdown] public ObjectPlacerComponentSource ComponentSource { get; set; } = ObjectPlacerComponentSource.Project;
	public string ComponentTypeName { get; set; }
	public PrefabFile Prefab { get; set; }
	public List<ObjectPlacerPropertyOverride> PropertyOverrides { get; set; } = [];
}

public sealed class ObjectPlacerPropertyOverride
{
	public string PropertyName { get; set; }
	public ObjectPlacerPropertyOverrideKind Kind { get; set; } = ObjectPlacerPropertyOverrideKind.Resource;
	public string Value { get; set; }
	public string ResourcePath { get; set; }
}

public enum ObjectPlacerEntryKind
{
	[Icon( "extension" )]
	Component,

	[Icon( "dataset" )]
	Prefab
}

public enum ObjectPlacerComponentSource
{
	[Icon( "code" )]
	Project,

	[Icon( "extension" )]
	Base
}

public enum ObjectPlacerPropertyOverrideKind
{
	Resource,
	Value
}
