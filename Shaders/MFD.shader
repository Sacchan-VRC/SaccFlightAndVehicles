Shader "SF-1/MFD" {
    Properties{
        _Color ("Color", Color) = (0.5,0.5,0.5,0.0) 
        _Brightness("Brightness", Range(0,1)) = 1
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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
                o.vertex = UnityObjectToClipPos(v.vertex);
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
