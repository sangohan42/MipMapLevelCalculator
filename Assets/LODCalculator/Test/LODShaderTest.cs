using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class LODShaderTest
{
	#region Private fields
	private LODRenderer _lodRenderer;
	private Material _lodMaterial;

	private Texture2D _inputTex;
	private List<Color> _inputTexMipMapColors;

	private Renderer _cubeRenderer;
	private Camera _camera;

	private int _cameraRenderWidth;
	private int _cameraRenderHeight;
	#endregion

	#region Setup & Teardown
	[ SetUp ]
	public void Setup()
	{
		_lodRenderer = new LODRenderer();
		_inputTex = Resources.Load<Texture2D>( "TestTexture" );

		_lodMaterial = new Material( _lodRenderer.LODShader );
		_cubeRenderer = InstantiateCubeWithLodShader();

		_camera = InstantiateCamera( _cubeRenderer.transform );

		_cameraRenderWidth = 1024;
		_cameraRenderHeight = 1024;
	}

	[ TearDown ]
	public void Teardown()
	{
		_inputTexMipMapColors = null;
		_inputTex = null;

		RenderTexture.active = null;

		Object.DestroyImmediate( _camera.gameObject );
		Object.DestroyImmediate( _cubeRenderer.gameObject );
	}

	private Renderer InstantiateCubeWithLodShader()
	{
		GameObject cube = GameObject.CreatePrimitive( PrimitiveType.Cube );
		cube.layer = 0;
		Renderer cubeRenderer = cube.GetComponent<Renderer>();
		SetLODShader( cubeRenderer );

		return cubeRenderer;
	}

	private void SetLODShader( Renderer renderer )
	{
		renderer.sharedMaterial = _lodMaterial;
	}

	private Camera InstantiateCamera( Transform target )
	{
		Camera cam = new GameObject( "camera" ).AddComponent<Camera>();
		cam.orthographic = false;
		cam.nearClipPlane = 0.01f;
		cam.transform.position = target.transform.position;
		cam.transform.forward = -target.transform.forward;
		cam.transform.Translate( 0, 0, -target.lossyScale.z * 2 );
		cam.backgroundColor = new Color( 0, 0, 0, 0 );
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.cullingMask = 1 << target.gameObject.layer;

		return cam;
	}
	#endregion

	#region Helper methods
	// The test texture is produced with a different color in each mip map level
	// in order to allow visual debugging
	private List<Color> GetMipMapColors( Texture2D testTexture )
	{
		List<Color> texMipMaps = new List<Color>();
		int maxMipLevel = Mathf.RoundToInt( Mathf.Log( testTexture.width, 2 ) );

		for( int i = 0; i <= maxMipLevel; i++ )
		{
			Color[] texColors = testTexture.GetPixels( i );
			texMipMaps.Add( texColors[ texColors.Length / 2 ] );
			//Debug.Log( i + " : " + texColors[ texColors.Length / 2 ] );
		}

		//Debug.Log( "Found " +texMipMaps.Count +" mip map colors");
		return texMipMaps;
	}

	private Color GetCenterPixelOfCameraRender( int width, int height )
	{
		RenderTexture rt = _lodRenderer.Render( _camera, width, height );

		Texture2D readTex = new Texture2D( rt.width, rt.height, TextureFormat.RGBAFloat, false, true ) { filterMode = FilterMode.Point };
		RenderTexture.active = rt;
		readTex.ReadPixels( new Rect( 0, 0, readTex.width, readTex.height ), 0, 0 );
		readTex.Apply();
		//System.IO.File.WriteAllBytes( Application.dataPath + "/mipTex_test.png", readTex.EncodeToPNG() );

		RenderTexture.active = null;
		rt.Release();
		Object.DestroyImmediate( rt );

		return readTex.GetPixel( readTex.width / 2, readTex.height / 2 );
	}

	private void SetMaterialToUnlitTexture( Renderer renderer, Texture tex )
	{
		renderer.sharedMaterial = new Material( Shader.Find( "Unlit/Texture" ) );
		renderer.sharedMaterial.mainTexture = tex;
	}

	private int GetMipMapLevelFromLOD( float scaledLod, float lodScale )
	{
		// the original mip level is multiplied by lodScale in the shader, so divide it back
		return Mathf.RoundToInt( scaledLod / lodScale );
	}
	#endregion

	#region Tests
	[ Test ]
	public void Should_Camera_RenderAllAvailableLODLevels_WhenMovingAwayFromObject()
	{
		_inputTexMipMapColors = GetMipMapColors( _inputTex );
		_lodMaterial.SetTexture( "_TargetTex", _inputTex );

		int maxMipLevel = GraphicUtils.GetNbMipMapsForSmallestDimension( _inputTex.width, _inputTex.height ) - 1;

		int mipLevel = -1;
		// this is just a debug variable to check that the camera is not moving back too fast. 
		// mip level should not change in a single translation, except for the first time when it's initialized to -1
		// so init the var at 1 the first time and 0 each time afterwards
		int cameraMoveSteps = 1;
		// an approximate value to prevent the test from looping endlessly 
		// it was obtained by observation of the current test, might not be always good if the test changes!
		int nbStepsAllowedToNextMip = cameraMoveSteps * 2; 
		while( mipLevel < maxMipLevel )
		{
			float translation = -0.4f;
			// go faster as lod grows, because it takes longer to switch on bigger distances
			if( mipLevel > 0 )
				translation *= mipLevel;
			_camera.transform.Translate( 0, 0, translation );

			SetLODShader( _cubeRenderer );
			Color cubePixel = GetCenterPixelOfCameraRender( _cameraRenderWidth, _cameraRenderHeight );

			if( GetMipMapLevelFromLOD( cubePixel.r, cubePixel.b ) > mipLevel )
			{
				mipLevel = GetMipMapLevelFromLOD( cubePixel.r, cubePixel.b );
				//Debug.Log( "LOD " + cubePixel.r + ", mip level " + mipLevel + ", got here in " + nbIterations );

				SetMaterialToUnlitTexture( _cubeRenderer, _inputTex );

				cubePixel = GetCenterPixelOfCameraRender( _cameraRenderWidth, _cameraRenderHeight );
				//Debug.Log( "Check LOD " + cubePixel +" for miplevel "+mipLevel );
				Assert.IsTrue( GraphicUtils.AreColorsRoughlyEqual( cubePixel, _inputTexMipMapColors[ mipLevel ] ) );
				// Changing the mip should take at least 2 steps back for the camera (at least 1 where it was the same, and one where it changed)
				Assert.IsTrue( cameraMoveSteps >= 1, "Mip level changed too fast" ); 

				// reset cameraMoveSteps for next level
				nbStepsAllowedToNextMip = cameraMoveSteps * 2;
				cameraMoveSteps = 0;
			}
			else
			{
				cameraMoveSteps++;
				// a hacky way of preventing endless loops. Might not work with a bigger test texture ( more steps may be needed for mip levels after 10 )
				if( cameraMoveSteps > 10 && cameraMoveSteps > nbStepsAllowedToNextMip )
					throw new Exception( "Mip level (" + mipLevel + ") stayed unchanged for too many camera steps: " + cameraMoveSteps );
			}
		}
	}

	[ Test ]
	[ TestCase( 0 ) ]
	[ TestCase( 1 ) ]
	public void TestDiscardProperty( int discard )
	{
		// Setup
		_lodMaterial.SetTexture( "_TargetTex", _inputTex );
		_lodMaterial.SetInt( "_Discard", discard );

		// Test
		Color cubePixel = GetCenterPixelOfCameraRender( _cameraRenderWidth, _cameraRenderHeight );

		// Assert
		bool pixelIsRendered = cubePixel.r + cubePixel.g + cubePixel.b + cubePixel.a > 0;
		//Debug.Log( cubePixel );
		if( discard == 0 )
			Assert.IsTrue( pixelIsRendered, cubePixel.ToString() );
		else
			Assert.IsFalse( pixelIsRendered, cubePixel.ToString() );
	}

	[ Test ]
	[ TestCase( false ) ]
	[ TestCase( true ) ]
	public void Should_ObjectBeRendered_WhenOccludedOrNot( bool targetIsOccluded )
	{
		// Setup
		int discardFrontObject = targetIsOccluded ? 0 : 1;
		float frontCubeTexId = 0.555f;
		float backCubeTexId = 0.777f;

		// setup the original cube
		_lodMaterial.SetTexture( "_TargetTex", _inputTex );
		_lodMaterial.SetFloat( "_TextureId", frontCubeTexId );
		_lodMaterial.SetInt( "_Discard", discardFrontObject );

		// create a cube behind the original
		Renderer backCube = InstantiateCubeWithLodShader();
		backCube.transform.Translate( 0, 0, -2f );
		Material backCubeMat = backCube.material; // make a copy!
		backCubeMat.SetFloat( "_TextureId", backCubeTexId );
		backCubeMat.SetInt( "_Discard", 0 );

		// Test
		Color cubePixel = GetCenterPixelOfCameraRender( _cameraRenderWidth, _cameraRenderHeight );

		// Assert
		float renderedTextureId = cubePixel.g;
		float expectedTextureId = targetIsOccluded ? frontCubeTexId : backCubeTexId;
		
		Assert.AreEqual( renderedTextureId, expectedTextureId );

		//Teardown
		Object.DestroyImmediate( backCubeMat );
		Object.DestroyImmediate( backCube );
	}
	#endregion
}
