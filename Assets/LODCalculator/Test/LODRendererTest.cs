using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object; 

public class LODRendererTest
{
	#region Private fields
	private LODRenderer _lodRenderer;
	private Texture2D _testTexture;
	#endregion

	#region Setup and teardown
	[ SetUp ]
	public void Setup()
	{
		_lodRenderer = new LODRenderer();
		_testTexture = Texture2D.blackTexture;
	}

	private Renderer InstantiateCubeWithColor( Color color )
	{
		GameObject cube = GameObject.CreatePrimitive( PrimitiveType.Cube );
		Renderer r = cube.GetComponent<Renderer>();
		r.sharedMaterial.shader = Shader.Find( "Unlit/Color" );
		r.sharedMaterial.color = color;

		return r;
	}

	private Camera InstantiateCamera( Transform target )
	{
		Camera cam = new GameObject( "camera" ).AddComponent<Camera>();
		cam.orthographic = false;
		cam.nearClipPlane = 0.01f;
		cam.transform.position = target.transform.position;
		cam.transform.forward = -target.transform.forward;
		cam.transform.Translate( 0, 0, -target.lossyScale.z * 3 );
		cam.backgroundColor = new Color( 0, 0, 0, 0 );
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.cullingMask = 1 << target.gameObject.layer;

		return cam;
	}

	[ TearDown ]
	public void Teardown()
	{
		_lodRenderer = null;
	}
	#endregion

	#region Tests
	[ Test ]
	public void Should_IsRenderedWithLOD_Return_True_IfMaterialWasAddedAsLOD()
	{
		// Setup 
		Material testMat = new Material( Shader.Find( "Standard" ) );
		_lodRenderer.SetupLODShader( testMat, null, 0 );

		// Test
		bool wasAdded = _lodRenderer.IsRenderedWithLOD( testMat );

		// Assert
		Assert.IsTrue( wasAdded );
	}

	[ Test ]
	public void Should_IsRenderedWithLOD_Return_True_IfMaterialWasDiscarded()
	{
		// Setup 
		Material testMat = new Material( Shader.Find( "Standard" ) );
		_lodRenderer.SetupLODShaderWithDiscard( testMat );

		// Test
		bool wasAdded = _lodRenderer.IsRenderedWithLOD( testMat );

		// Assert
		Assert.IsTrue( wasAdded );
	}

	[ Test ]
	public void Should_SetupLODShader_SetAllLODProperties()
	{
		// Setup 
		Material testMat = new Material( Shader.Find( "Standard" ) );
		float texId = 0.1234f;

		// Test
		_lodRenderer.SetupLODShader( testMat, _testTexture, texId );

		// Assert
		Assert.IsTrue( testMat.shader == _lodRenderer.LODShader );
		Assert.IsTrue( testMat.GetTexture( "_TargetTex" ) == _testTexture );
		Assert.AreEqual( testMat.GetFloat( "_TextureId" ), texId );
		Assert.AreEqual( testMat.GetInt( "_Discard" ), 0 );
	}

	[ Test ]
	public void Should_SetupLODShaderWithDiscard_SetDiscardProperty()
	{
		// Setup 
		Material testMat = new Material( Shader.Find( "Standard" ) );

		// Test
		_lodRenderer.SetupLODShaderWithDiscard( testMat );

		// Assert
		Assert.IsTrue( testMat.shader == _lodRenderer.LODShader );
		Assert.AreEqual( testMat.GetInt( "_Discard" ), 1 );
	}

	[ Test ]
	public void Should_AllReplacedShaders_BeRestored()
	{
		// Setup
		Material standardMat = new Material( Shader.Find( "Standard" ) );
		Material specularMat = new Material( Shader.Find( "Standard (Specular setup)" ) );
		Material diffuseMat = new Material( Shader.Find( "Legacy Shaders/Diffuse" ) );

		_lodRenderer.SetupLODShader( standardMat, null, 0 );
		_lodRenderer.SetupLODShaderWithDiscard( specularMat );
		_lodRenderer.SetupLODShader( diffuseMat, null, 0 );

		// Test
		_lodRenderer.RestoreAllReplacedShaders();

		// Assert
		Assert.IsTrue( standardMat.shader == Shader.Find( "Standard" ), "Shader name was " + standardMat.shader.name );
		Assert.IsTrue( specularMat.shader == Shader.Find( "Standard (Specular setup)" ), "Shader name was " + specularMat.shader.name );
		Assert.IsTrue( diffuseMat.shader == Shader.Find( "Legacy Shaders/Diffuse" ), "Shader name was " + diffuseMat.shader.name );
	}

	[ Test ]
	public void Should_RenderWithLOD_Produce_CorrectTexture()
	{
		// Setup
		Color cubeColor = new Color( 0.05f, 0.12345f, 0f, 1f );
		Color blackColor = new Color( 0f, 0f, 0f, 0f );
		Renderer cube = InstantiateCubeWithColor( cubeColor );
		Camera cam = InstantiateCamera( cube.transform );

		// Test
		RenderTexture rt = _lodRenderer.Render( cam, 128, 128 );

		Texture2D readTex = new Texture2D( rt.width, rt.height, TextureFormat.RGBAFloat, false, true ) { filterMode = FilterMode.Point };
		RenderTexture.active = rt;
		readTex.ReadPixels( new Rect( 0, 0, readTex.width, readTex.height ), 0, 0 );
		readTex.Apply();
		//System.IO.File.WriteAllBytes( Application.dataPath + "/mipTex_test.png", readTex.EncodeToPNG() );

		RenderTexture.active = null;
		rt.Release();
		Object.DestroyImmediate( rt );

		// Assert
		foreach( Color p in readTex.GetPixels() )
		{
			// test against linear cube color, because the target texture is linear
			Assert.IsTrue( p == blackColor || p == cubeColor.linear, "Color was " + p.ToString( "F6" ) );
		}

		// Teardown
		Object.DestroyImmediate( readTex );
		Object.DestroyImmediate( cube );
		Object.DestroyImmediate( cam );
	}
	#endregion
}
