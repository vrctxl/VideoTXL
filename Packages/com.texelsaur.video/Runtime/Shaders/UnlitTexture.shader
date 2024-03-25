// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit shader. Simplest possible textured shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "VideoTXL/Unlit" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_MarginTex("Margin (RGB)", 2D) = "black" {}
		_AspectRatio("Aspect Ratio", Float) = 1.777777
		[HideInInspector] _TexAspectRatio("Aspect Ratio Override", Float) = 0
		[Enum(Fit,0,Fit Height,1,Fit Width,2,Stretch,3,Fill,4)] _FitMode("Fit Mode", Int) = 0
		[Toggle] _ApplyGammaAVPro("Apply Gamma", Int) = 0
		[Toggle] _IsAVProInput("Is AV Pro Input", Int) = 0
		[Toggle] _InvertAVPro("Invert AVPro", Int) = 0
	}

	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#pragma multi_compile_fog

				#include "UnityCG.cginc"
				#include "VideoTXL.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					//float visibility : FLOAT;
					UNITY_FOG_COORDS(1)
					UNITY_VERTEX_OUTPUT_STEREO
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _MainTex_TexelSize;

				sampler2D _MarginTex;
				float4 _MarginTex_ST;

				int _IsAVProInput;
				int _InvertAVPro;
				int _ApplyGammaAVPro;

				v2f vert(appdata_t v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					float2 uv = float2(0, 0);
					float visibility = 0;
					TXL_ComputeScreenFit(i.texcoord.xy, _MainTex_TexelSize.zw, uv, visibility);

					if (_IsAVProInput && _InvertAVPro)
						uv.y = 1 - uv.y;

					float2 muv = TRANSFORM_TEX(i.texcoord.xy, _MarginTex);
					float4 margin = tex2D(_MarginTex, muv) * (1 - visibility);

					float4 color = tex2D(_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw);

					if (_IsAVProInput && _ApplyGammaAVPro)
						color.rgb = GammaToLinearSpace(color.rgb);

					color = color * visibility + margin;

					UNITY_APPLY_FOG(i.fogCoord, color);
					UNITY_OPAQUE_ALPHA(color.a);
					return color;
				}
			ENDCG
		}
	}
}