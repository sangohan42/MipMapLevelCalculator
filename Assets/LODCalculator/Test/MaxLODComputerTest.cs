using System;
using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

public class MaxLODComputerTest
{
	private ComputeShader _notCompilingComputeShader;
	private MaxLODComputer _maxLodComputer;

	private delegate int LODGenerationMethod( int texId, int nbMipMaps, float increasePercentage = 0f );

	#region Setup and teardown
	[ SetUp ]
	public void Setup()
	{
		_notCompilingComputeShader = Resources.Load<ComputeShader>( "NotCompilingComputeShader" );
	}
	#endregion

    [ Test ]
	[ TestCase( 30, 10, false ) ]
	[ TestCase( 400, 10, true ) ]
	public void Should_GetMaxLOD_Return_MaxLOD_WithAnyLODInput( int nbTextures, int nbLevelsOfDetail, bool constantLOD )
	{
        _maxLodComputer = new MaxLODComputer();
		Texture testTexture = SimulateSceneRender( nbTextures, nbLevelsOfDetail, GenerateLODIntFromTextureIndex, constantLOD );

		int[] result = _maxLodComputer.GetMaxLOD( testTexture, nbTextures );

		UnityEngine.Object.Destroy( testTexture );
		for( int i = 0; i < result.Length; i++ )
		{
			int expectedLOD = GenerateLODIntFromTextureIndex( i, nbLevelsOfDetail );
			//Debug.Log( i + ": Found lod " + result[ i ] + ", expecting " + expectedLOD );
			Assert.AreEqual( expectedLOD, result[ i ] );
		}
	}

	[ Test ]
	[ TestCase( 75, 10, false ) ]
	[ TestCase( 236, 10, true ) ]
	public void Should_GetMaxLOD_Return_AsManyResults_AsNumberOfTextures( int nbTextures, int nbLevelsOfDetail, bool constantLOD )
	{
		_maxLodComputer = new MaxLODComputer();
		Texture transparentTexture = UniformTextureCreator.CreateUniformTexture( TextureFormat.ARGB32, new Color32( 0, 0, 0, 0 ), true );

		int[] result = _maxLodComputer.GetMaxLOD( transparentTexture, nbTextures );

		Assert.IsTrue( result.Length == nbTextures );
	}

	[ Test ]
	[ TestCase( 7 ) ]
	public void Should_GetMaxLOD_ReturnOccludedLOD_WhenTextureIsNotRendered( int nbTextures )
	{
		_maxLodComputer = new MaxLODComputer();
		Texture transparentTexture = UniformTextureCreator.CreateUniformTexture( TextureFormat.ARGB32, new Color32( 0, 0, 0, 0 ), true );

		int[] result = _maxLodComputer.GetMaxLOD( transparentTexture, nbTextures );

		foreach( int lod in result )
			Assert.AreEqual( lod, MaxLODComputer.OCCLUDED_TEX_LOD );
	}

	[ Test ]
	public void Should_GetMaxLOD_ThrowCorrectArgException_WhenNumberOfTexturesIsZero()
	{
		_maxLodComputer = new MaxLODComputer();
		Texture transparentTexture = UniformTextureCreator.CreateUniformTexture( TextureFormat.ARGB32, new Color32( 0, 0, 0, 0 ), true );

		ArgumentException ex = Assert.Throws<ArgumentException>( () => _maxLodComputer.GetMaxLOD( transparentTexture, 0 ) );
		Assert.AreEqual( ex.ParamName, "count" );
	}

	[ Test ]
	[ TestCase( 5 ) ]
	public void Should_ConvertTextureId_ThrowCorrectArgException_When_TotalTexturesAreZeroOrLess( int zeroBasedId )
	{
		_maxLodComputer = new MaxLODComputer();

		int[] invalidTexCountValues = { 0, -1 };
		foreach( int invalidTexturesCountValue in invalidTexCountValues )
		{
			ArgumentException ex = Assert.Throws<ArgumentException>( () => _maxLodComputer.ConvertTextureId( zeroBasedId, invalidTexturesCountValue ) );
			Assert.AreEqual( ex.ParamName, "totalNumberOfTextures" );
		}
	}

	[ Test ]
	[ TestCase( 10 ) ]
	public void Should_ConvertTextureId_ThrowCorrectArgException_When_ZeroBasedId_IsLessThanZero( int totalNumberOfTextures )
	{
		_maxLodComputer = new MaxLODComputer();

		ArgumentException ex = Assert.Throws<ArgumentException>( () => _maxLodComputer.ConvertTextureId( -1, totalNumberOfTextures ) );
		Assert.AreEqual( ex.ParamName, "zeroBasedId" );
	}

	[ Test ]
	[ TestCase( 9 ) ]
	public void Should_ConvertTextureId_ThrowCorrectArgException_When_IdIsNotSmallerThanTotal( int totalNumberOfTextures )
	{
		_maxLodComputer = new MaxLODComputer();

		int[] invalidTexIds = { totalNumberOfTextures, totalNumberOfTextures + 1 };
		foreach( int invalidTexId in invalidTexIds )
		{
			ArgumentException ex = Assert.Throws<ArgumentException>( () => _maxLodComputer.ConvertTextureId( invalidTexId, totalNumberOfTextures ) );
			Assert.AreEqual( ex.Message, "The id of the texture must be smaller than the total number of textures" );
		}
	}

	[ Test ]
	[ TestCase( 0, 10 ) ]
	[ TestCase( 8, 9 ) ]
	public void Should_ConvertTextureId_ReturnFloatBetween0and1_WhenArgumentsAreValid( int zeroBasedId, int totalNumberOfTextures )
	{
		_maxLodComputer = new MaxLODComputer();
		float id = _maxLodComputer.ConvertTextureId( zeroBasedId, totalNumberOfTextures );

		Assert.IsTrue( id >= 0f && id < 1f );
	}

	#region Helper functions
	Texture SimulateSceneRender( int nbTextures, int nbLevelsOfDetail, LODGenerationMethod lodGenerationMethod, bool constantLOD )
	{
		if( nbTextures <= 0 )
			throw new ArgumentException( "Argument must be greater than 0", nameof( nbTextures ) );

		float lodScale = 0.1f;
		int size = GetSceneSizeFittingAllTextures( nbTextures );
		int nbPixels = size * size;
		Color[] pixels = new Color[ nbPixels ];
		// how many pixels will be dedicated to each texture (= rendered with the same texId)
		int pixelsPerTex = Mathf.FloorToInt( nbPixels / ( float )nbTextures );

		int texId = 0;
		for( int i = 0; i < nbPixels; i++ )
		{
			// increase texture index after each block of pixelsPerTex (except the last one)
			if( i > 0 && i % pixelsPerTex == 0 && texId < nbTextures - 1 )
				texId++;

			// the index of the pixel relevant to the block of pixelsPerTex
			// = the Nth pixel that we're rendering for the current texture
			int curTexPixel = i - texId * pixelsPerTex;

			float increaseLODBy = constantLOD ? 0f : ( float )curTexPixel / ( float )pixelsPerTex;

			// when setting the last block or pixelsPerTex, curTexId may exceed that block,
			// if the total number of pixels is not a multiple of pixelsPerTex
			// so only render if we're still rendering the pixels dedicated for the texture
			if( curTexPixel < pixelsPerTex )
			{
				float lod = lodGenerationMethod( texId, nbLevelsOfDetail, increaseLODBy );
				//Debug.Log( texId + ": generating scaled lod " + lod );
				pixels[ i ] = new Color( lod * lodScale, _maxLodComputer.ConvertTextureId( texId, nbTextures ), lodScale, 1 );
			}
			else // for the remaining pixels in sceneRender
				pixels[ i ] = new Color( 0, 0, 0, 0 );
		}

		Texture2D sceneRender = new Texture2D( size, size, TextureFormat.RGBAFloat, false, true );
		sceneRender.SetPixels( pixels );
		sceneRender.Apply();
		return sceneRender;
	}

	private int GetSceneSizeFittingAllTextures( int nbTextures )
	{
		int size = 2;
		while( size * size < nbTextures )
			size *= 2;

		return size;
	}

	// cycle over the number of mipmaps and assing directly that value to each subsequent id
	// increase percentage allows to increase that id within the remaining lod values
	// so the value generated with increasePercentage = 0, will be the smallest generated lod for that id
	// increasePercentage ranges [0:1] and depends on the number of increases we would like to generate
	// in this test it's the number of pixels that will be rendered with the same texture id
	private int GenerateLODIntFromTextureIndex( int textureId, int mipMapsNumber, float increasePercentage = 0f )
	{
		int lod = textureId;

		while( lod > mipMapsNumber )
		{
			lod -= mipMapsNumber;
		}

		int margin = mipMapsNumber - lod;
		lod += Mathf.RoundToInt( increasePercentage * margin );

		return lod;
	}
	#endregion
}
