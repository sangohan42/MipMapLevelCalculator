using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[AddComponentMenu("Noise/Compute Texture")]
public class ComputeTexture : MonoBehaviour {
	//-------------------------------------------------------------------------------------------------------------------
	// Public Structs
	//-------------------------------------------------------------------------------------------------------------------
    [System.Serializable]
    public struct IntVector3{ public int x,y,z; }

	//[System.Serializable]
	//public struct ComputeParameter<T>{ public string name; public T value; }
	//Couldn't get this to serialize with unity's inspector so I'm hardcoding 
	//it for now until I can figure out some solution
    [System.Serializable]
    public struct ComputeParameterFloat
    {
        public string name;
        public float value;
    }

	[System.Serializable]
	public struct ComputeRWTexture{ 
		public string name;
		[HideInInspector] 
		public RenderTexture rt;
	}

	//-------------------------------------------------------------------------------------------------------------------
	// Public Variables
	//-------------------------------------------------------------------------------------------------------------------
	public string assetName;
    public string kernelName;
	public ComputeRWTexture rwTexture;
    public int squareResolution;
	public ComputeParameterFloat[] parameters;
	public IntVector3 computeThreads;
	public ComputeShader computeShader;

	//-------------------------------------------------------------------------------------------------------------------
	// Generator Functions
	//-------------------------------------------------------------------------------------------------------------------
	public virtual void GenerateTexture(){
		int kernel = computeShader.FindKernel(kernelName);
		computeShader.Dispatch(kernel, 
			squareResolution/computeThreads.x, 
			squareResolution/computeThreads.y, 1);
	}

    public virtual void CreateRenderTexture(){
        RenderTexture rt = new RenderTexture(squareResolution, squareResolution, 24, RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;
        rt.Create();
        rwTexture.rt = rt;
    }

	//-------------------------------------------------------------------------------------------------------------------
	// Compute Shader Getters/Setters
	//-------------------------------------------------------------------------------------------------------------------
	public void SetParameters(){
		/*Currently I have this hardcoded for float parameters,
		**however it very easily can be modified/extended.
		**If this becomes used more in the future, a full on
		**editor window could allow for more modularity.*/
		foreach(ComputeParameterFloat param in parameters)
			computeShader.SetFloat(param.name, param.value);
	}

	public void SetTexture(){
		int kernel = computeShader.FindKernel(kernelName);
		computeShader.SetTexture(kernel, rwTexture.name, rwTexture.rt);
	}

	//-------------------------------------------------------------------------------------------------------------------
	// Save/Utility Functions
	//-------------------------------------------------------------------------------------------------------------------
	protected Texture2D ConvertFromRenderTexture(RenderTexture rt){
		Texture2D output = new Texture2D(squareResolution, squareResolution);
		RenderTexture.active = rt;
		output.ReadPixels(new Rect(0,0,squareResolution, squareResolution), 0, 0);
		output.Apply();
		return output;
	}
	
	public virtual Texture SaveAsset(){
		Texture2D output = ConvertFromRenderTexture(rwTexture.rt);
        System.IO.Directory.CreateDirectory("Assets/Noise/GeneratedTextures");
        AssetDatabase.CreateAsset(output, "Assets/Noise/GeneratedTextures/" + assetName + ".asset");
        AssetDatabase.SaveAssets();
        return output;
    }

    public List<GameObject> InstantiateInScene(Transform parent, BoxCollider spawningArea, int copyNumber )
    {
        if (copyNumber < 0)
        {
            Debug.Log("Copy number is invalid. Cancelling instantiation.");
            return null;
        }
        if (!spawningArea)
        {
            Debug.Log("Spawning area is null. Cancelling instantiation.");
            return null;
        }
        if (!parent)
        {
            Debug.Log("Parent transform is null. Cancelling instantiation.");
            return null;
        }

        List<GameObject> instantiatedGO = new List<GameObject>();

        Texture2D outputTexture = ConvertFromRenderTexture(rwTexture.rt);
        for (int i = 0; i < copyNumber; i++)
        {
            Renderer renderer = GraphicUtils.InstantiateCubeWithStandardShader(parent);
            renderer.name += assetName + "_" + i;
            renderer.sharedMaterial.mainTexture = outputTexture;
            renderer.transform.position = GraphicUtils.RandomPointInBox( spawningArea.center, spawningArea.size);
            instantiatedGO.Add(renderer.gameObject);
        }
        return instantiatedGO;
    }
}
