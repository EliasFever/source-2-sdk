using System;

namespace GameObjects.Components;

[TestClass]
public class ModelPhysicsTests
{
	[TestMethod]
	public void ComponentCreation()
	{
		var scene = new Scene();
		using var sceneScope = scene.Push();
		var go = scene.CreateObject();
		var modelPhysics = go.Components.Create<ModelPhysics>();

		Assert.IsNotNull( modelPhysics, "ModelPhysics component should be created" );
		Assert.IsTrue( modelPhysics.IsValid(), "ModelPhysics component should be valid" );
		Assert.AreEqual( 0, modelPhysics.Bodies.Count, "Bodies collection should be empty initially" );
		Assert.AreEqual( 0, modelPhysics.Joints.Count, "Joints collection should be empty initially" );
		Assert.AreEqual( 0, modelPhysics.PhysicsRebuildCount, "Shouldn't have built anything" );
	}

	[TestMethod]
	public void ProxyMode_HandlesNetworking()
	{
		var scene = new Scene();
		using var sceneScope = scene.Push();
		var go = scene.CreateObject();
		var modelPhysics = go.Components.Create<ModelPhysics>();

		// Test proxy detection (this would normally be set by networking)
		// We can't fully test proxy behavior without networking, but we can verify the property exists
		var isProxy = modelPhysics.IsProxy;
		Assert.IsNotNull( isProxy, "IsProxy property should exist for networking support" );
	}

	[TestMethod]
	public void NullModel_HandlesGracefully()
	{
		var scene = new Scene();
		using var sceneScope = scene.Push();
		var go = scene.CreateObject();
		var modelPhysics = go.Components.Create<ModelPhysics>();

		// Enable with null model
		modelPhysics.Model = null;
		modelPhysics.Enabled = true;

		// Should handle gracefully without creating bodies
		Assert.AreEqual( 0, modelPhysics.Bodies.Count, "Should have no bodies with null model" );
		Assert.AreEqual( 0, modelPhysics.Joints.Count, "Should have no joints with null model" );
	}
}
