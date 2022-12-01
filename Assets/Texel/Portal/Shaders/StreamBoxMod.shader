Shader "Unlit/StreamBoxMod"
{
	Properties
	{
		_ScreenTex("ScreenTex", 2D) = "black" {}
		_ShellTex("ShellTex", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque"  "Queue" = "Transparent+1999" "IgnoreProjector" = "True" "IsEmissive" = "true" "ForceNoShadowCasting" = "true" }

		Pass
		{
			Cull Front
			ZTest Always
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
		// make fog work
		#pragma multi_compile_fog

		#include "UnityCG.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			UNITY_FOG_COORDS(1)
			float4 vertex : SV_POSITION;
		};

		sampler2D _ScreenTex;
		float4 _ScreenTex_ST;
		sampler2D _ShellTex;
		float4 _ShellTex_ST;

		float _VRChatCameraMode;

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.uv, _ShellTex);
			UNITY_TRANSFER_FOG(o,o.vertex);

			if (_VRChatCameraMode == 0 || _VRChatCameraMode == 3)
				o.vertex = float4(0, 0, 0, 0);

			return o;
		}

		fixed4 frag(v2f i, uint face : SV_IsFrontFace) : SV_Target
		{
			float2 grabScreenPosNorm = i.vertex.xy / _ScreenParams.xy;
			fixed4 c;
			if (face > 0)
				return tex2D(_ShellTex, i.uv);
			else
				return tex2D(_ScreenTex, grabScreenPosNorm.xy);
		}
		ENDCG
	}


	Pass
	{
		Cull Back
		ZTest LEqual
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _ScreenTex;
			float4 _ScreenTex_ST;
			sampler2D _ShellTex;
			float4 _ShellTex_ST;

			float _VRChatCameraMode;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _ShellTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				//if (_VRChatCameraMode == 1 || _VRChatCameraMode == 2)
				//	o.vertex = float4(0, 0, 0, 0);

				return o;
			}

			fixed4 frag(v2f i, uint face : SV_IsFrontFace) : SV_Target
			{
				float2 grabScreenPosNorm = i.vertex.xy / _ScreenParams.xy;
				if (face > 0)
					return tex2D(_ShellTex, i.uv);
				else
					return tex2D(_ScreenTex, grabScreenPosNorm.xy);
			}
			ENDCG
		}
	}
}
