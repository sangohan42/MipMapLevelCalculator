using NUnit.Framework;
using UnityEngine;

using static GraphicUtils;

public class GraphicUtilsTest
{
	private Texture2D _testTexture;

	#region Setup and teardown
	[ SetUp ]
	public void Setup()
	{
		_testTexture = Texture2D.blackTexture;
	}
	#endregion

	[ Test ]
	[ TestCase( 512, 512 ) ]
	[ TestCase( 75, 230 ) ]
	[ TestCase( 256, 128 ) ]
	public void Should_GetSizeFromLOD_Return_CorrectSizeForAllLODs( int width, int height )
	{
		// Setup
		_testTexture.Resize( width, height, TextureFormat.ARGB32, true );

		int minTextureSize = -1; // no min for this test

		for( int lod = 0; lod < GetNbMipMapsForSmallestDimension( width, height ); lod++ )
		{
			// Test
			Vector2Int size = GetSizeFromLOD( _testTexture, lod, minTextureSize );

			//Assert
			Assert.AreEqual( _testTexture.GetPixels( lod ).Length, size.x * size.y );
		}
	}

	[ Test ]
	[ TestCase( 512, 512, 2 ) ]
	[ TestCase( 75, 230, 2 ) ]
	[ TestCase( 256, 128, 4 ) ]
	public void Should_GetSizeFromLOD_ReturnMinSize_WhenLODSizeIsSmaller( int width, int height, int lod )
	{
		// Setup
		_testTexture.Resize( width, height, TextureFormat.ARGB32, true );

		GetSizeFromLOD( _testTexture, 1, -1 );
		// make sure minSize is bigger than the result size
		Vector2Int sizeAboveExpected = GetSizeFromLOD( _testTexture, lod - 1, -1 );
		int minTextureSize = Mathf.Min( sizeAboveExpected.x, sizeAboveExpected.y );

		// Test
		Vector2Int size = GetSizeFromLOD( _testTexture, lod, minTextureSize );

		//Assert
		Assert.AreEqual( Mathf.Min( size.x, size.y ), minTextureSize );
	}

	[ Test ]
	[ TestCase( 0 ) ]
	[ TestCase( -512 ) ]
	public void Should_GetSizeFromLOD_ReturnOne_WhenMinSizeIsZeroOrLess( int minTextureSize )
	{
		// Setup
		_testTexture.Resize( 4, 4, TextureFormat.ARGB32, true );

		// Test
		Vector2Int size = GetSizeFromLOD( _testTexture, 2, minTextureSize );

		//Assert
		Assert.AreEqual( size.x, 1 );
	}

	[ Test ]
	[ TestCase( 512, 512 ) ]
	[ TestCase( 75, 230 ) ]
	[ TestCase( 256, 128 ) ]
	public void Should_GetSizeFromLOD_ReturnMinSize_WhenLODIsBiggerThanAvailable( int width, int height )
	{
		// Setup
		_testTexture.Resize( width, height, TextureFormat.ARGB32, true );

		int minTextureSize = Mathf.Min( width, height ) / 2;

		// Test
		Vector2Int size = GetSizeFromLOD( _testTexture, GetNbMipMapsForSmallestDimension( width, height ) + 1, minTextureSize );

		//Assert
		Assert.AreEqual( Mathf.Min( size.x, size.y ), minTextureSize );
	}

	[ Test ]
	[ TestCase( 512, 512 ) ]
	[ TestCase( 75, 230 ) ]
	[ TestCase( 256, 128 ) ]
	public void Should_GetSizeFromLOD_ReturnTexSize_WhenMinSizeIsBigger( int width, int height )
	{
		// Setup
		_testTexture.Resize( width, height, TextureFormat.ARGB32, true );

		int minTextureSize = width * 2;

		// Test
		Vector2Int size = GetSizeFromLOD( _testTexture, GetNbMipMapsForSmallestDimension( width, height ) - 1, minTextureSize );

		//Assert
		Assert.AreEqual( size.x * size.y, width * height );
	}

	[ Test ]
	[ TestCase( 512, 512 ) ]
	[ TestCase( 207, 512 ) ]
	[ TestCase( 151, 512 ) ]
	public void Should_GetNbMipMaps_ReturnCorrectNumber( int width, int height )
	{
		// Setup
		// We're comparing to Unity's mipMap method, which gets mipMaps from the biggest side 
		// So we create a texture with dimensions equal to the smallest side in order to compare correctly
		int smallerSide = Mathf.Min( width, height );
		_testTexture.Resize( smallerSide, smallerSide, TextureFormat.ARGB32, true );
		_testTexture.Apply( true ); 

		// Test
		int nbMipMaps = GetNbMipMapsForSmallestDimension( width, height );

		// Assert 
		Assert.AreEqual( nbMipMaps, _testTexture.mipmapCount );
	}
}
