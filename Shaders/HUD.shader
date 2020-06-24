Shader "SF-1/HUDStencil" {
    Properties{
        _Color ("Color", Color) = (0.5,0.5,0.5,0.0) 
    }
    SubShader{
        Tags {"Queue"="Transparent+2000" "RenderType"="Transparent" }
        ZTest Off
        
        Stencil{
            Ref 1
            Comp equal
            Pass Keep
        }
        Blend SrcAlpha OneMinusSrcAlpha
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

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
