using System.Collections.Generic;
using UnityEngine;

public class LODProcessor
{
	private readonly LODRenderer _lodRenderer;
	private readonly MaxLODComputer _maxLodComputer;
	private readonly int _renderWidth;
	private readonly int _renderHeight;


	public LODProcessor( int renderWidth, int renderHeight )
	{
		_maxLodComputer = new MaxLODComputer();
		_renderHeight = renderHeight;
		_renderWidth = renderWidth;
		_lodRenderer = new LODRenderer();
	}

	public Dictionary<Texture2D, Vector2Int> CalculateTexturesLOD( AllTextureParser.TextureType textureType, Transform rootNode, bool discardTransparent = true )
	{
        Renderer[] allSceneRenderers = rootNode.GetComponentsInChildren<Renderer>(false);
        Dictionary<Texture2D, List<Material>> materialsByTexture = AllTextureParser.GetVisibleMaterialsByTextureForTextureType( allSceneRenderers, textureType, discardTransparent );
        Dictionary<Texture2D, Vector2Int> textureSizes = new Dictionary<Texture2D, Vector2Int>();

		if( materialsByTexture.Count == 0 )
		{
			Debug.LogWarning( "No suitable objects found for LOD calculation" );
			return textureSizes;
		}

		//////////////
		// 1a.  Prepare all "relevant" materials for LOD rendering 
		int i = 0;
		int texturesCount = materialsByTexture.Count;
		foreach( KeyValuePair<Texture2D, List<Material>> pair in materialsByTexture )
		{
			float textureId = _maxLodComputer.ConvertTextureId( i++, texturesCount );
			foreach( Material mat in pair.Value )
				_lodRenderer.SetupLODShader( mat, pair.Key, textureId );
		}

        //////////////
        // 1b.   Exclude all other materials from render  
        // - materials without the required type of texture 
        // - materials with transparency
        // - materials that share the required type of texture with other transparent materials
        foreach ( Renderer r in allSceneRenderers )
		{
			if( !r.enabled )
				continue;

			// Discard all scene materials that were not already setup in the previous phase
			foreach( Material mat in r.sharedMaterials )
			{
				if( mat != null && !_lodRenderer.IsRenderedWithLOD( mat ) )
				{
					_lodRenderer.SetupLODShaderWithDiscard( mat );
				}
			}
		}

		//////////////
		// 2.   Render the scene. All visible objects have the LOD shader setup now
		RenderTexture src = _lodRenderer.Render( Camera.main, _renderWidth, _renderHeight );

		/////////////
		// 3.   Calculate the LOD for all visible objects
		int[] lodByTexId = _maxLodComputer.GetMaxLOD( src, materialsByTexture.Count );

		/////////////
		// 4.   Calculate sizes from lod
		int texId = 0;
		foreach( Texture2D tex in materialsByTexture.Keys )
		{
			int lod = lodByTexId[ texId++ ];
			if( lod == MaxLODComputer.OCCLUDED_TEX_LOD )
				textureSizes.Add( tex, new Vector2Int( 1, 1 ) );
			else
				textureSizes.Add( tex, GraphicUtils.GetSizeFromLOD( tex, lod ) );
		}

        // Clean-up and restore scene state
		src.Release();
		Object.Destroy( src );
		_lodRenderer.RestoreAllReplacedShaders();

		return textureSizes;
	}
}
