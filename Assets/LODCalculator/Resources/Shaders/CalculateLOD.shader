Shader "CalculateLOD" {
	Properties{
		_TargetTex("Texture", 2D) = "white" { }
		_TextureId("Texture Id", Float) = 0
		_Discard("Discard transparent", Int) = 0
	}
	SubShader{
		ZWrite On
		ZTest LEqual
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc"
			
			#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE)
				#define UNITY_LOD_TEX2D(tex,coord) tex.CalculateLevelOfDetailUnclamped (sampler##tex,coord)
			#else
				// Just match the type i.e. define as a float value
				#define UNITY_LOD_TEX2D(tex,coord) float(0)
			#endif


			Texture2D _TargetTex;
			SamplerState sampler_TargetTex;
			float4 _TargetTex_TexelSize;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			float4 _TargetTex_ST;
			float _TextureId;
			int _Discard;

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _TargetTex);
				
				return o;
			}
			float4 frag(v2f i) : SV_Target
			{
				if( _Discard > 0 )
					discard;

				float calcultedLOD = UNITY_LOD_TEX2D(_TargetTex, i.uv);
				float lodScale = 0.1;
				return float4( calcultedLOD * lodScale, _TextureId, lodScale, 1 );
			}
			ENDCG
		}
	}
}