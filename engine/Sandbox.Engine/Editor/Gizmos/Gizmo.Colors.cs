using Microsoft.Win32;
using System.Collections.Immutable;

namespace Sandbox;

public static partial class Gizmo
{
	/// <summary>
	/// Using pure primary colors is horrible. Lets make it easier to avoid.
	/// </summary>
	public static class Colors
	{
		public static Color Red { get; } = "#c81e1e";
		public static Color Forward => Red;
		public static Color Pitch => Red;

		public static Color Green { get; } = "#32c81e";
		public static Color Left => Green;
		public static Color Yaw => Green;

		public static Color Blue { get; } = "#1e6ec8";
		public static Color Up => Blue;
		public static Color Roll => Blue;

		public static Color Selected { get; } = "#fbfbfb";
		public static Color Hovered { get; } = "#90f1ef";
		public static Color Active { get; } = "#ffe600";

		public static class Local
		{
			public static Color Red { get; } = "#cd6666";
			public static Color Forward => Red;
			public static Color Pitch => Red;

			public static Color Green { get; } = "#54cd66";
			public static Color Left => Green;
			public static Color Yaw => Green;

			public static Color Purple { get; } = "#661ecd";
			public static Color Up => Purple;
			public static Color Roll => Purple;
		}
	}
}
