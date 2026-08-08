namespace Editor.Assets;

[AssetPreview( "vmat" )]
class PreviewMaterial : AssetPreview
{
	public override float PreviewWidgetCycleSpeed => 0.2f;

	SkyBox2D skyboxObject;

	static readonly Model Plane = Model.Load( "models/dev/plane_blend.vmdl" );
	private SceneSpotLight _sceneSpotLight;

	public PreviewMaterial( Asset asset ) : base( asset )
	{

	}

	/// <summary>
	/// Create the model or whatever needs to be viewed
	/// </summary>
	public override async Task InitializeAsset()
	{
		var material = await Material.LoadAsync( Asset.Path );
		if ( material is null ) return;

		using ( Scene.Push() )
		{
			PrimaryObject = new GameObject { WorldTransform = Transform.Zero };

			if ( material.Flags.IsSky )
			{
				skyboxObject = PrimaryObject.AddComponent<SkyBox2D>();
				skyboxObject.SkyMaterial = material;
			}
			else
			{
				var go = Scene.Directory.FindByName( "envmap" )?.FirstOrDefault() ?? new GameObject( true, "envmap" );
				var c = go.GetOrAddComponent<EnvmapProbe>();
				c.WorldPosition = new Vector3( 0, 0, 0 );
				c.TintColor = Color.White * 1;
				var sprite = PrimaryObject.AddComponent<ModelRenderer>();
				sprite.Model = Plane;
				sprite.MaterialOverride = material;

				var multiplier = material.ShaderName.StartsWith( "shaders/hl2k_" ) ? 3.1415926f * 0.8f : 0.8f;

				_sceneSpotLight = new SceneSpotLight( Scene.SceneWorld )
				{
					Radius = 4000,
					LightColor = Color.White * multiplier,
					Position = new Vector3( 0, 0, 128 ),
					ConeOuter = 89,
					ConeInner = 0,
					QuadraticAttenuation = 5f,
					ShadowsEnabled = true,
					Rotation = Rotation.From( 90, 0, 0 )
				};
			}
		}
	}

	public override void UpdateScene( float cycle, float timeStep )
	{
		// Big fov for skybox preview so you can see a few sides
		if ( skyboxObject.IsValid() )
		{
			Camera.WorldPosition = Vector3.Zero;
			Camera.WorldRotation = new Angles( 0, 180 * cycle, 0 );
			Camera.FieldOfView = 120;
			return;
		}

		float spin = 180 * cycle;
		float pitch = 90;

		Camera.WorldPosition = Vector3.Up * 300;
		Camera.WorldRotation = new Angles( pitch, 180 + spin, 0 );

		PrimaryObject.WorldRotation = new Angles( 0, spin, 0 );

		// make sure the thumbnail gets the default
		if ( cycle > 5 ) _sceneSpotLight.Position = new( MathF.Cos( cycle * MathF.PI * 2f ) * 64, MathF.Sin( cycle * MathF.PI * 2f ) * 64, 128 );

		SceneCenter = 0;
		SceneSize = 55;
		FrameScene();
	}

	public override Widget CreateToolbar()
	{
		var info = new IconButton( "settings" )
		{
			Layout = Layout.Row(),
			MinimumSize = 16
		};

		info.MouseLeftPress = () => OpenSettings( info );

		return info;
	}

	public void OpenSettings( Widget parent )
	{
		var popup = new PopupWidget( parent )
		{
			IsPopup = true,
			Layout = Layout.Column()
		};

		popup.Layout.Margin = 16;

		var ps = new ControlSheet();

		ps.AddProperty( this, x => x.BackgroundColor );
		ps.AddProperty( PrimaryObject.GetComponent<ModelRenderer>(), x => x.Tint );
		//ps.AddProperty( Camera, x => x.EnablePostProcessing );

		popup.Layout.Add( ps );
		popup.MaximumWidth = 300;
		popup.Show();
		popup.Position = parent.ScreenRect.TopRight - popup.Size;
		popup.ConstrainToScreen();
	}
}
