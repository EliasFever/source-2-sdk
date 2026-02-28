using Sandbox.Internal;

namespace Sandbox;

[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
public sealed class DebugExposeAttribute : Attribute
{
	public string Label { get; }
	public string Group { get; }
	public int Order { get; }
	public bool HideIfEmpty { get; }

	/// <summary>
	/// Nested member path, e.g. "ResourcePath" or "Model.ResourcePath"
	/// </summary>
	public string DisplayMember { get; set; }

	/// <summary>
	/// String formatting, e.g. "0.00"
	/// </summary>
	public string Format { get; set; }

	public string Condition { get; set; }

	public DebugExposeAttribute(
		string label = null,
		string group = null,
		int order = 0,
		bool hideIfEmpty = false )
	{
		Label = label;
		Group = group ?? "Default";
		Order = order;
		HideIfEmpty = hideIfEmpty;
	}
}
