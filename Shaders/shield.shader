Shader "SaccFlight/shield" {
    Properties {
      _MainTex ("Texture", 2D) = "white" {}
      _Cube ("Cubemap", CUBE) = "" {}
	    _RimColor ("Rim Color", Color) = (0.26,0.19,0.16,0.0)
      _RimPower ("Rim Power", Range(0.01,8.0)) = 3.0
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            ZWrite Off//
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            sampler2D _Emission;
            samplerCUBE _Cube;
	        float4 _RimColor;
            float _RimPower;
            struct VertexInput {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
		        float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float2 uv0 : TEXCOORD0;
		        float3 normal : NORMAL;
		        float3 viewDir : TEXCOORD2;
		        float3 worldRefl : TEXCOORD3;
		        float3 worldPos : TEXCOORD4;
                UNITY_FOG_COORDS(1)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos( v.vertex );
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
		        o.worldPos = worldPos;
		        o.normal = normalize(mul(v.normal, (float3x3)unity_WorldToObject));
                o.viewDir = viewDir;
		        o.worldRefl = reflect(-viewDir, v.normal);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                half rim = 1.0 - saturate(dot (normalize(i.viewDir), i.normal));
                float3 emissive = _MainTex_var * texCUBE (_Cube, i.worldRefl) + _RimColor * pow(rim, _RimPower);
                float3 finalColor = emissive;
                fixed4 finalRGBA = fixed4(finalColor,1);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, finalRGBA, fixed4(0,0,0,1));
                return finalRGBA;
            }
            ENDCG
        }
    }
}
