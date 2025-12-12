// Shader originally created with Shader Forge v1.40 

Shader "SaccFlight/PBRVCol_Blend" {
    Properties {
        _BlendContrast ("BlendContrast", Range(1, 25)) = 1
        _NoiseTex ("Blending Noise Texture", 2D) = "white" {}
        _NoiseAmount ("Noise Amount", Range(0, 25)) = 1
        _Color ("Color", Color) = (0.5019608,0.5019608,0.5019608,1)
        _MainTex ("Base Texture", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MetallicTex ("Metallic Texture", 2D) = "white" {}
        _Gloss ("Smoothness", Range(0, 1)) = 0.8
        _Metallic ("Metallic", Range(0, 1)) = 0
        _NormalStrength ("Normal Strength", Range(0, 10)) = 1
        _Color2 ("Color2", Color) = (0.5019608,0.5019608,0.5019608,1)
        _MainTex2 ("Texture2", 2D) = "white" {}
        _BumpMap2 ("Normal Map2", 2D) = "bump" {}
        _MetallicTex2 ("Metallic Texture2", 2D) = "white" {}
        _Gloss2 ("Smoothness2", Range(0, 1)) = 0.8
        _Metallic2 ("Metallic2", Range(0, 1)) = 0
        _NormalStrength2 ("Normal Strength2", Range(0, 10)) = 1
        _Color3 ("Color3", Color) = (0.5019608,0.5019608,0.5019608,1)
        _MainTex3 ("Texture3", 2D) = "white" {}
        _BumpMap3 ("Normal Map3", 2D) = "bump" {}
        _MetallicTex3 ("Metallic Texture3", 2D) = "white" {}
        _Gloss3 ("Smoothness3", Range(0, 1)) = 0.8
        _Metallic3 ("Metallic3", Range(0, 1)) = 0
        _NormalStrength3 ("Normal Strength3", Range(0, 10)) = 1
        [Enum(Off,0,Front,1,Back,2)] CullMode ("Cull", Int) = 2
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Cull [CullMode]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define SHOULD_SAMPLE_SH ( defined (LIGHTMAP_OFF) && defined(DYNAMICLIGHTMAP_OFF) )
            #define _GLOSSYENV 1
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON NO_CULL
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma target 3.0
            half _BlendContrast;
            half _NoiseAmount;
            uniform sampler2D _NoiseTex; uniform float4 _NoiseTex_ST;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _MetallicTex; uniform float4 _MetallicTex_ST;
            uniform sampler2D _BumpMap; uniform float4 _BumpMap_ST;
            uniform sampler2D _MainTex2; uniform float4 _MainTex2_ST;
            uniform sampler2D _MetallicTex2; uniform float4 _MetallicTex2_ST;
            uniform sampler2D _BumpMap2; uniform float4 _BumpMap2_ST;
            uniform sampler2D _MainTex3; uniform float4 _MainTex3_ST;
            uniform sampler2D _MetallicTex3; uniform float4 _MetallicTex3_ST;
            uniform sampler2D _BumpMap3; uniform float4 _BumpMap3_ST;
            UNITY_INSTANCING_BUFFER_START( Props )
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength)
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color2)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic2)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss2)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength2)
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color3)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic3)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss3)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength3)
            UNITY_INSTANCING_BUFFER_END( Props )
            struct VertexInput {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                fixed4 color : COLOR;  // Added vertex color
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 posWorld : TEXCOORD3;
                float3 normalDir : TEXCOORD4;
                float3 tangentDir : TEXCOORD5;
                float3 bitangentDir : TEXCOORD6;
                LIGHTING_COORDS(7,8)
                UNITY_FOG_COORDS(9)
                #if defined(LIGHTMAP_ON) || defined(UNITY_SHOULD_SAMPLE_SH)
                    float4 ambientOrLightmapUV : TEXCOORD10;
                #endif
                fixed4 vertexColor : COLOR;  // Or float4 if you prefer
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                UNITY_SETUP_INSTANCE_ID( v );
                UNITY_TRANSFER_INSTANCE_ID( v, o );
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.uv2 = v.texcoord2;
                #ifdef LIGHTMAP_ON
                    o.ambientOrLightmapUV.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                    o.ambientOrLightmapUV.zw = 0;
                #elif UNITY_SHOULD_SAMPLE_SH
                #endif
                #ifdef DYNAMICLIGHTMAP_ON
                    o.ambientOrLightmapUV.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                o.vertexColor = v.color;
                return o;
            }
            half3 AdjustContrast_ColorMask(half3 color, half contrast) {
                float maxpoint = max(color.r, max(color.g, color.b));
		        half3 adjust = lerp(half3(maxpoint, maxpoint, maxpoint), color, contrast);
                return saturate(adjust / maxpoint);
	        }
            float4 frag(VertexOutput i) : COLOR {
                UNITY_SETUP_INSTANCE_ID( i );
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float4 _NoiseTex_var = tex2D(_NoiseTex,TRANSFORM_TEX(i.uv0, _NoiseTex)) ;
                half3 texBlend = i.vertexColor.rgb;
                texBlend += _NoiseTex_var.rgb * _NoiseAmount;
                texBlend = AdjustContrast_ColorMask(texBlend,_BlendContrast);
                float rgbSum = texBlend.r + texBlend.g + texBlend.b;
                float rComponent = texBlend.r / rgbSum;
                float gComponent = texBlend.g / rgbSum;
                float bComponent = texBlend.b / rgbSum;
                float3 _BumpMap_var = UnpackScaleNormal(tex2D(_BumpMap,TRANSFORM_TEX(i.uv0, _BumpMap)), UNITY_ACCESS_INSTANCED_PROP( Props, _NormalStrength ) * rComponent);
                float3 _BumpMap_var2 = UnpackScaleNormal(tex2D(_BumpMap2,TRANSFORM_TEX(i.uv0, _BumpMap2)), UNITY_ACCESS_INSTANCED_PROP( Props, _NormalStrength2 ) * gComponent);
                float3 _BumpMap_var3 = UnpackScaleNormal(tex2D(_BumpMap3,TRANSFORM_TEX(i.uv0, _BumpMap3)), UNITY_ACCESS_INSTANCED_PROP( Props, _NormalStrength3 ) * bComponent);
                float3 normalLocal = BlendNormals(_BumpMap_var.rgb, _BumpMap_var2.rgb);
                normalLocal = BlendNormals(normalLocal, _BumpMap_var3.rgb);
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
///////// Gloss:
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex)) * rComponent;
                float4 _MainTex2_var = tex2D(_MainTex2,TRANSFORM_TEX(i.uv0, _MainTex2)) * gComponent;
                float4 _MainTex3_var = tex2D(_MainTex3,TRANSFORM_TEX(i.uv0, _MainTex3)) * bComponent;
                float _Gloss_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss ) * _MainTex_var.a;
                float _Gloss2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss2 ) * _MainTex2_var.a;
                float _Gloss3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss3 ) * _MainTex3_var.a;
                float gloss = _Gloss_var + _Gloss2_var + _Gloss3_var;
                float perceptualSmoothness = 1 - gloss;
                float Smoothness = perceptualSmoothness * perceptualSmoothness;
                float specPow = exp2(gloss * 10.0 + 1.0 );
/////// GI Data:
                UnityLight light;
                #ifdef LIGHTMAP_OFF
                    light.color = lightColor;
                    light.dir = lightDirection;
                    light.ndotl = LambertTerm (normalDirection, light.dir);
                #else
                    light.color = half3(0.f, 0.f, 0.f);
                    light.ndotl = 0.0f;
                    light.dir = half3(0.f, 0.f, 0.f);
                #endif
                UnityGIInput d;
                d.light = light;
                d.worldPos = i.posWorld.xyz;
                d.worldViewDir = viewDirection;
                d.atten = attenuation;
                #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
                    d.ambient = 0;
                    d.lightmapUV = i.ambientOrLightmapUV;
                #else
                    d.ambient = i.ambientOrLightmapUV;
                #endif
                #if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
                    d.boxMin[0] = unity_SpecCube0_BoxMin;
                    d.boxMin[1] = unity_SpecCube1_BoxMin;
                #endif
                #if UNITY_SPECCUBE_BOX_PROJECTION
                    d.boxMax[0] = unity_SpecCube0_BoxMax;
                    d.boxMax[1] = unity_SpecCube1_BoxMax;
                    d.probePosition[0] = unity_SpecCube0_ProbePosition;
                    d.probePosition[1] = unity_SpecCube1_ProbePosition;
                #endif
                d.probeHDR[0] = unity_SpecCube0_HDR;
                d.probeHDR[1] = unity_SpecCube1_HDR;
                Unity_GlossyEnvironmentData ugls_en_data;
                ugls_en_data.roughness = 1.0 - gloss;
                ugls_en_data.reflUVW = viewReflectDirection;
                UnityGI gi = UnityGlobalIllumination(d, 1, normalDirection, ugls_en_data );
                lightDirection = gi.light.dir;
                lightColor = gi.light.color;
////// Specular:
                float NdotL = saturate(dot( normalDirection, lightDirection ));
                float LdotH = saturate(dot(lightDirection, halfDirection));
                float _Metallic_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic) * tex2D(_MetallicTex,TRANSFORM_TEX(i.uv0, _MetallicTex)) * rComponent;
                float _Metallic2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic2) * tex2D(_MetallicTex2,TRANSFORM_TEX(i.uv0, _MetallicTex2)) * gComponent;
                float _Metallic3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic3) * tex2D(_MetallicTex3,TRANSFORM_TEX(i.uv0, _MetallicTex3)) * bComponent;
                float3 specularColor = _Metallic_var + _Metallic2_var + _Metallic3_var;
                float specularMonochrome;
                float4 _Color_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color );
                float4 _Color2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color2 );
                float4 _Color3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color3 );
                float3 diffuseColor = _MainTex_var.rgb * _Color_var.rgb + _MainTex2_var.rgb * _Color2_var.rgb + _MainTex3_var.rgb * _Color3_var.rgb;
                diffuseColor = DiffuseAndSpecularFromMetallic( diffuseColor, specularColor, specularColor, specularMonochrome );
                specularMonochrome = 1.0-specularMonochrome;
                float NdotV = abs(dot( normalDirection, viewDirection ));
                float NdotH = saturate(dot( normalDirection, halfDirection ));
                float VdotH = saturate(dot( viewDirection, halfDirection ));
                float visTerm = SmithJointGGXVisibilityTerm( NdotL, NdotV, Smoothness );
                float normTerm = GGXTerm(NdotH, Smoothness);
                float specularPBL = (visTerm*normTerm) * UNITY_PI;
                #ifdef UNITY_COLORSPACE_GAMMA
                    specularPBL = sqrt(max(1e-4h, specularPBL));
                #endif
                specularPBL = max(0, specularPBL * NdotL);
                #if defined(_SPECULARHIGHLIGHTS_OFF)
                    specularPBL = 0.0;
                #endif
                half surfaceReduction;
                #ifdef UNITY_COLORSPACE_GAMMA
                    surfaceReduction = 1.0-0.28*Smoothness*perceptualSmoothness;
                #else
                    surfaceReduction = 1.0/(Smoothness*Smoothness + 1.0);
                #endif
                specularPBL *= any(specularColor) ? 1.0 : 0.0;
                float3 directSpecular = attenColor*specularPBL*FresnelTerm(specularColor, LdotH);
                half grazingTerm = saturate( gloss + specularMonochrome );
                float3 indirectSpecular = (gi.indirect.specular);
                indirectSpecular *= FresnelLerp (specularColor, grazingTerm, NdotV);
                indirectSpecular *= surfaceReduction;
                float3 specular = (directSpecular + indirectSpecular);
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                half fd90 = 0.5 + 2 * LdotH * LdotH * (1-gloss);
                float nlPow5 = Pow5(1-NdotL);
                float nvPow5 = Pow5(1-NdotV);
                float3 directDiffuse = ((1 +(fd90 - 1)*nlPow5) * (1 + (fd90 - 1)*nvPow5) * NdotL) * attenColor;
                float3 indirectDiffuse = float3(0,0,0);
                indirectDiffuse += gi.indirect.diffuse;
                float3 diffuse = (directDiffuse + indirectDiffuse) * diffuseColor;
/// Final Color:
                float vBrightness = saturate((i.vertexColor.r+i.vertexColor.g+i.vertexColor.b));
                float3 finalColor = diffuse * vBrightness + specular;
                fixed4 finalRGBA = fixed4(finalColor,1);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
        Pass {
            Name "FORWARD_DELTA"
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define SHOULD_SAMPLE_SH ( defined (LIGHTMAP_OFF) && defined(DYNAMICLIGHTMAP_OFF) )
            #define _GLOSSYENV 1
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma target 3.0
            half _BlendContrast;
            half _NoiseAmount;
            uniform sampler2D _NoiseTex; uniform float4 _NoiseTex_ST;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _MetallicTex; uniform float4 _MetallicTex_ST;
            uniform sampler2D _BumpMap; uniform float4 _BumpMap_ST;
            uniform sampler2D _MainTex2; uniform float4 _MainTex2_ST;
            uniform sampler2D _MetallicTex2; uniform float4 _MetallicTex2_ST;
            uniform sampler2D _BumpMap2; uniform float4 _BumpMap2_ST;
            uniform sampler2D _MainTex3; uniform float4 _MainTex3_ST;
            uniform sampler2D _MetallicTex3; uniform float4 _MetallicTex3_ST;
            uniform sampler2D _BumpMap3; uniform float4 _BumpMap3_ST;
            UNITY_INSTANCING_BUFFER_START( Props )
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength)
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color2)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic2)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss2)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength2)
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color3)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic3)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss3)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength3)
            UNITY_INSTANCING_BUFFER_END( Props )
            struct VertexInput {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                fixed4 color : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 posWorld : TEXCOORD3;
                float3 normalDir : TEXCOORD4;
                float3 tangentDir : TEXCOORD5;
                float3 bitangentDir : TEXCOORD6;
                LIGHTING_COORDS(7,8)
                UNITY_FOG_COORDS(9)
                fixed4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                UNITY_SETUP_INSTANCE_ID( v );
                UNITY_TRANSFER_INSTANCE_ID( v, o );
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.uv2 = v.texcoord2;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                o.vertexColor = v.color;
                return o;
            }
            half3 AdjustContrast_ColorMask(half3 color, half contrast) {
                float maxpoint = max(color.r, max(color.g, color.b));
		        half3 adjust = lerp(half3(maxpoint, maxpoint, maxpoint), color, contrast);
                return saturate(adjust / maxpoint);
	        }
                float4 frag(VertexOutput i) : COLOR {
                UNITY_SETUP_INSTANCE_ID( i );
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float4 _NoiseTex_var = tex2D(_NoiseTex,TRANSFORM_TEX(i.uv0, _NoiseTex)) ;
                half3 texBlend = i.vertexColor.rgb;
                texBlend += _NoiseTex_var.rgb * _NoiseAmount;
                texBlend = AdjustContrast_ColorMask(texBlend,_BlendContrast);
                float rgbSum = texBlend.r + texBlend.g + texBlend.b;
                float rComponent = texBlend.r / rgbSum;
                float gComponent = texBlend.g / rgbSum;
                float bComponent = texBlend.b / rgbSum;
                float3 _BumpMap_var = UnpackScaleNormal(tex2D(_BumpMap,TRANSFORM_TEX(i.uv0, _BumpMap)), UNITY_ACCESS_INSTANCED_PROP( Props, _NormalStrength ) * rComponent);
                float3 _BumpMap_var2 = UnpackScaleNormal(tex2D(_BumpMap2,TRANSFORM_TEX(i.uv0, _BumpMap2)), UNITY_ACCESS_INSTANCED_PROP( Props, _NormalStrength2 ) * gComponent);
                float3 _BumpMap_var3 = UnpackScaleNormal(tex2D(_BumpMap3,TRANSFORM_TEX(i.uv0, _BumpMap3)), UNITY_ACCESS_INSTANCED_PROP( Props, _NormalStrength3 ) * bComponent);
                float3 normalLocal = BlendNormals(_BumpMap_var.rgb, _BumpMap_var2.rgb);
                normalLocal = BlendNormals(normalLocal, _BumpMap_var3.rgb);
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 lightColor = _LightColor0.rgb;
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
///////// Gloss:
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex)) * rComponent;
                float4 _MainTex2_var = tex2D(_MainTex2,TRANSFORM_TEX(i.uv0, _MainTex2)) * gComponent;
                float4 _MainTex3_var = tex2D(_MainTex3,TRANSFORM_TEX(i.uv0, _MainTex3)) * bComponent;
                float _Gloss_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss ) * _MainTex_var.a;
                float _Gloss2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss2 ) * _MainTex2_var.a;
                float _Gloss3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss3 ) * _MainTex3_var.a;
                float gloss = _Gloss_var + _Gloss2_var + _Gloss3_var;
                float perceptualSmoothness = 1 - gloss;
                float Smoothness = perceptualSmoothness * perceptualSmoothness;
                float specPow = exp2(gloss * 10.0 + 1.0 );
////// Specular:
                float NdotL = saturate(dot( normalDirection, lightDirection ));
                float LdotH = saturate(dot(lightDirection, halfDirection));
                float _Metallic_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic) * tex2D(_MetallicTex,TRANSFORM_TEX(i.uv0, _MetallicTex)) * rComponent;
                float _Metallic2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic2) * tex2D(_MetallicTex2,TRANSFORM_TEX(i.uv0, _MetallicTex2)) * gComponent;
                float _Metallic3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic3) * tex2D(_MetallicTex3,TRANSFORM_TEX(i.uv0, _MetallicTex3)) * bComponent;
                float3 specularColor = _Metallic_var + _Metallic2_var + _Metallic3_var;
                float specularMonochrome;
                float4 _Color_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color );
                float4 _Color2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color2 );
                float4 _Color3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color3 );
                float3 diffuseColor = _MainTex_var.rgb * _Color_var.rgb + _MainTex2_var.rgb * _Color2_var.rgb + _MainTex3_var.rgb * _Color3_var.rgb;
                diffuseColor = DiffuseAndSpecularFromMetallic( diffuseColor, specularColor, specularColor, specularMonochrome );
                specularMonochrome = 1.0-specularMonochrome;
                float NdotV = abs(dot( normalDirection, viewDirection ));
                float NdotH = saturate(dot( normalDirection, halfDirection ));
                float VdotH = saturate(dot( viewDirection, halfDirection ));
                float visTerm = SmithJointGGXVisibilityTerm( NdotL, NdotV, Smoothness );
                float normTerm = GGXTerm(NdotH, Smoothness);
                float specularPBL = (visTerm*normTerm) * UNITY_PI;
                #ifdef UNITY_COLORSPACE_GAMMA
                    specularPBL = sqrt(max(1e-4h, specularPBL));
                #endif
                specularPBL = max(0, specularPBL * NdotL);
                #if defined(_SPECULARHIGHLIGHTS_OFF)
                    specularPBL = 0.0;
                #endif
                specularPBL *= any(specularColor) ? 1.0 : 0.0;
                float3 directSpecular = attenColor*specularPBL*FresnelTerm(specularColor, LdotH);
                float3 specular = directSpecular;
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                half fd90 = 0.5 + 2 * LdotH * LdotH * (1-gloss);
                float nlPow5 = Pow5(1-NdotL);
                float nvPow5 = Pow5(1-NdotV);
                float3 directDiffuse = ((1 +(fd90 - 1)*nlPow5) * (1 + (fd90 - 1)*nvPow5) * NdotL) * attenColor;
                float3 diffuse = directDiffuse * diffuseColor;
/// Final Color:
                float vBrightness = saturate((i.vertexColor.r+i.vertexColor.g+i.vertexColor.b));
                float3 finalColor = diffuse * vBrightness + specular;
                fixed4 finalRGBA = fixed4(finalColor * 1,0);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
        Pass {
            Name "Meta"
            Tags {
                "LightMode"="Meta"
            }
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_META 1
            #define SHOULD_SAMPLE_SH ( defined (LIGHTMAP_OFF) && defined(DYNAMICLIGHTMAP_OFF) )
            #define _GLOSSYENV 1
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "UnityMetaPass.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma target 3.0
            half _BlendContrast;
            half _NoiseAmount;
            uniform sampler2D _NoiseTex; uniform float4 _NoiseTex_ST;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _MetallicTex; uniform float4 _MetallicTex_ST;
            uniform sampler2D _MainTex2; uniform float4 _MainTex2_ST;
            uniform sampler2D _MetallicTex2; uniform float4 _MetallicTex2_ST;
            uniform sampler2D _MainTex3; uniform float4 _MainTex3_ST;
            uniform sampler2D _MetallicTex3; uniform float4 _MetallicTex3_ST;
            uniform sampler2D _BumpMap3; uniform float4 _BumpMap3_ST;
            UNITY_INSTANCING_BUFFER_START( Props )
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength)
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color2)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic2)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss2)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength2)
                UNITY_DEFINE_INSTANCED_PROP( float4, _Color3)
                UNITY_DEFINE_INSTANCED_PROP( float, _Metallic3)
                UNITY_DEFINE_INSTANCED_PROP( float, _Gloss3)
                UNITY_DEFINE_INSTANCED_PROP( float, _NormalStrength3)
            UNITY_INSTANCING_BUFFER_END( Props )
            struct VertexInput {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                fixed4 color : COLOR;  // Added vertex color
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 posWorld : TEXCOORD3;
                fixed4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                UNITY_SETUP_INSTANCE_ID( v );
                UNITY_TRANSFER_INSTANCE_ID( v, o );
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.uv2 = v.texcoord2;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST );
                o.vertexColor = v.color;
                return o;
            }
            half3 AdjustContrast_ColorMask(half3 color, half contrast) {
                float maxpoint = max(color.r, max(color.g, color.b));
		        half3 adjust = lerp(half3(maxpoint, maxpoint, maxpoint), color, contrast);
                return saturate(adjust / maxpoint);
	        }
                float4 frag(VertexOutput i) : SV_Target {
                UNITY_SETUP_INSTANCE_ID( i );
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT( UnityMetaInput, o );
                
                o.Emission = 0;
                
                float4 _NoiseTex_var = tex2D(_NoiseTex,TRANSFORM_TEX(i.uv0, _NoiseTex)) ;
                half3 texBlend = i.vertexColor.rgb;
                texBlend += _NoiseTex_var.rgb * _NoiseAmount;
                texBlend = AdjustContrast_ColorMask(texBlend,_BlendContrast);
                float rgbSum = texBlend.r + texBlend.g + texBlend.b;
                float rComponent = texBlend.r / rgbSum;
                float gComponent = texBlend.g / rgbSum;
                float bComponent = texBlend.b / rgbSum;
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float4 _MainTex2_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex2));
                float4 _MainTex3_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex3));
                float4 _Color_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color );
                float4 _Color2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color2 );
                float4 _Color3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Color3 );
                float3 diffColor = _MainTex_var.rgb * _Color_var.rgb + _MainTex2_var.rgb * _Color2_var.rgb + _MainTex3_var.rgb * _Color3_var.rgb;
                float vBrightness = saturate((i.vertexColor.r+i.vertexColor.g+i.vertexColor.b));
                diffColor *= vBrightness;
                float specularMonochrome;
                float3 specColor;
                float _Metallic_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic) * tex2D(_MetallicTex,TRANSFORM_TEX(i.uv0, _MetallicTex)) * rComponent;
                float _Metallic2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic2) * tex2D(_MetallicTex2,TRANSFORM_TEX(i.uv0, _MetallicTex2))* gComponent;
                float _Metallic3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Metallic3) * tex2D(_MetallicTex3,TRANSFORM_TEX(i.uv0, _MetallicTex3))* bComponent;
                diffColor = DiffuseAndSpecularFromMetallic( diffColor, _Metallic_var + _Metallic2_var + _Metallic3_var, specColor, specularMonochrome );
                float _Gloss_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss ) * _MainTex_var.a;
                float _Gloss2_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss2 ) * _MainTex2_var.a;
                float _Gloss3_var = UNITY_ACCESS_INSTANCED_PROP( Props, _Gloss3 ) * _MainTex3_var.a;
                float Smoothness = 1 - (_Gloss_var + _Gloss2_var + _Gloss3_var);
                o.Albedo = diffColor + specColor * Smoothness * Smoothness * 0.5;
                //
                return UnityMetaFragment( o );
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
