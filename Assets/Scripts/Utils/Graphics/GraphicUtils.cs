using UnityEngine;

public class GraphicUtils
{
    private static Renderer InstantiateCubeWithColor( Color color )
    {
        GameObject cube = GameObject.CreatePrimitive( PrimitiveType.Cube );
        Renderer r = cube.GetComponent<Renderer>();
        r.sharedMaterial.shader = Shader.Find( "Unlit/Color" );
        r.sharedMaterial.color = color;

        return r;
    }

    public static Renderer InstantiateCubeWithStandardShader(Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.parent = parent;
        Renderer r = cube.GetComponent<Renderer>();
        r.material = new Material( Shader.Find( "Standard" ) );
        return r;
    }

    public static Vector3 RandomPointInBox( Vector3 center, Vector3 size )
    {
        return center + new Vector3(
                   (Random.value - 0.5f) * size.x,
                   (Random.value - 0.5f) * size.y,
                   (Random.value - 0.5f) * size.z
               );
    }

    // allow for float precision error. Typically it happens when a component is 100/255 to have an output of 0.392 or 0.388, which is exactly in the range of 32bit conversion
    // Probably somewhere along the rendering pipeline 100 gets converted to 0.392, then to 99.99, which is 99 when converted to Color32, so back as Color it gives 0.388
    // However, consecutive mip map colors in the test are radically different, so this kind of precision is not needed for the test
    public static bool AreColorsRoughlyEqual( Color c1, Color c2 )
	{
		float color32 = 1 / 255f;
		for( int i = 0; i < 4; i++ )
		{
			if( Mathf.Abs( c1[ i ] - c2[ i ] ) > color32 )
				return false;
		}
		return true;
	}

	/// <summary>
	/// Useful for runtime textures that do not have mipmaps enabled,
	/// so the Unity method cannot be used
	/// </summary>
	public static int GetNbMipMapsForSmallestDimension( int width, int height )
	{
		float nbMipMaps = Mathf.Min( Mathf.Log( width, 2 ) + 1, Mathf.Log( height, 2 ) + 1 );
		//Debug.Log( "(float)nbmipmaps was " + nbMipMaps + " for " + width + "*" + height );
		return Mathf.FloorToInt( nbMipMaps );
	}

	public static Vector2Int GetSizeFromLOD( Texture2D tex, int lod, int minTextureSize = 128 )
	{
		if( minTextureSize < 1 )
			minTextureSize = 1;

		Vector2Int size = new Vector2Int( tex.width, tex.height );
		int curLod = 0;
		while( curLod < lod && size.x > minTextureSize && size.y > minTextureSize )
		{
			curLod++;
			size.x /= 2;
			size.y /= 2;
		}

		//Debug.Log( tex.name + ": lod " + lod + ", size " + size.x + ":" + size.y + ", (original " + tex.width + ":" + tex.height + ")" );
		return size;
	}
}
