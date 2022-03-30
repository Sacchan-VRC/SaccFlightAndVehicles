Shader "SF-1/HUDText"
{
	Properties
	{
		_Color ("Tint", Color) = (1,1,1,1)
		_StencilComp ("Stencil Comparison", Float) = 3
		_Stencil ("Stencil ID", Float) = 1
		_StencilOp ("Stencil Operation", Float) = 1
		_StencilWriteMask ("Stencil Write Mask", Float) = 1
		_StencilReadMask ("Stencil Read Mask", Float) = 1
		_ColorMask ("Color Mask", Float) = 15
		_Brightness("Brightness", Range(0,1)) = 1
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent+2000" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"


			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
			};
			
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float _Brightness;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f IN) : SV_Target
			{
				return (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color * _Brightness;
			}
			ENDCG
		}
	}
}