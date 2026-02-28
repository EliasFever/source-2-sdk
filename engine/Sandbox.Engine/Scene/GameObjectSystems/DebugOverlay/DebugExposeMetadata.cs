namespace Sandbox;

using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

public static class DebugExposeMetadata
{
	static readonly ConcurrentDictionary<Type, Lazy<DebugTypeInfo>> cache = new();

	public static DebugTypeInfo Get( Type type )
	{
		var lazy = cache.GetOrAdd(
			type,
			t => new Lazy<DebugTypeInfo>(
				() => Build( t ),
				LazyThreadSafetyMode.ExecutionAndPublication ) );

		return lazy.Value;
	}

	internal static Func<object, object> CompileAccessor( MemberInfo member )
	{
		if ( member is PropertyInfo prop )
		{
			var getter = prop.GetGetMethod( true );
			return getter == null
				? (_ => null)
				: CreateTypedPropertyAccessor(
				getter,
				prop.DeclaringType,
				prop.PropertyType );
		}

		return member is FieldInfo field
			? CreateTypedFieldAccessor(
				field,
				field.DeclaringType,
				field.FieldType )
			: throw new InvalidOperationException();
	}

	static Func<object, object> CreateTypedPropertyAccessor(
	MethodInfo getter,
	Type declaringType,
	Type returnType )
	{
		var helperMethod = typeof( DebugExposeMetadata )
			.GetMethod( nameof( CreatePropertyAccessorGeneric ),
				BindingFlags.NonPublic | BindingFlags.Static )!
			.MakeGenericMethod( declaringType, returnType );

		return (Func<object, object>)helperMethod.Invoke( null, [getter] )!;
	}

	static Func<object, object> CreatePropertyAccessorGeneric<TDeclaring, TReturn>( MethodInfo getter )
	{
		var openDelegate = (Func<TDeclaring, TReturn>)
			getter.CreateDelegate( typeof( Func<TDeclaring, TReturn> ) );

		return instance =>
		{
			var typed = (TDeclaring)instance;
			TReturn result = openDelegate( typed );
			return result!;
		};
	}

	static Func<object, object> CreateTypedFieldAccessor(
	FieldInfo field,
	Type declaringType,
	Type fieldType )
	{
		var helperMethod = typeof( DebugExposeMetadata )
			.GetMethod( nameof( CreateFieldAccessorGeneric ),
				BindingFlags.NonPublic | BindingFlags.Static )!
			.MakeGenericMethod( declaringType, fieldType );

		return (Func<object, object>)helperMethod.Invoke( null, [field] )!;
	}

	static Func<object, object> CreateFieldAccessorGeneric<TDeclaring, TField>(
		FieldInfo field )
	{
		return instance =>
		{
			var typed = (TDeclaring)instance;
			return (TField)field.GetValue( typed )!;
		};
	}

	internal static Func<object, object> CompileMemberPipeline( MemberInfo member, string displayMember )
	{
		var baseAccessor = CompileAccessor( member );

		if ( string.IsNullOrWhiteSpace( displayMember ) )
			return baseAccessor;

		var parts = displayMember.Split( '.' );

		return instance =>
		{
			object value = baseAccessor( instance );

			foreach ( var part in parts )
			{
				if ( value == null )
					return null;

				var type = value.GetType();

				var prop = type.GetProperty(
					part,
					BindingFlags.Public |
					BindingFlags.Instance );

				if ( prop == null )
					return null;

				value = prop.GetValue( value );
			}

			return value;
		};
	}

	static DebugTypeInfo Build( Type type )
	{
		var members = new List<DebugMemberInfo>();

		var flags = BindingFlags.Instance |
					BindingFlags.Public |
					BindingFlags.NonPublic;

		foreach ( var prop in type.GetProperties( flags ) )
		{
			var attr = prop.GetCustomAttribute<DebugExposeAttribute>();
			if ( attr != null )
				members.Add( new DebugMemberInfo( prop, attr ) );
		}

		foreach ( var field in type.GetFields( flags ) )
		{
			var attr = field.GetCustomAttribute<DebugExposeAttribute>();
			if ( attr != null )
				members.Add( new DebugMemberInfo( field, attr ) );
		}

		return new DebugTypeInfo( members );
	}
}

public class DebugTypeInfo
{
	public readonly IReadOnlyList<DebugMemberInfo> Members;

	public bool HasMembers => Members.Count > 0;

	public DebugTypeInfo( List<DebugMemberInfo> members )
	{
		Members = [.. members
			.OrderBy( m => m.Group )
			.ThenBy( m => m.Order )];
	}
}

public class DebugMemberInfo
{
	public readonly string Label;
	public readonly string Group;
	public readonly int Order;
	public readonly bool HideIfEmpty;
	public readonly string Format;

	readonly Func<object, object> pipelineAccessor;

	public DebugMemberInfo( PropertyInfo prop, DebugExposeAttribute attr )
	{
		Label = string.IsNullOrWhiteSpace( attr.Label )
			? prop.Name
			: attr.Label;

		Group = attr.Group ?? "Default";
		Order = attr.Order;
		HideIfEmpty = attr.HideIfEmpty;
		Format = attr.Format;

		pipelineAccessor =
			DebugExposeMetadata.CompileMemberPipeline(
				prop,
				attr.DisplayMember );
	}

	public DebugMemberInfo( FieldInfo field, DebugExposeAttribute attr )
	{
		Label = string.IsNullOrWhiteSpace( attr.Label )
			? field.Name
			: attr.Label;

		Group = attr.Group ?? "Default";
		Order = attr.Order;
		HideIfEmpty = attr.HideIfEmpty;
		Format = attr.Format;

		pipelineAccessor =
			DebugExposeMetadata.CompileMemberPipeline(
				field,
				attr.DisplayMember );
	}

	public object ReadValue( object instance )
	{
		if ( instance == null )
			return null;

		return pipelineAccessor?.Invoke( instance );
	}

	public string FormatValue( object value )
	{
		if ( value == null )
			return "null";

		if ( !string.IsNullOrEmpty( Format ) &&
			 value is IFormattable formattable )
		{
			return formattable.ToString(
				Format,
				System.Globalization.CultureInfo.InvariantCulture );
		}

		return value?.ToString() ?? "null";
	}
}
