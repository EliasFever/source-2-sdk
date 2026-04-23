using System;

namespace Sandbox.UI;

public abstract partial class BaseStyles
{
	internal Length? _caretwidth;

	public Length? CaretWidth
	{
		get => _caretwidth;
		set
		{
			if ( _caretwidth == value ) return;
			_caretwidth = value;
			Dirty();
		}
	}

	internal bool? _caretblink;

	public bool? CaretBlink
	{
		get => _caretblink;
		set
		{
			if ( _caretblink == value ) return;
			_caretblink = value;
			Dirty();
		}
	}

	internal float? _caretblinkrate;

	/// <summary>
	/// Blink period in seconds.
	/// </summary>
	public float? CaretBlinkRate
	{
		get => _caretblinkrate;
		set
		{
			if ( _caretblinkrate == value ) return;
			_caretblinkrate = value;
			Dirty();
		}
	}
}

