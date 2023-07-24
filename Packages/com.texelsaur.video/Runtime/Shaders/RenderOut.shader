Shader "VideoTXL/RenderOut" {
	Properties {
		_MainTex ("MainTex", 2D) = "black" {}
		_MarginTex("Margin (RGB)", 2D) = "black" {}
		[Toggle] _ApplyGamma("Apply Gamma", Int) = 0
		[Toggle] _FlipY("Flip Y", Int) = 0
		_AspectRatio("Aspect Ratio", Float) = 1.777777
		_TexAspectRatio("Aspect Ratio Override", Float) = 0
		[Enum(Fit,0,Fit Height,1,Fit Width,2,Stretch,3)] _FitMode("Fit Mode", Int) = 0
	}

	SubShader {

		Pass {
			Lighting Off

			CGPROGRAM
			#pragma vertex CustomRenderTextureVertexShader
			#pragma fragment frag

			#include "UnityCustomRenderTexture.cginc"
			#include "VideoTXL.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			sampler2D _MarginTex;
			float4 _MarginTex_ST;

			int _ApplyGamma;
			int _FlipY;

			float4 frag(v2f_customrendertexture i) : SV_Target {
				float2 uv = float2(0, 0);
				float visibility = 0;
				TXL_ComputeScreenFit(i.globalTexcoord.xy, _MainTex_TexelSize.zw, uv, visibility);

				//float2 uv = i.globalTexcoord.xy;
				if (_FlipY)
					uv.y = 1 - uv.y;

				float2 muv = TRANSFORM_TEX(i.globalTexcoord.xy, _MarginTex);
				float4 margin = tex2D(_MarginTex, muv) * (1 - visibility);

				float4 color = tex2D(_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw);

				if (_ApplyGamma)
					color.rgb = GammaToLinearSpace(color.rgb);

				color = color * visibility + margin;

				return color;
			}
			ENDCG
		}
	}
}
