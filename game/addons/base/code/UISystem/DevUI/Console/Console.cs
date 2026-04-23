namespace Sandbox.UI.Dev;

using Sandbox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[Library( "console" )]
public class Console : Panel
{
	struct AutoCompleteItem
	{
		public string Command;
		public string Description;
	}

	static List<AutoCompleteItem> AutoCompleteCache;
	static bool TriedBuildAutoCompleteCache;

	static string GetStringMember( object obj, string name )
	{
		if ( obj is null ) return null;

		var t = obj.GetType();

		var prop = t.GetProperty( name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
		if ( prop?.PropertyType == typeof( string ) )
			return prop.GetValue( obj ) as string;

		var field = t.GetField( name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
		if ( field?.FieldType == typeof( string ) )
			return field.GetValue( obj ) as string;

		return null;
	}

	static IReadOnlyList<AutoCompleteItem> GetAutoCompleteCache()
	{
		if ( TriedBuildAutoCompleteCache )
			return AutoCompleteCache ?? (IReadOnlyList<AutoCompleteItem>)Array.Empty<AutoCompleteItem>();

		TriedBuildAutoCompleteCache = true;
		AutoCompleteCache = [];

		try
		{
			var convarSystemType = typeof( ConsoleSystem ).Assembly.GetType( "Sandbox.ConVarSystem", throwOnError: false, ignoreCase: false );
			if ( convarSystemType is null )
				return AutoCompleteCache;

			var seen = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
			foreach ( var field in convarSystemType.GetFields( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic ) )
			{
				object value = null;
				try
				{
					value = field.GetValue( null );
				}
				catch
				{
					continue;
				}

				if ( value is not IDictionary dict )
					continue;

				foreach ( DictionaryEntry de in dict )
				{
					if ( de.Key is not string key || string.IsNullOrWhiteSpace( key ) )
						continue;

					if ( !seen.Add( key ) )
						continue;

					var desc = GetStringMember( de.Value, "Description" )
						?? GetStringMember( de.Value, "Help" )
						?? GetStringMember( de.Value, "HelpText" )
						?? GetStringMember( de.Value, "DefaultHelp" );

					AutoCompleteCache.Add( new AutoCompleteItem
					{
						Command = key,
						Description = desc
					} );
				}
			}
		}
		catch
		{
			// Reflection probing - best effort only.
		}

		return AutoCompleteCache;
	}

	internal List<LogEvent> Entries = [];
	internal DevScrollView OutputView;
	internal DevVirtualList Output;
	internal TextEntry Input;
//	internal TextEntry Filter; // kept for later, hidden for now

	LogEventPanel logEventPanel;
	Panel InputBar;

	struct MessageCategory
	{
		public Button Button;
		public int Count;
		public bool Disabled;

		public void Toggle()
		{
			Disabled = !Disabled;
			if ( Button.IsValid() )
				Button.SetClass( "disabled", Disabled );
		}

		public void Clear()
		{
			Count = 0;

			if ( Button.IsValid() )
				Button.Text = "0";
		}
	}

	MessageCategory Message;
	MessageCategory Warning;
	MessageCategory Error;

	public int MessageCount => Message.Count;
	public int WarningCount => Warning.Count;
	public int ErrorCount => Error.Count;

	public bool MessagesDisabled => Message.Disabled;
	public bool WarningsDisabled => Warning.Disabled;
	public bool ErrorsDisabled => Error.Disabled;

	public void ToggleMessages() { Message.Toggle(); RebuildVisible(); }
	public void ToggleWarnings() { Warning.Toggle(); RebuildVisible(); }
	public void ToggleErrors() { Error.Toggle(); RebuildVisible(); }

	public Console()
	{
		CanDragScroll = false;

		OutputView = AddChild<DevScrollView>();
		OutputView.AddClass( "console_output" );

		Output = OutputView.View.AddChild<DevVirtualList>();
		Output.Style.Position = PositionMode.Absolute;
		Output.Style.Left = 5;
		Output.Style.Top = 0;
		Output.Style.Right = 5;
		Output.Style.Bottom = 15;
		Output.Style.Dirty();
		Output.CreateCell = CreateOutputCell;
		Output.BindCell = BindOutputCell;

		Output.ItemHeight = 15;

		Output.PaddingLeft = 0;
		Output.PaddingRight = 0;
		Output.PaddingBottom = 0;
		Output.PaddingTop = 0;

		OutputView.OnScroll = OnOutputScrolled;

		logEventPanel = AddChild<LogEventPanel>();

		InputBar = Add.Panel( "inputbar" );
		InputBar.AddEventListener( "onmousedown", OnInputBarMouseDown );

		Input = InputBar.AddChild<TextEntry>();		
		Input.HistoryCookie = "console-input-history";
		Input.AddClass( "input" );
		Input.AutoComplete = FillAutoComplete;
		Input.AddEventListener( "onsubmit", OnSubmit );

		// Filtering is currently disabled
		// Filter = InputBar.AddChild<TextEntry>();
		// Filter.AddClass( "filter" );
		// Filter.Placeholder = "Filter..";
		// Filter.AddEventListener( "onchange", OnFilter );

		MenuUtility.AddLogger( OnConsoleMessage );

		OutputView.AcceptsFocus = true;
		OutputView.AllowChildSelection = true;
	}

	void OnInputBarMouseDown( PanelEvent e )
	{
		if ( e is not MousePanelEvent me )
			return;

		if ( me.Button != "mouseleft" )
			return;

		// Only handle clicks on the empty area of the input bar, let the TextEntry handle direct clicks.
		if ( me.Target != InputBar )
			return;

		Input?.Focus();

		// Clicking outside the rendered text should place the caret at the end.
		Input?.CaretPosition = Input.TextLength;

		e.StopPropagation();
	}

	Panel CreateOutputCell() => new Panel();

	void BindOutputCell( Panel cell, object data )
	{
		var row = cell.ChildrenOfType<ConsoleRow>().FirstOrDefault();
		if ( row is null )
		{
			row = new ConsoleRow();
			row.Parent = cell;
			row.OnEntryClicked = logEventPanel.Switch;
		}

		row.SetLogEvent( (LogEvent)data );
	}

	void OnOutputScrolled( Vector2 off ) => Output.VirtualScrollOffset = off;

	public override void OnDeleted()
	{
		base.OnDeleted();

		MenuUtility.RemoveLogger( OnConsoleMessage );
	}

	private void OnConsoleMessage( LogEvent e )
	{
		if ( e.Message.Contains( '\n' ) || e.Message.Contains( '\r' ) )
		{
			var parts = e.Message.Split( new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries );
			foreach ( var part in parts )
			{
				var ee = e;
				ee.Message = part;

				AddEvent( ee );
			}
		}
		else
		{
			AddEvent( e );
		}
	}

	void AddEvent( LogEvent e )
	{
		Entries.Add( e );

		if ( ShouldShowEvent( e ) )
		{
			Output.AddItem( e );
		}

		UpdateScrollSizes();

		if ( e.Level == LogLevel.Info || e.Level == LogLevel.Trace )
		{
			Message.Count++;
			if ( Message.Button.IsValid() )
			{
				Message.Button.Text = $"{Message.Count:n0}";
			}
		}

		if ( e.Level == LogLevel.Warn )
		{
			Warning.Count++;
			if ( Warning.Button.IsValid() )
			{
				Warning.Button.Text = $"{Warning.Count:n0}";
			}
		}

		if ( e.Level == LogLevel.Error )
		{
			Error.Count++;
			if ( Error.Button.IsValid() )
			{
				Error.Button.Text = $"{Error.Count:n0}";
			}
		}
	}

	internal void CreateLevelToggles( Panel parent )
	{
		if ( parent is null )
			return;

		// These buttons live in the DevWindow tab strip. If the tab strip is rebuilt (i.e hotload),
		// the old button panels can be deleted while our references remain. Recreate when needed.
		var needsRecreate =
			!Error.Button.IsValid() || Error.Button.Parent != parent ||
			!Warning.Button.IsValid() || Warning.Button.Parent != parent ||
			!Message.Button.IsValid() || Message.Button.Parent != parent;

		if ( !needsRecreate )
			return;

		Error.Button?.Delete( true );
		Warning.Button?.Delete( true );
		Message.Button?.Delete( true );

		Error.Button = null;
		Warning.Button = null;
		Message.Button = null;

		Error.Button = parent.AddChild( new Button( $"{Error.Count:n0}", null, "type err", null ) );
		Error.Button.AddEventListener( "onclick", () => ToggleErrors() );
		Error.Button.SetClass( "disabled", Error.Disabled );

		Warning.Button = parent.AddChild( new Button( $"{Warning.Count:n0}", null, "type wrn", null ) );
		Warning.Button.AddEventListener( "onclick", () => ToggleWarnings() );
		Warning.Button.SetClass( "disabled", Warning.Disabled );

		Message.Button = parent.AddChild( new Button( $"{Message.Count:n0}", null, "type msg", null ) );
		Message.Button.AddEventListener( "onclick", () => ToggleMessages() );
		Message.Button.SetClass( "disabled", Message.Disabled );
	}

	void OnFilter()
	{
		Output.SetItems( Entries.Where( x => ShouldShowEvent( x ) ).Select( x => x as object ) );
		UpdateScrollSizes();
	}

	void RebuildVisible()
	{
		OnFilter();
	}

	bool ShouldShowEvent( LogEvent e )
	{
		if ( e.Level == LogLevel.Error && Error.Disabled ) return false;
		if ( e.Level == LogLevel.Warn && Warning.Disabled ) return false;
		if ( e.Level == LogLevel.Info && Message.Disabled ) return false;
		if ( e.Level == LogLevel.Trace && Message.Disabled ) return false;

		// if ( Filter is null || string.IsNullOrWhiteSpace( Filter.Text ) )
		// 	return true;

		return true;

		// return e.Message.Contains( Filter.Text, StringComparison.OrdinalIgnoreCase );
	}

	public override void Tick()
	{
		base.Tick();
		UpdateScrollSizes();
	}

	void OnClear()
	{
		Output.Clear();
		Entries.Clear();

		Message.Clear();
		Warning.Clear();
		Error.Clear();
	}

	void OpenLogsFolder()
	{
		// John: Depreacting this for now, since I thought it's not very useful.
		// Can return it back if needed.
		// MenuUtility.OpenFolder( Environment.CurrentDirectory + "/logs/" );
	}

	void OutputLine( string line )
	{
		var e = new LogEvent() { Message = line, Level = LogLevel.Info, Logger = "in", Time = DateTime.Now };
		Entries.Add( e );
		Output.AddItem( e );
		UpdateScrollSizes();

		// Don't throw exceptions through UI event processing for bad/unknown commands.
		DevConsoleAccess.Run( line, allowProtected: true );
	}

	void OnSubmit()
	{
		var t = Input.Text;
		if ( string.IsNullOrWhiteSpace( t ) )
		{
			Input.Text = "";
			return;
		}

		if ( t == "clear" )
		{
			OnClear();
		}
		else
		{
			if ( t.Contains( '\n' ) || t.Contains( '\r' ) )
			{
				var parts = t.Split( new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries );
				foreach ( var part in parts )
				{
					OutputLine( part );
				}
			}
			else
			{
				OutputLine( t );
			}
		}

		Output.TryScrollToBottom();
		// Virtual list updates its own virtual scroll offset, keep the scroll view in sync explicitly.
		OutputView.SetScrollOffset( Output.VirtualScrollOffset );

		Input.Text = "";
		Input.AddToHistory( t );
		Input.DestroyAutoComplete();
		Input.Focus();
	}

	private object[] FillAutoComplete( string arg )
	{
		if ( string.IsNullOrWhiteSpace( arg ) )
			return Array.Empty<string>();

		var trimmed = arg.TrimEnd();
		var lastWhitespace = trimmed.LastIndexOfAny( [' ', '\t'] );
		var head = lastWhitespace >= 0 ? trimmed[..(lastWhitespace + 1)] : string.Empty;
		var token = lastWhitespace >= 0 ? trimmed[(lastWhitespace + 1)..] : trimmed;

		if ( string.IsNullOrWhiteSpace( token ) || token.Length < 2 )
			return Array.Empty<string>();

		// Prefer engine/menu-provided ranking first (usually prefix matches).
		var menuResults = MenuUtility.AutoComplete( token, 20 )
			.Select( x => (command: x.Command, description: x.Description) )
			.ToList();

		var results = new List<(string command, string description)>( menuResults.Count + 20 );
		var seen = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

		foreach ( var r in menuResults )
		{
			if ( string.IsNullOrWhiteSpace( r.command ) ) continue;
			if ( !seen.Add( r.command ) ) continue;
			results.Add( r );
		}

		// Add substring matches from the cached list to support queries like "cheats" / "sv_cheats".
		var cache = GetAutoCompleteCache();
		if ( cache.Count > 0 && token.Length >= 2 )
		{
			var matches = new List<(AutoCompleteItem item, int group, int index)>();
			foreach ( var x in cache )
			{
				if ( string.IsNullOrWhiteSpace( x.Command ) )
					continue;

				var idx = x.Command.IndexOf( token, StringComparison.OrdinalIgnoreCase );
				if ( idx < 0 )
					continue;

				var group = idx == 0 ? 0 : 1;
				matches.Add( (x, group, idx) );
			}

			foreach ( var m in matches
				.OrderBy( x => x.group )
				.ThenBy( x => x.index )
				.ThenBy( x => x.item.Command.Length )
				.Take( 40 ) )
			{
				if ( !seen.Add( m.item.Command ) ) continue;
				results.Add( (m.item.Command, m.item.Description) );
			}
		}

		return results
			.Take( 20 )
			.Select( x => (object)new TextEntry.AutocompleteEntry
			{
				Title = $"{x.command} - {x.description}".Trim( '-', ' ' ),
				Value = $"{head}{x.command}"
			} )
			.ToArray();
	}

	void UpdateScrollSizes()
	{
		if ( OutputView is null || Output is null )
			return;

		// Estimate horizontal content width so long lines can scroll even when virtualized.
		var maxChars = 0;
		foreach ( var e in Entries )
		{
			if ( !ShouldShowEvent( e ) ) continue;

			var logger = e.Logger;
			if ( !string.IsNullOrWhiteSpace( logger ) && logger != "Generic" && logger != "in" )
			{
				maxChars = Math.Max( maxChars, (logger.Length + 3) + (e.Message?.Length ?? 0) ); // [x] + space
			}
			else
			{
				maxChars = Math.Max( maxChars, e.Message?.Length ?? 0 );
			}
		}

		var fontSize = ConsoleRow.ConsoleMsgFontSize;
		var estimatedWidth = (maxChars * (fontSize * 0.6f)) + 64f;
		Output.ContentWidthHint = MathF.Max( Output.Box.Rect.Width, estimatedWidth );

		var visibleCount = Entries.Count( ShouldShowEvent );
		var view = Output.Box.Rect.Size;
		OutputView.ContentSize = new Vector2(
			MathF.Max( view.x, Output.ContentWidthHint ),
			MathF.Max( view.y, visibleCount * Output.ItemHeight )
		);
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );

		foreach ( var child in Children )
		{
			Unselect( child );
		}
	}

	private void Unselect( Panel p )
	{
		if ( p is Label l )
		{
			l.ShouldDrawSelection = false;
			return;
		}

		foreach ( var child in p.Children )
		{
			Unselect( child );
		}
	}

}
