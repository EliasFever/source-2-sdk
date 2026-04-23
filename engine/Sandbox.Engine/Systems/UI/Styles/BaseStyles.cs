namespace Sandbox.UI;

public abstract partial class BaseStyles : ICloneable
{
	/// <summary>
	/// Called when any CSS properties are changed.
	/// </summary>
	public abstract void Dirty();

	/// <summary>
	/// Represents the <c>overflow</c> CSS property.
	/// </summary>
	public OverflowMode? Overflow
	{
		get
		{
			if ( _overflowx.HasValue && _overflowx.Value == OverflowMode.Scroll ) return OverflowMode.Scroll;
			if ( _overflowy.HasValue && _overflowy.Value == OverflowMode.Scroll ) return OverflowMode.Scroll;

			return _overflowx ?? _overflowy;
		}
		set
		{
			if ( _overflowx == value && _overflowy == value ) return;

			_overflowx = value;
			_overflowy = value;

			Dirty();
		}
	}

	/// <summary>
	/// Copy over only the styles that are set.
	/// </summary>
	public virtual void Add( BaseStyles bs )
	{
		AddGenerated( bs );

		if ( bs._backgroundImage != null ) _backgroundImage = bs._backgroundImage;
		if ( bs._maskImage != null ) _maskImage = bs._maskImage;
		if ( bs._borderImageSource != null ) _borderImageSource = bs._borderImageSource;

		if ( bs._caretwidth != null ) _caretwidth = bs._caretwidth;
		if ( bs._caretblink != null ) _caretblink = bs._caretblink;
		if ( bs._caretblinkrate != null ) _caretblinkrate = bs._caretblinkrate;
		if ( bs._backgroundPlaybackPaused.HasValue ) _backgroundPlaybackPaused = bs._backgroundPlaybackPaused;
	}

	/// <summary>
	/// Copy all styles from given style set.
	/// </summary>
	public virtual void From( BaseStyles bs )
	{
		FromGenerated( bs );

		_backgroundImage = bs._backgroundImage;
		_maskImage = bs._maskImage;
		_borderImageSource = bs._borderImageSource;

		_caretwidth = bs._caretwidth;
		_caretblink = bs._caretblink;
		_caretblinkrate = bs._caretblinkrate;
		_backgroundPlaybackPaused = bs._backgroundPlaybackPaused;
	}

	/// <summary>
	/// Copy all styles from given style set.
	/// </summary>
	public virtual bool Set( string property, string value )
	{
		if ( SetGenerated( property, value ) )
			return true;

		switch ( property )
		{
			case "caret-width":
			{
				var l = Length.Parse( value );
				if ( !l.HasValue ) return false;
				CaretWidth = l;
				return true;
			}

			case "caret-blink":
				CaretBlink = value.ToBool();
				return true;

			case "caret-blink-rate":
			{
				if ( !TryParseTimeSeconds( value, out var seconds ) )
					return false;

				CaretBlinkRate = MathF.Max( 0.05f, seconds );
				return true;
			}

			case "overflow":
				return SetOverflow( value, x => Overflow = x );
			case "overflow-x":
				return SetOverflow( value, x => OverflowX = x );
			case "overflow-y":
				return SetOverflow( value, x => OverflowY = x );
		}

		return false;
	}

	public void FillDefaults()
	{
		_overflowx ??= Overflow ?? OverflowMode.Visible;
		_overflowy ??= Overflow ?? OverflowMode.Visible;

		_caretwidth ??= 1;
		_caretblink ??= true;
		_caretblinkrate ??= 1.0f;

		FillDefaultsGenerated();
	}

	static bool TryParseTimeSeconds( string value, out float seconds )
	{
		seconds = 0;
		if ( string.IsNullOrWhiteSpace( value ) )
			return false;

		value = value.Trim();

		if ( value.EndsWith( "ms", StringComparison.OrdinalIgnoreCase ) )
		{
			var num = value[..^2].Trim();
			if ( !float.TryParse( num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ms ) )
				return false;

			seconds = ms / 1000f;
			return true;
		}

		if ( value.EndsWith( "s", StringComparison.OrdinalIgnoreCase ) )
		{
			var num = value[..^1].Trim();
			if ( !float.TryParse( num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s ) )
				return false;

			seconds = s;
			return true;
		}

		return float.TryParse( value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds );
	}


	bool SetOverflow( string value, Action<OverflowMode> set )
	{
		switch ( value )
		{
			case "hidden":
				set( OverflowMode.Hidden );
				return true;
			case "scroll":
				set( OverflowMode.Scroll );
				return true;
			case "clip":
				set( OverflowMode.Clip );
				return true;
			case "clip-whole":
				set( OverflowMode.ClipWhole );
				return true;
			case "visible":
				set( OverflowMode.Visible );
				return true;
			default:
				Log.Warning( $"Unhandled overflow property: {value}" );
				return false;
		}
	}

	/// <summary>
	/// Set Left, Right, Width and Height based on this rect. Scale can be used to scale the rect (maybe you want to use Panel.ScaleFromScreen etc)
	/// </summary>
	public void SetRect( in Rect r, float scale = 1.0f )
	{
		Top = Length.Pixels( r.Top * scale );
		Left = Length.Pixels( r.Left * scale );
		Width = Length.Pixels( r.Width * scale );
		Height = Length.Pixels( r.Height * scale );
	}


	public override int GetHashCode()
	{
		var generated_hash = GetHashCodeGenerated();

		generated_hash = HashCode.Combine( generated_hash, _backgroundImage, _borderImageSource, _maskImage, _backgroundPlaybackPaused, _caretwidth, _caretblink, _caretblinkrate );

		return generated_hash;
	}
}
