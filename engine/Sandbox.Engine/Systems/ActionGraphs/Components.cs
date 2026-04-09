
using static Sandbox.Component;

namespace Sandbox.ActionGraphs;

/// <summary>
/// A component which allows you to use action in all the usual functions.
/// </summary>
[Title( "Actions Invoker" ), Group( "Actions" ), Icon( "bolt" )]
public sealed class ActionsInvoker : Component
{
	[Property] public Action OnEnabledAction { get; set; }

	protected override void OnEnabled()
	{
		OnEnabledAction?.InvokeWithWarning();
	}

	[Property] public Action OnUpdateAction { get; set; }

	protected override void OnUpdate()
	{
		OnUpdateAction?.InvokeWithWarning();
	}

	[Property] public Action OnFixedUpdateAction { get; set; }

	protected override void OnFixedUpdate()
	{
		OnFixedUpdateAction?.InvokeWithWarning();
	}

	[Property] public Action OnDisabledAction { get; set; }

	protected override void OnDisabled()
	{
		OnDisabledAction?.InvokeWithWarning();
	}

	[Property] public Action OnDestroyAction { get; set; }

	protected override void OnDestroy()
	{
		OnDestroyAction?.InvokeWithWarning();
	}
}

/// <summary>
/// A component that only provides actions to implement with an Action Graph.
/// </summary>
public interface IActionComponent
{
}


/// <summary>
/// Reacts to collisions.
/// </summary>
[Obsolete( "TODO: We don't have a replacement for this yet." )]
[Title( "Collision" ), Group( "Actions" ), Icon( "minor_crash" )]
public class CollisionActionComponent : Component, ICollisionListener, IActionComponent
{
	public delegate void CollisionDelegate( Collision other );
	public delegate void CollisionStopDelegate( CollisionStop other );

	/// <inheritdoc cref="Component.ICollisionListener.OnCollisionStart"/>
	[Property]
	public CollisionDelegate CollisionStart { get; set; }

	/// <inheritdoc cref="Component.ICollisionListener.OnCollisionUpdate"/>
	[Property]
	public CollisionDelegate CollisionUpdate { get; set; }

	/// <inheritdoc cref="Component.ICollisionListener.OnCollisionStop"/>
	[Property]
	public CollisionStopDelegate CollisionStop { get; set; }

	void ICollisionListener.OnCollisionStart( Collision other )
	{
		CollisionStart?.Invoke( other );
	}

	void ICollisionListener.OnCollisionUpdate( Collision other )
	{
		CollisionUpdate?.Invoke( other );
	}

	void ICollisionListener.OnCollisionStop( CollisionStop other )
	{
		CollisionStop?.Invoke( other );
	}
}

/// <summary>
/// Reacts to collider triggers.
/// </summary>
[Obsolete( $"Please use \"{nameof( Collider )}.{nameof( Collider.OnTriggerEnter )}\" and \"{nameof( Collider )}.{nameof( Collider.OnTriggerExit )}\"." )]
[Title( "Trigger" ), Group( "Actions" ), Icon( "filter_center_focus" )]
public class TriggerActionComponent : Component, ITriggerListener, IActionComponent
{
	public delegate void TriggerDelegate( Collider other );

	/// <inheritdoc cref="Component.ITriggerListener.OnTriggerEnter(Collider)"/>
	[Property]
	public TriggerDelegate TriggerEnter { get; set; }

	/// <inheritdoc cref="Component.ITriggerListener.OnTriggerExit(Collider)"/>
	[Property]
	public TriggerDelegate TriggerExit { get; set; }

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		TriggerEnter?.Invoke( other );
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		TriggerExit?.Invoke( other );
	}
}
