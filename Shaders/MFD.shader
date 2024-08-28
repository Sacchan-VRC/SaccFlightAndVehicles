Shader "SaccFlight/MFD" {
    Properties{
        [HDR] _Color ("Color", Color) = (0.5,0.5,0.5,0.0) 
        _Brightness("Brightness", Range(0,1)) = 1
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        Tags { "DisableBatching" = "True" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma DisableBatching 
            #include "UnityCG.cginc"

			#define UNITY_SHADER_NO_UPGRADE 1 

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, float4(v.vertex.xyz, 1.0));
                return o;
            }

            fixed4 _Color;
            float _Brightness;

            fixed4 frag (v2f i) : SV_Target
            {
				return _Color * _Brightness;
            }
            ENDCG
        }
    }
}
