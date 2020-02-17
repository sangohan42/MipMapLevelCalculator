using UnityEngine;

public static class UniformTextureCreator
{
    public static TextureFormat DetectFormat( TextureFormat originFormat )
    {
        TextureFormat format = TextureFormat.ARGB32;
        if( originFormat == TextureFormat.RGB24 || originFormat == TextureFormat.DXT1 )
            format = TextureFormat.RGB24;

        return format;
    }

    /// <summary>
    /// Creates an uncompressed uniform texture
    /// </summary>
    /// <param name="format">The desired texture format</param>
    /// <param name="color32">The fill color, must be 32bits, otherwise it will be cast</param>
    /// <param name="linear">If true the created texture is linear</param>
    /// <param name="w">Tex width</param>
    /// <param name="h">Tex height</param>
    /// <returns>An uncompressed uniform texture of the given size</returns>
    public static Texture2D CreateUniformTexture( TextureFormat format, Color32 color32, bool linear, int w = 2, int h = 2 )
    {
        Texture2D tex = new Texture2D( w, h, format, false, linear );
        Color32[] colors = new Color32[ w * h ];

        for( int i = 0; i < w * h; i++ )
            colors[ i ] = color32;

        //Debug.Log( "Setting color "+col.ToString("F6") );
        tex.SetPixels32( colors );
        tex.Apply();

        return tex;
    }
}
