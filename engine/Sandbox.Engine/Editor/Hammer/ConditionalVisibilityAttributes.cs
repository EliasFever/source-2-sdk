
using Sandbox;

/// <summary>
/// Hide a property if a condition matches.
/// </summary>
public abstract class ConditionalVisibilityAttribute : InspectorVisibilityAttribute
{
	// TODO - we should change this to return a flag indicating that we want
	// * show / hidden
	// * enabled / disabled

	/// <summary>
	/// The test condition.
	/// </summary>
	/// <param name="targetObject">The class instance of the property this attribute is attached to.</param>
	/// <param name="td">Description of the <paramref name="targetObject"/>'s type.</param>
	/// <returns>Return true if the property should be visible.</returns>
	public abstract bool TestCondition( object targetObject, TypeDescription td );
}


/// <summary>
/// Hide this property if a given property within the same class has the given value. Used typically in the Editor Inspector.
/// </summary>
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true )]
public class HideIfAttribute : ConditionalVisibilityAttribute
{
	/// <summary>
	/// Property name to test.
	/// </summary>
	public string PropertyName { get; set; }

	/// <summary>
	/// Property value to test against.
	/// </summary>
	public object Value { get; set; }

	public HideIfAttribute( string propertyName, object value )
	{
		PropertyName = propertyName;
		Value = value;
	}

	public override bool TestCondition( object targetObject, TypeDescription td )
	{
		var property = td.GetProperty( PropertyName );
		if ( property == null ) return true;
		if ( !property.CanRead ) return true;

		var val = property.GetValue( targetObject );
		if ( val == Value ) return false;
		if ( $"{val}" == $"{Value}" ) return false;

		return true;
	}

	public override bool TestCondition( SerializedObject so )
	{
		if ( so.TryGetProperty( PropertyName, out var property ) )
		{
			var value = property.GetValue<object>( so );
			return Equals( value, Value );
		}

		Log.Warning( $"HideIfAttribute: Couldn't find property '{PropertyName}' on {so.TypeName}" );
		return true;
	}
}

/// <summary>
/// Show this property if a given property within the same class has the given value. Used typically in the Editor Inspector.
/// </summary>
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true )]
public class ShowIfAttribute : HideIfAttribute
{
	public ShowIfAttribute( string propertyName, object value ) : base( propertyName, value )
	{
	}

	public override bool TestCondition( object targetObject, TypeDescription td )
	{
		// opposite
		return !base.TestCondition( targetObject, td );
	}

	public override bool TestCondition( SerializedObject so )
	{
		// opposite
		return !base.TestCondition( so );
	}
}

public enum ShowIfPlusComparison
{
	Equal,
	NotEqual,
	GreaterOrEqual,
	LessOrEqual,
	Greater,
	Less
}

public enum ShowIfPlusLogical
{
	None,
	And,
	Or
}

/// <summary>
/// Specifies that a property, field, or method should be conditionally visible in the editor based on the values of one
/// or more other properties, using advanced comparison and logical operations.
/// </summary>
/// <remarks> Multiple conditions can be combined using logical operators such as AND or OR, and each condition supports various 
/// comparison types (e.g., equal, not equal, greater than). The attribute can be applied multiple times to the same member to define 
/// complex visibility rules.</remarks>
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true )]
public class ShowIfPlusAttribute : ConditionalVisibilityAttribute
{
	public struct Condition
	{
		public string PropertyName;
		public object CompareValue;
		public ShowIfPlusComparison Comparison;
	}

	private readonly List<Condition> _conditions;
	private readonly ShowIfPlusLogical _logical;

	public ShowIfPlusAttribute( ShowIfPlusLogical logical = ShowIfPlusLogical.None, params object[] conditions )
	{
		_conditions = new List<Condition>();
		_logical = logical;

		// Expect triplets: propertyName, value, comparison(optional)
		for ( int i = 0; i < conditions.Length; i += 2 )
		{
			string propName = conditions[i] as string;
			object value = conditions[i + 1];
			ShowIfPlusComparison comp = ShowIfPlusComparison.Equal;
			if ( i + 2 < conditions.Length && conditions[i + 2] is ShowIfPlusComparison c )
			{
				comp = c;
				i++;
			}

			_conditions.Add( new Condition { PropertyName = propName, CompareValue = value, Comparison = comp } );
		}
	}

	private bool EvaluateCondition( object val, Condition cond )
	{
		double dVal = Convert.ToDouble( val );
		double dComp = Convert.ToDouble( cond.CompareValue );

		return cond.Comparison switch
		{
			ShowIfPlusComparison.Equal => Equals( val, cond.CompareValue ),
			ShowIfPlusComparison.NotEqual => !Equals( val, cond.CompareValue ),
			ShowIfPlusComparison.GreaterOrEqual => dVal >= dComp,
			ShowIfPlusComparison.LessOrEqual => dVal <= dComp,
			ShowIfPlusComparison.Greater => dVal > dComp,
			ShowIfPlusComparison.Less => dVal < dComp,
			_ => false
		};
	}

	public override bool TestCondition( object targetObject, TypeDescription td )
	{
		if ( _conditions.Count == 0 ) return true;

		bool result = _logical switch
		{
			ShowIfPlusLogical.And => true,
			ShowIfPlusLogical.Or => false,
			ShowIfPlusLogical.None => true,
			_ => true
		};

		foreach ( var cond in _conditions )
		{
			var prop = td.GetProperty( cond.PropertyName );
			if ( prop == null || !prop.CanRead ) continue;

			var val = prop.GetValue( targetObject );
			bool condResult = EvaluateCondition( val, cond );

			result = _logical switch
			{
				ShowIfPlusLogical.And => result & condResult,
				ShowIfPlusLogical.Or => result | condResult,
				ShowIfPlusLogical.None => condResult,
				_ => result
			};

			if ( _logical == ShowIfPlusLogical.None ) break; // only first condition
		}

		return !result; // invert for editor (true=hide, false=show)
	}

	public override bool TestCondition( SerializedObject so )
	{
		if ( _conditions.Count == 0 ) return true;

		bool result = _logical switch
		{
			ShowIfPlusLogical.And => true,
			ShowIfPlusLogical.Or => false,
			ShowIfPlusLogical.None => true,
			_ => true
		};

		foreach ( var cond in _conditions )
		{
			if ( !so.TryGetProperty( cond.PropertyName, out var prop ) ) continue;

			var val = prop.GetValue<object>( so );
			bool condResult = EvaluateCondition( val, cond );

			result = _logical switch
			{
				ShowIfPlusLogical.And => result & condResult,
				ShowIfPlusLogical.Or => result | condResult,
				ShowIfPlusLogical.None => condResult,
				_ => result
			};

			if ( _logical == ShowIfPlusLogical.None ) break; // only first condition
		}

		return !result;
	}
}
