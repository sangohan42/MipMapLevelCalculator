using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TexturePackerCreator
{
    Dictionary<AllTextureParser.TextureType, IPacker> _atlas = new Dictionary<AllTextureParser.TextureType, IPacker>();
    public event Action<string> OnPackerError;

    /// <summary>
    /// This method does everything that is required to maintain atlases up to date
    /// </summary>
    public void ParseSceneAndCreateAtlases( Transform rootTransform )
    {
        Debug.Log( "ParseSceneAndCreateAtlases" );

        Renderer[] sceneAllRenderers = rootTransform.GetComponentsInChildren<Renderer>( false );

        foreach( AllTextureParser.TextureType textureType in Enum.GetValues(typeof( AllTextureParser.TextureType ) ) )
        {
            Dictionary<Texture2D, List<Material>> materialsByTexture = AllTextureParser.GetVisibleMaterialsByTextureForTextureType( sceneAllRenderers, textureType, false );

            if( materialsByTexture.Count > 0 )
            {
                List<Map> maps = Map.ConvertTexturesToMaps( materialsByTexture.Keys.ToList() );
                if( !_atlas.ContainsKey( textureType ) )
                {
                    Debug.Log( "Try creating an atlas with " + maps.Count + " texture of type " + textureType );
                    _atlas[ textureType ] = new Packer( maps, SystemInfo.maxTextureSize, textureType, rootTransform );
                }
                else
                {
                    //Debug.Log("Processing atlas " + textureType + " inserting in packer " + packer.ToString());
                    //insert new textures in packer
                    foreach( var map in maps )
                    {
                        if( !_atlas[ textureType ].Add( map ) )
                            break;
                    }
                }

                IPacker packer = _atlas[ textureType ];
                if( !packer.SuccessfullyPacked )
                {
                    OnPackerError?.Invoke("Could not fit textures in atlas " + textureType);
                    return;
                }
            }
            else
            {
                Debug.Log( "No Texture found for texture type = " + textureType );
            }
        }
    }

    /// <summary>
    /// Draw a packed texture on screen for debugging purpose
    /// </summary>
    /// <param name="type"></param>
    public void DrawForDebug( AllTextureParser.TextureType type )
    {
        if( !_atlas.ContainsKey( type ) )
            return;

        IPacker p = _atlas[ type ];
        Texture2D tex = p.GetTexture();
        float ratio = tex.width * 1f / tex.height;
        int size = 800;
        GUI.DrawTexture( new Rect( 0, 0, ratio >= 1 ? size : size * ratio, ratio >= 1 ? size / ratio : size ), tex );
    }

    public void OnDrawGizmos( AllTextureParser.TextureType type )
    {
        if( !_atlas.ContainsKey( type ) )
            return;
        Packer p = _atlas[ type ] as Packer;
        p.OnDrawGizmos();
    }
}