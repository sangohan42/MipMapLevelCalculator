using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LODRenderer
{
	private readonly Dictionary<Material, Shader> _replacedShaders;
	public Shader LODShader { get; }

	#region Public methods
	public LODRenderer()
	{
		LODShader = Shader.Find( "CalculateLOD" );
		if( LODShader == null )
			throw new FileNotFoundException( "LODRenderer creation error: cannot find 'CalculateLOD' shader" );

		_replacedShaders = new Dictionary<Material, Shader>();
	}

	public RenderTexture Render( Camera cam, int renderWidth, int renderHeight )
	{
		RenderTexture rt = new RenderTexture( renderWidth, renderHeight, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear );
		rt.Create();
		rt.filterMode = FilterMode.Point;
		rt.antiAliasing = 1;

		Color prevCamColor = cam.backgroundColor;
		CameraClearFlags prevClearFlags = cam.clearFlags;

		cam.backgroundColor = new Color( 0, 0, 0, 0 );
		cam.clearFlags = CameraClearFlags.Color;
		RenderTexture.active = rt;
		cam.targetTexture = rt;
		cam.Render();

		cam.backgroundColor = prevCamColor;
		cam.clearFlags = prevClearFlags;

		RenderTexture.active = null;
		cam.targetTexture = null;

		return rt;
	}

	public void SetupLODShader( Material mat, Texture2D targetTex, float texId )
	{
		_replacedShaders.Add( mat, mat.shader );
		mat.shader = LODShader;
		mat.SetTexture( "_TargetTex", targetTex );
		mat.SetFloat( "_TextureId", texId );
		mat.SetInt( "_Discard", 0 );
	}

	public void SetupLODShaderWithDiscard( Material mat )
	{
		_replacedShaders.Add( mat, mat.shader );
		mat.shader = LODShader;
		mat.SetInt( "_Discard", 1 );
	}

	public void RestoreAllReplacedShaders()
	{
		foreach( KeyValuePair<Material, Shader> pair in _replacedShaders )
			pair.Key.shader = pair.Value;
	}

	public bool IsRenderedWithLOD( Material mat )
	{
		return _replacedShaders.ContainsKey( mat );
	}
	#endregion
}
