using System.Collections;
using System.Collections.Generic; //For list functionality
using UnityEditor;
using UnityEngine;

[AddComponentMenu("Noise/Noise Generator")]
public class NoiseGenerator : MonoBehaviour
{
    public ComputeTexture[] computeTextures2D;
    public ComputeTexture3D[] computeTextures3D;

    public bool saveOnDisk = false;
    public bool instantiateInScene = true;

    [SerializeField]
    private List<GameObject> _instantiatedGOs = new List<GameObject>();
    [SerializeField]
    private List<Texture> _savedTextures = new List<Texture>();

    public void Generate(Transform parent, BoxCollider spawningArea, int numberOfCopy )
    {
        foreach(ComputeTexture ct in computeTextures2D)
        {
            ct.CreateRenderTexture();
            ct.SetParameters();
            ct.SetTexture();
            ct.GenerateTexture();
            if( saveOnDisk )
                _savedTextures.Add(ct.SaveAsset());
            if( parent )
                _instantiatedGOs.AddRange(ct.InstantiateInScene( parent, spawningArea, numberOfCopy ));
        }
        foreach(ComputeTexture3D ct in computeTextures3D)
        {
            ct.CreateRenderTexture();
            ct.SetParameters();
            ct.SetTexture();
            ct.GenerateTexture();
            if( saveOnDisk )
                _savedTextures.Add(ct.SaveAsset());
            else if( instantiateInScene )
                _instantiatedGOs.AddRange(ct.InstantiateInScene( parent, spawningArea, numberOfCopy ));
        }
    }

    public void Delete()
    {
        DeleteInstantiatedGOs();
        DeleteSavedTextures();
    }

    private void DeleteInstantiatedGOs()
    {
        foreach (var go in _instantiatedGOs)
        {
            DestroyImmediate(go);
        }
    }

    private void DeleteSavedTextures()
    {
        foreach (var texture in _savedTextures)
        {
            string pathToDelete = AssetDatabase.GetAssetPath( texture );
            AssetDatabase.DeleteAsset( pathToDelete );
        }
    }
}

