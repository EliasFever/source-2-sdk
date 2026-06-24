using Sandbox.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sandbox;


[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Method )]
public class SaveRestoreAttribute : Attribute
{
	/// <summary>
	/// The internal type of this property.
	/// </summary>
	public Type type { get; set; }

	public SaveRestoreAttribute() : base()
	{
	}

}
