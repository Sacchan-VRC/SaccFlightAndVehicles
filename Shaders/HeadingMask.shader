Shader "KitKatsStuff/HMCSHeadingMask"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
	}
		SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent+2000"}
		Blend DstColor Zero
		ZWrite OFF
		ZTest Off

		Stencil{
			Ref 3
			Comp always
			Pass replace
		}

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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 _Color;

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = _Color;
				return col;
			}
			ENDCG
		}
	}
}