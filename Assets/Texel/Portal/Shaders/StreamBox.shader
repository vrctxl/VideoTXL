Shader "Unlit/StreamBox"
{
    Properties
    {
		_ScreenTex("ScreenTex", 2D) = "white" {}
    }
    SubShader
    {
		Tags { "RenderType" = "Opaque"  "Queue" = "Transparent+2000" "IgnoreProjector" = "True" "IsEmissive" = "true" "ForceNoShadowCasting" = "true" }
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
				float4 screenPos : TEXCOORD2;
            };

            sampler2D _ScreenTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = o.vertex;
				// o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

			inline float4 AComputeGrabScreenPos(float4 pos)
			{
#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
#else
				float scale = 1.0;
#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = (pos.y - o.y) * _ProjectionParams.x * scale + o.y;
				return o;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				float4 screenPos = float4(i.screenPos.xyz, i.screenPos.w + 0.00000000001);
				float4 grabScreenPos = AComputeGrabScreenPos(screenPos);
				half4 grabScreenPosNorm = grabScreenPos / grabScreenPos.w;
				grabScreenPosNorm = grabScreenPosNorm * .5 + .5;
				grabScreenPosNorm.y = 1 - grabScreenPosNorm.y;
                
				// sample the texture
                fixed4 col = tex2D(_ScreenTex, grabScreenPosNorm.xy);
                return col;
            }
            ENDCG
        }
    }
}
