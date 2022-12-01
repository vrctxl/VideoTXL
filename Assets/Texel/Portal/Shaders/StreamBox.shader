Shader "Unlit/StreamBox"
{
	Properties
	{
		_ScreenTex("ScreenTex", 2D) = "black" {}
		_ShellTex("ShellTex", 2D) = "white" {}
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque"  "Queue" = "Transparent+1999" "IgnoreProjector" = "True" "IsEmissive" = "true" "ForceNoShadowCasting" = "true" }
			Cull Off

			ZTest Always
			ZWrite Off

			Pass
			{
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

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _ShellTex);
					UNITY_TRANSFER_FOG(o,o.vertex);
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
