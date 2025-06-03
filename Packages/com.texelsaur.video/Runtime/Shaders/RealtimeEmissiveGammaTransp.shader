// Copy of VRCSDK Video/RealtimeEmissiveGamma
// Aspect ratio correction by Merlin from USharpVideo
// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "VideoTXL/RealtimeEmissiveGammaTransp" {
	Properties{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Emissive (RGB)", 2D) = "black" {}
		_MarginTex("Margin (RGB)", 2D) = "black" {}
		_Emission("Emission Scale", Float) = 1
		_AspectRatio("Aspect Ratio", Float) = 1.777777
		[HideInInspector] _TexAspectRatio("Aspect Ratio Override", Float) = 0
		[Enum(Fit,0,Fit Height,1,Fit Width,2,Stretch,3)] _FitMode("Fit Mode", Int) = 0
		[Toggle] _ApplyGammaAVPro("Apply Gamma", Int) = 0
		[Toggle] _IsAVProInput("Is AV Pro Input", Int) = 0
		[Toggle] _InvertAVPro("Invert AVPro", Int) = 0
	}

	SubShader{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" }
		LOD 200

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows addshadow alpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#pragma shader_feature _EMISSION
		#pragma multi_compile_local APPLY_GAMMA_OFF APPLY_GAMMA

		#include "Packages/com.texelsaur.video/Runtime/Shaders/VideoTXL.cginc"

		fixed _Emission;
		float4 _Color;
		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		sampler2D _MarginTex;
		float4 _MarginTex_ST;

		struct Input {
			float2 uv_MainTex;
		};

		int _IsAVProInput;
		int _InvertAVPro;
		int _ApplyGammaAVPro;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
			UNITY_INSTANCING_BUFFER_END(Props)

			void surf(Input IN, inout SurfaceOutputStandard o) {
				float2 uv = float2(0, 0);
				float visibility = 0;
				TXL_ComputeScreenFit(IN.uv_MainTex.xy, _MainTex_TexelSize.zw, uv, visibility);

				if (_IsAVProInput && _InvertAVPro)
					uv.y = 1 - uv.y;

				float2 muv = TRANSFORM_TEX(float2(IN.uv_MainTex.x, IN.uv_MainTex.y), _MarginTex);
				float4 margin = tex2D(_MarginTex, muv) * _Emission * (1 - visibility);

				fixed4 e = tex2D(_MainTex, uv) * _Color;
				o.Albedo = fixed4(0,0,0,0);
				o.Alpha = e.a;

				if (_IsAVProInput && _ApplyGammaAVPro)
					e.rgb = pow(e.rgb,2.2);

				o.Emission = e * _Emission * visibility + margin;
				o.Metallic = 0;
				o.Smoothness = 0;
			}
		ENDCG
	}

	FallBack "Diffuse"
	CustomEditor "RealtimeEmissiveGammaGUI"
}
