
using System.Text.Json.Serialization;

namespace Editor.PathEditor;

/// <summary>
/// A path element can be a path point, spline, cable mesh, generated geo
/// </summary>
public interface IPathElement : IValid
{
	PathTrack Component { get; }
	GameObject GameObject => Component.IsValid() ? Component.GameObject : null;
	Scene Scene => Component.IsValid() ? Component.Scene : null;
}
