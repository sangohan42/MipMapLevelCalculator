using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Map : IEquatable<Map>
{
    [System.Serializable]
    public class MapMetaData
    {
        public int MipMapWidth;
        public int MipMapHeight;
    }

    public Map( Texture2D texture )
    {
        if (texture == null)
            throw new ArgumentException("null texture");

        _texture = texture;
        MetaData.MipMapWidth = _texture.width;
        MetaData.MipMapHeight = _texture.height;
    }

    private readonly Texture2D _texture;
    public Texture2D Texture { get { return _texture; } }

    public MapMetaData MetaData { get; set; } = new MapMetaData();

    public override bool Equals(object other)
    {
        return Equals(other as Map);
    }

    public bool Equals(Map other)
    {
        if (other == null)
            return false;

        if (object.ReferenceEquals(this, other))
            return true;

        return Texture.Equals(other.Texture);
    }

    public override int GetHashCode()
    {
        int textureHashCode = Texture ? Texture.GetHashCode() : 1;
        return textureHashCode * 7;
    }

    public static bool operator ==(Map map1, Map map2)
    {
        if (map1 is null)
            return map2 is null;
        return map1.Equals(map2);
    }

    public static bool operator !=(Map map1, Map map2)
    {
        if (map1 is null)
            return map2 is null;
        return !map1.Equals(map2);
    }

    public static List<Map> ConvertTexturesToMaps(List<Texture2D> textures)
    {
        List<Map> maps = new List<Map>();
        foreach (var texture in textures)
        {
            if (texture)
                maps.Add(new Map(texture));
        }

        return maps;
    }
}