using NUnit.Framework;
using UnityEngine;

public class UniformTextureCreatorTest
{
	#region Tests
	[ Test ]
	public void Should_CreatedTexture_HaveExpectedSize()
	{
		// Setup
		int testW = 2;
		int testH = 2;

		// Test
		Texture2D testTexture = UniformTextureCreator.CreateUniformTexture( TextureFormat.ARGB32, Color.magenta, true, testW, testH );

		// Assert
		Assert.IsTrue( testTexture.width == testW && testTexture.height == testH );
	}

	[ Test ]
	[ TestCase( false ) ]
	[ TestCase( true ) ]
	public void Should_CreatedTexture_HaveCorrectUniformColor( bool isLinear )
	{
		// Setup
		Color32 testColor = new Color32( 111, 222, 33, 144 );

		// Test
		Texture2D testTexture = UniformTextureCreator.CreateUniformTexture( TextureFormat.ARGB32, testColor, isLinear );

		// Assert
		Color32[] allPixels = testTexture.GetPixels32();
		foreach( Color32 res in allPixels )
		{
			Assert.AreEqual( res, testColor );
		}
	}

	[ Test ]
	[ TestCase( TextureFormat.ARGB32 ) ]
	[ TestCase( TextureFormat.DXT5 ) ]
	public void Should_DetectFormat_ReturnARGB32_IfFormatHasAlpha( TextureFormat format )
	{
		TextureFormat result = UniformTextureCreator.DetectFormat( format );

		Assert.IsTrue( result == TextureFormat.ARGB32 );
	}

	[ Test ]
	[ TestCase( TextureFormat.RGB24 ) ]
	[ TestCase( TextureFormat.DXT1 ) ]
	public void Should_DetectFormat_ReturnRGB24_IfFormatHasNoAlpha( TextureFormat format )
	{
		TextureFormat result = UniformTextureCreator.DetectFormat( format );

		Assert.IsTrue( result == TextureFormat.RGB24 );
	}
	#endregion
}
