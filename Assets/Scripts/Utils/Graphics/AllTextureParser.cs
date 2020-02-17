using System;
using System.Collections.Generic;
using UnityEngine;

public static class AllTextureParser
{
    public enum TextureType
    {
        COLOR,
        GLOSS_MAP,
        AO,
        NORMAL
    }

    public static List<string> ConvertTextureTypeToShaderProperties(TextureType textureType)
    {
        List<string> matchingShaderProperties = new List<string>();

        switch (textureType)
        {
            case TextureType.COLOR:
                matchingShaderProperties.Add("_MainTex");
                break;
            case TextureType.GLOSS_MAP:
                matchingShaderProperties.Add("_SpecGlossMap");
                matchingShaderProperties.Add("_MetallicGlossMap");
                break;
            case TextureType.AO:
                matchingShaderProperties.Add("_Occlusion");
                break;
            case TextureType.NORMAL:
                matchingShaderProperties.Add("_BumpMap");
                break;
        }

        return matchingShaderProperties;
    }

    public static Dictionary<Texture2D, List<Material>> GetVisibleMaterialsByTextureForTextureType(Renderer[] renderers, TextureType textureType, bool excludeTransparent = true)
    {
        List<string> texToLookFor = ConvertTextureTypeToShaderProperties(textureType);

        List<Material> processedMaterials = new List<Material>();
        Dictionary<Texture2D, List<Material>> materialsSharingTexture = new Dictionary<Texture2D, List<Material>>();
        List<Texture2D> texturesUsedByTransparentMaterials = new List<Texture2D>();

        foreach (Renderer r in renderers)
        {
            if (!r.enabled || !r.isVisible)
                continue;

            foreach (Material m in r.sharedMaterials)
            {
                if (m == null || processedMaterials.Contains(m))
                    continue;

                string textureProperty = null;
                foreach (string texType in texToLookFor)
                {
                    if (m.HasProperty(texType))
                    {
                        textureProperty = texType;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(textureProperty))
                    continue;

                processedMaterials.Add(m);

                Texture2D tex = m.GetTexture(textureProperty) as Texture2D;

                if (tex != null)
                {
                    // add all transparent materials to the list, but mark it for deletion later
                    // cannot exclude them directly in case they share a texture with a non-transparent material 
                    // which could pop later in the list and add the texture after all
                    if (MaterialHasTransparency(m))
                        texturesUsedByTransparentMaterials.Add(tex);

                    if (!materialsSharingTexture.TryGetValue(tex, out List<Material> mats))
                        materialsSharingTexture.Add(tex, new List<Material> { m });
                    else if (!mats.Contains(m))
                        mats.Add(m);
                }
            }
        }

        if (excludeTransparent)
            foreach (Texture2D texToExclude in texturesUsedByTransparentMaterials)
                materialsSharingTexture.Remove(texToExclude); // Removes if it has it. Only Exception is argument null, which is pre-checked above

        return materialsSharingTexture;
    }

    public static bool MaterialHasTransparency(Material mat)
    {
        return mat.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON") || mat.IsKeywordEnabled("_ALPHATEST_ON") || mat.IsKeywordEnabled("_ALPHABLEND_ON") || mat.shader.name.ToLower().Contains("transparent") || mat.shader.name.ToLower().Contains("cutout");
    }
}