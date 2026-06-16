namespace Editor.ProjectSettingPages;

internal sealed partial class ObjectPlacerCategory
{
	static IEnumerable<TypeDescription> GetComponentTypes()
	{
		return EditorTypeLibrary.GetTypes<Component>()
			.Where( x => !x.IsAbstract )
			.OrderBy( x => x.Title );
	}

	static IEnumerable<TypeDescription> GetProjectComponentTypes()
	{
		return GetComponentTypes().Where( IsProjectComponent );
	}

	static IEnumerable<TypeDescription> GetBaseComponentTypes()
	{
		return GetComponentTypes().Where( IsBaseComponent );
	}

	static bool IsProjectComponent( TypeDescription type )
	{
		if ( type is null )
			return false;

		if ( !IsBaseComponent( type ) )
			return true;

		var sourceFile = NormalizePath( type?.SourceFile );
		if ( string.IsNullOrWhiteSpace( sourceFile ) )
			return false;

		var project = Project.Current;
		if ( project is null )
			return false;

		var projectRoot = NormalizePath( project.GetRootPath() );
		var assetsPath = NormalizePath( project.GetAssetsPath() );
		var codePath = NormalizePath( project.GetCodePath() );

		if ( System.IO.Path.IsPathRooted( sourceFile ) )
		{
			return StartsWithPath( sourceFile, projectRoot )
				|| StartsWithPath( sourceFile, assetsPath )
				|| StartsWithPath( sourceFile, codePath );
		}

		return StartsWithPath( sourceFile, "code/" )
			|| StartsWithPath( sourceFile, "assets/" )
			|| StartsWithPath( sourceFile, assetsPath )
			|| StartsWithPath( sourceFile, codePath );
	}

	static bool IsBaseComponent( TypeDescription type )
	{
		var fullName = type?.TargetType?.FullName ?? type?.FullName ?? "";
		return fullName.StartsWith( "Sandbox.", StringComparison.Ordinal );
	}

	static string NormalizePath( string path )
	{
		return string.IsNullOrWhiteSpace( path )
			? null
			: path.Replace( "\\", "/" ).TrimStart( '/' );
	}

	static bool StartsWithPath( string path, string prefix )
	{
		if ( string.IsNullOrWhiteSpace( path ) || string.IsNullOrWhiteSpace( prefix ) )
			return false;

		prefix = NormalizePath( prefix ).TrimEnd( '/' ) + "/";
		return path.StartsWith( prefix, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( path.TrimEnd( '/' ), prefix.TrimEnd( '/' ), StringComparison.OrdinalIgnoreCase );
	}

	static bool IsTypeName( TypeDescription type, string typeName )
	{
		if ( type is null || string.IsNullOrWhiteSpace( typeName ) )
			return false;

		return string.Equals( GetTypeName( type ), typeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( type.FullName, typeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( type.Name, typeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( type.ClassName, typeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( type.TargetType?.FullName, typeName, StringComparison.OrdinalIgnoreCase )
			|| string.Equals( type.TargetType?.Name, typeName, StringComparison.OrdinalIgnoreCase );
	}

	static string GetTypeName( TypeDescription type )
	{
		return type?.TargetType?.FullName
			?? type?.FullName
			?? type?.Name
			?? type?.ClassName;
	}

	static TypeDescription FindComponentType( string typeName )
	{
		if ( string.IsNullOrWhiteSpace( typeName ) )
			return null;

		return GetComponentTypes().FirstOrDefault( type => IsTypeName( type, typeName ) );
	}
}
