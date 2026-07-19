Shader "VideoTXL/360 Panoramic Unlit"
{
    Properties
    {
        _MainTex ("Video Texture", 2D) = "black" {}

        [Toggle] _FlipU ("Flip U", Float) = 0
        [Toggle] _FlipV ("Flip V", Float) = 0

        _RotationY ("Y-Axis Rotation (Degrees)", Range(0,360)) = 0
        _Exposure ("Exposure", Range(0,8)) = 1

        [Enum(Off,0,Front,1,Back,2)]
        _CullMode ("Cull Mode", Float) = 1

        [Enum(Off,0,On,1)]
        _ZWrite ("ZWrite", Float) = 0

        [Toggle(VIEWDIR_MODE)]
        _UseViewDir ("Use View Direction Mapping", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }

        Cull [_CullMode]
        ZWrite [_ZWrite]
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            #pragma shader_feature VIEWDIR_MODE

            #include "UnityCG.cginc"
            #include "Unlit360.cginc"

            sampler2D _MainTex;

            float _FlipU;
            float _FlipV;
            float _RotationY;
            float _Exposure;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;

                #ifdef VIEWDIR_MODE
                float3 viewDir : TEXCOORD1;
                #endif

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;

                #ifdef VIEWDIR_MODE
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = worldPos - _WorldSpaceCameraPos;
                #endif

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float yawOffset = _RotationY / 360.0;
                float2 uv;

                #ifdef VIEWDIR_MODE
                    uv = Unlit360_EquirectUV(i.viewDir);
                #else
                    uv = i.uv;
                #endif

                uv = Unlit360_AdjustUV(uv, yawOffset, _FlipU, _FlipV);

                fixed4 col = Unlit360_SampleEquirectGrad(_MainTex, uv);
                col.rgb *= _Exposure;

                return col;
            }

            ENDCG
        }
    }
}