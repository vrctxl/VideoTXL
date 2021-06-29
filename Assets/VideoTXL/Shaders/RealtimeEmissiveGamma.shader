// Copy of VRCSDK Video/RealtimeEmissiveGamma
// Aspect ratio correction by Merlin from USharpVideo
// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "VideoTXL/RealtimeEmissiveGamma" {
	Properties{
		_MainTex("Emissive (RGB)", 2D) = "white" {}
		_MarginTex("Margin (RGB)", 2D) = "black" {}
		_Emission("Emission Scale", Float) = 1
		_AspectRatio("Aspect Ratio", Float) = 1.777777
		[Toggle(APPLY_GAMMA)] _ApplyGamma("Apply Gamma", Float) = 0
		[Toggle(_)] _IsAVProInput("Is AV Pro Input", Int) = 0
		[Toggle(_)] _InvertAVPro("Invert AVPro", Int) = 0
	}

	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#pragma shader_feature _EMISSION
		#pragma multi_compile APPLY_GAMMA_OFF APPLY_GAMMA

		fixed _Emission;
		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		sampler2D _MarginTex;
		float4 _MarginTex_ST;

		struct Input {
			float2 uv_MainTex;
		};

		float _AspectRatio;
		int _IsAVProInput;
		int _InvertAVPro;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
			UNITY_INSTANCING_BUFFER_END(Props)

			void surf(Input IN, inout SurfaceOutputStandard o) {
				float2 res = _MainTex_TexelSize.zw;
				float curAspectRatio = res.x / res.y;

				float2 uv = float2(IN.uv_MainTex.x, IN.uv_MainTex.y);
				float visibility = 1;

				if (abs(curAspectRatio - _AspectRatio) > .001) {
					float2 normRes = float2(res.x / _AspectRatio, res.y);
					float2 correction;

					if (normRes.x > normRes.y)
						correction = float2(1, normRes.y / normRes.x);
					else
						correction = float2(normRes.x / normRes.y, 1);

					uv = ((uv - 0.5) / correction) + 0.5;

					float2 uvPadding = (1 / res) * 0.1;
					float2 uvFwidth = fwidth(uv.xy);
					float2 maxf = smoothstep(uvFwidth + uvPadding + 1, uvPadding + 1, uv.xy);
					float2 minf = smoothstep(-uvFwidth - uvPadding, -uvPadding, uv.xy);

					visibility = maxf.x * maxf.y * minf.x * minf.y;
				}

				// emissive comes from texture
				//float2 newUV = float2(IN.uv_MainTex.x, IN.uv_MainTex.y);
				fixed4 margin = fixed4(0, 0, 0, 0);

				if (_IsAVProInput) {
					if (_InvertAVPro)
						uv.y = 1 - uv.y;
					
					float2 muv = TRANSFORM_TEX(float2(IN.uv_MainTex.x, IN.uv_MainTex.y), _MarginTex);
					margin = tex2D(_MarginTex, muv) * _Emission * (1 - visibility);
				}

				fixed4 e = tex2D(_MainTex, uv);
				o.Albedo = fixed4(0,0,0,0);
				o.Alpha = e.a;

			#if APPLY_GAMMA
				if (_IsAVProInput)
					e.rgb = pow(e.rgb,2.2);
			#endif
				o.Emission = e * _Emission * visibility + margin;
				o.Metallic = 0;
				o.Smoothness = 0;
			}
		ENDCG
	}

	FallBack "Diffuse"
	CustomEditor "RealtimeEmissiveGammaGUI"
}
