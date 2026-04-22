namespace Sandbox;

/// <summary>
/// When added to a string property, the editor will show a dropdown of types deriving from <see cref="BaseType"/>,
/// and store the selected type's full name into the string.
/// </summary>
[AttributeUsage( AttributeTargets.Property )]
public sealed class PanelTypeDropdownAttribute : System.Attribute
{
	public Type BaseType { get; }

	public PanelTypeDropdownAttribute( Type baseType )
	{
		BaseType = baseType;
	}
}

