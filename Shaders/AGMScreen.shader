Shader "SaccFlight/AGMScreen" 
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		[Toggle(TRIPLANAR_ROCKS)]_BlackAndWhite ("Black and White", Range(0,1)) = 1
        [HDR] _Color ("B&W Screen Color", Color) = (1,1,1,1)
		[Toggle(TRIPLANAR_ROCKS)]_BlackAndWhite_R ("B&W Use R", Range(0,1)) = 1
		[Toggle(TRIPLANAR_ROCKS)]_BlackAndWhite_G ("B&W Use G", Range(0,1)) = 1
		[Toggle(TRIPLANAR_ROCKS)]_BlackAndWhite_B ("B&W Use B", Range(0,1)) = 1
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float _BlackAndWhite;
            float _BlackAndWhite_R;
            float _BlackAndWhite_G;
            float _BlackAndWhite_B;
            float4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col;
                if (_BlackAndWhite)
                {
                    float numcols = 0;
                    float4 newcol4 = tex2D(_MainTex, i.uv);
                    float newcol = 0;
                    if (_BlackAndWhite_R)
                    {
                        newcol += newcol4.r;
                        numcols++;
                    }
                    if (_BlackAndWhite_G)
                    {
                        newcol += newcol4.g;
                        numcols++;
                    }
                    if (_BlackAndWhite_B)
                    {
                        newcol += newcol4.b;
                        numcols++;
                    }
                    newcol /= numcols;
                    col = newcol * _Color;
                }
                else
                {
                    col = tex2D(_MainTex, i.uv);
                }
                return col;
            }
            ENDCG
        }
    }
}
