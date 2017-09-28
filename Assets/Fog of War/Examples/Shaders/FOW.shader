Shader "FX/FOW" {
	Properties {
		_FogTex0 ("Fog 0", 2D) = "white" {}
		_FogTex1 ("Fog 1", 2D) = "white" {}
		_Unexplored ("Unexplored Color", Color) = (0.05, 0.05, 0.05, 0.05)
		_Explored ("Explored Color", Color) = (0.35, 0.35, 0.35, 0.35)
	}

	Category {

	// We must be transparent, so other objects are drawn before this one.
		Tags { "Queue"="Transparent" "RenderType"="Opaque" }

		SubShader {

			Pass {
				ZTest Off
				Cull Off
				ZWrite Off
				Blend One Zero
				//Fog { Mode off }
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord: TEXCOORD0;
			};

			struct v2f {
				float4 vertex : POSITION;
				float4 uvgrab : TEXCOORD0;
				float3 worldPos: TEXCOORD4;
			};

			sampler2D _FogTex0;
			sampler2D _FogTex1;
			uniform half4 _Unexplored;
			uniform half4 _Explored;
			uniform float4 _Params;
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
				o.uvgrab.zw = o.vertex.zw;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			sampler2D _GrabBlurTexture;

			half4 frag (v2f i) : SV_Target
			{
				half4 original = tex2Dproj (_GrabBlurTexture, UNITY_PROJ_COORD(i.uvgrab));
				float2 uv = i.worldPos.xz * _Params.z -  _Params.xy;
				half4 fog = lerp(tex2D(_FogTex0, uv), tex2D(_FogTex1, uv), _Params.w);
				return lerp(lerp(original * _Unexplored, original * _Explored, fog.g), original, fog.r);
			}
			ENDCG
			}
		}
	}
}
