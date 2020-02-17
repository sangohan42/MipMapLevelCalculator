using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class UniformTextureGenerator : MonoBehaviour
{
    public bool saveOnDisk = false;
    public bool instantiateInScene = true;

    [SerializeField]
    private List<GameObject> _instantiatedGOs = new List<GameObject>();
    [SerializeField]
    private List<Texture> _savedTextures = new List<Texture>();

    public void Generate( Transform parent, BoxCollider spawningArea, int numberOfTextures, int numberOfCopy, int maxTextureSize )
    {
        if( numberOfTextures < 0 )
            return;

        for( int i = 0; i < numberOfTextures; i++ )
        {
            Color randomColor = new Color(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            );
            int randomTextureSize = Random.Range( 1, maxTextureSize );
            Texture2D uniformTexture = UniformTextureCreator.CreateUniformTexture( TextureFormat.ARGB32, randomColor, true, randomTextureSize, randomTextureSize);
            if( uniformTexture )
            {
                string name = randomColor + "_" + randomTextureSize;
                if( saveOnDisk )
                    Save( uniformTexture, name );
                if( instantiateInScene )
                    InstantiateInScene( uniformTexture, name, parent, spawningArea, numberOfCopy);
            }
            else
                Debug.Log("Couldn't create the uniform texture.");
        }
    }

    public void Save( Texture2D uniformTexture, string fileName )
    {
        System.IO.Directory.CreateDirectory("Assets/UniformTexture/GeneratedTextures");
        AssetDatabase.CreateAsset(uniformTexture, "Assets/UniformTexture/GeneratedTextures/" + fileName + ".asset");
        AssetDatabase.SaveAssets();
        _savedTextures.Add( uniformTexture );
    }

    public void InstantiateInScene(Texture2D uniformTexture, string gameobjectName, Transform parent, BoxCollider spawningArea, int copyNumber )
    {
        if (copyNumber < 0)
        {
            Debug.Log("Copy number is invalid. Cancelling instantiation.");
            return;
        }
        if (!spawningArea)
        {
            Debug.Log("Spawning area is null. Cancelling instantiation.");
            return;
        }
        if (!parent)
        {
            Debug.Log("Parent transform is null. Cancelling instantiation.");
            return;
        }

        List<GameObject> instantiatedGO = new List<GameObject>();

        for (int i = 0; i < copyNumber; i++)
        {
            Renderer renderer = GraphicUtils.InstantiateCubeWithStandardShader(parent);
            renderer.name += gameobjectName + "_" + i;
            renderer.sharedMaterial.mainTexture = uniformTexture;
            renderer.transform.position = GraphicUtils.RandomPointInBox(spawningArea.center, spawningArea.size);
            instantiatedGO.Add(renderer.gameObject);
        }
        _instantiatedGOs.AddRange(instantiatedGO);
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
            string pathToDelete = AssetDatabase.GetAssetPath(texture);
            AssetDatabase.DeleteAsset(pathToDelete);
        }
    }
}
