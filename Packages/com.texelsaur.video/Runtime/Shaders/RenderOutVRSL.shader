Shader "VideoTXL/RenderOutVRSL" {
	Properties {
		_MainTex ("MainTex", 2D) = "black" {}
		[HideInInspector] _BufferTex("BufferTex", 2D) = "black" {}
		[Toggle] _ApplyGamma("Apply Gamma", Int) = 0
		[Toggle] _FlipY("Flip Y", Int) = 0
		_AspectRatio("Aspect Ratio", Float) = 1.777777
		[Enum(Linear,0,sRGB,1)] _TargetColorSpace("Target Color Space", Int) = 0
		[Toggle] _DoubleBuffered ("Double Buffered", Int) = 0
		[Toggle] _Horizontal ("Horizontal", Int) = 1
		_OffsetScale("Offset Scale", Vector) = (0, 0, 0, 0)
	}

	SubShader {

		Pass {
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

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
				float2 texcoord1 : TEXCOORD1;
				UNITY_FOG_COORDS(1)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			sampler2D _BufferTex;
			int _ApplyGamma;
			int _FlipY;
			int _DoubleBuffered;
			float4 _OffsetScale;
			int _Horizontal;
			int _TargetColorSpace;

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);

				if (_Horizontal)
					o.texcoord = float2(v.texcoord.y, 1 - v.texcoord.x);
				else
					o.texcoord = v.texcoord;

				if (_FlipY)
					o.texcoord.y = 1 - o.texcoord.y;

				o.texcoord1.xy = v.texcoord.xy;

				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			float4 frag(v2f i) : SV_Target {
				float2 offset = _OffsetScale.xy;
				float2 scale = _OffsetScale.zw;

				if (_FlipY)
					offset.y = 1 - offset.y;

				float dmxW = (208.0 / 1920) * (_AspectRatio / 1.777777);
				float dmxH = 1;
				if (_Horizontal) {
					dmxW = 1;
					dmxH = (208.0 / 1080) * (_AspectRatio / 1.777777);
				}

				scale.x *= dmxW;
				scale.y *= dmxH;

				offset.x *= (1 - scale.x);
				offset.y *= (1 - scale.y);

				float2 uv = i.texcoord.xy * scale + offset;

				float4 color = tex2D(_MainTex, uv);

				if (_DoubleBuffered) {
					float4 prev = tex2D(_BufferTex, i.texcoord1.xy);
					color.rgb = lerp(prev, color, color.a);
				}

				if (_ApplyGamma && _TargetColorSpace == 1)
					color.rgb = GammaToLinearSpace(color.rgb);
				else if (!_ApplyGamma && _TargetColorSpace == 0)
					color.rgb = LinearToGammaSpace(color.rgb);

				return color;
			}
			ENDCG
		}
	}
}
