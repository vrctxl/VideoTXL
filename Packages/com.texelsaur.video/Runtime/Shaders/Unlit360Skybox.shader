Shader "VideoTXL/360 Panoramic Skybox"
{
    Properties
    {
        _MainTex ("Video Texture", 2D) = "black" {}

        [Toggle] _FlipU ("Flip U", Float) = 0
        [Toggle] _FlipV ("Flip V", Float) = 0

        _RotationY ("Y-Axis Rotation (Degrees)", Range(0,360)) = 0
        _Exposure ("Exposure", Range(0,8)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }

        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float yawOffset = _RotationY / 360.0;

                float2 uv = Unlit360_EquirectUV(i.dir);
                uv = Unlit360_AdjustUV(uv, yawOffset, _FlipU, _FlipV);

                fixed4 col = Unlit360_SampleEquirectGrad(_MainTex, uv);
                col.rgb *= _Exposure;

                return col;
            }

            ENDCG
        }
    }

    FallBack Off
}