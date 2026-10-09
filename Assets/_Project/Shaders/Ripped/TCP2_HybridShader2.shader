// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "Toony Colors Pro 2/Hybrid Shader 2"
{
    Properties
    {
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull ("Render Face", Float) = 2.0
        [ToggleUI] _ZWrite ("Depth Write", Float) = 1.0
        [Toggle(_ALPHATEST_ON)] _UseAlphaTest ("Alpha Clipping", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Albedo", 2D) = "white" { }
        _HColor ("Highlight Color", Color) = (1,1,1,1)
        _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
        [Toggle(TCP2_SHADOW_LIGHT_COLOR)] _ShadowColorLightAtten ("Main Light affects Shadow Color", Float) = 1.0
        [Toggle(TCP2_SHADOW_TEXTURE)] _UseShadowTexture ("Enable Shadow Albedo Texture", Float) = 0.0
        _ShadowBaseMap ("Shadow Albedo", 2D) = "gray" { }
        _RampType ("Ramp Type", Float) = 0.0
        _Ramp ("Ramp Texture (RGB)", 2D) = "gray" { }
        _RampScale ("Scale", Float) = 1.0
        _RampOffset ("Offset", Float) = 0.0
        [PowerSlider(0.415)] _RampThreshold ("Threshold", Range(0.01, 1)) = 0.75
        _RampSmoothing ("Smoothing", Range(0, 1)) = 0.10000000149011612
        [IntRange] _RampBands ("Bands Count", Range(1, 20)) = 4.0
        _RampBandsSmoothing ("Bands Smoothing", Range(0, 1)) = 0.10000000149011612
        [Toggle(_NORMALMAP)] _UseNormalMap ("Normal Mapping", Float) = 0.0
        _BumpMap ("Normal Map", 2D) = "bump" { }
        _BumpScale ("Scale", Range(-1, 1)) = 1.0
        [Toggle(TCP2_SPECULAR)] _UseSpecular ("Specular", Float) = 0.0
        _SpecularType ("Type", Float) = 0.0
        _SpecularColor ("Color", Color) = (0.75,0.75,0.75,1)
        [PowerSlider(5.0)] _SpecularToonSize ("Size", Range(0.001, 1)) = 0.25
        _SpecularToonSmoothness ("Smoothing", Range(0, 1)) = 0.05000000074505806
        _SpecularRoughness ("Roughness", Range(0, 1)) = 0.5
        [Enum(Disabled,0,Albedo Alpha,1,Custom R,2,Custom G,3,Custom B,4,Custom A,5)] _SpecularMapType ("Specular Map#Specular Map (A)", Float) = 0.0
        _SpecGlossMap ("Specular Texture", 2D) = "white" { }
        [Toggle(_EMISSION)] _UseEmission ("Emission", Float) = 0.0
        [Enum(No Texture,5,R,0,G,1,B,2,A,3,RGB,4)] _EmissionChannel ("Texture Channel", Float) = 4.0
        _EmissionMap ("Texture#Texture (A)", 2D) = "white" { }
        _EmissionColor ("Color", Color) = (1,1,0,1)
        [Toggle(TCP2_RIM_LIGHTING)] _UseRim ("Rim Lighting", Float) = 0.0
        _RimColor ("Color", Color) = (0.8,0.8,0.8,0.5)
        _RimMin ("Min", Range(0, 2)) = 0.5
        _RimMax ("Max", Range(0, 2)) = 1.0
        [Toggle(TCP2_RIM_LIGHTING_LIGHTMASK)] _UseRimLightMask ("Light-based Mask", Float) = 1.0
        [Toggle(TCP2_MATCAP)] _UseMatCap ("MatCap", Float) = 0.0
        [Enum(Additive,0,Replace,1)] _MatCapType ("MatCap Blending", Float) = 0.0
        _MatCapTex ("Texture", 2D) = "black" { }
        _MatCapColor ("Color", Color) = (1,1,1,1)
        [Toggle(TCP2_MATCAP_MASK)] _UseMatCapMask ("Enable Mask", Float) = 0.0
        _MatCapMask ("Mask Texture#Mask Texture (A)", 2D) = "black" { }
        [Enum(R,0,G,1,B,2,A,3)] _MatCapMaskChannel ("Texture Channel", Float) = 0.0
        _IndirectIntensity ("Strength", Range(0, 1)) = 1.0
        [ToggleUI] _SingleIndirectColor ("Single Indirect Color", Float) = 0.0
        [Toggle(TCP2_REFLECTIONS)] _UseReflections ("Indirect Specular (Environment Reflections)", Float) = 0.0
        _ReflectionColor ("Color", Color) = (1,1,1,1)
        _ReflectionSmoothness ("Smoothness", Range(0, 1)) = 0.5
        _ReflectionMapType ("Reflection Map", Float) = 0.0
        _ReflectionTex ("Reflection Texture#Reflection Texture (A)", 2D) = "white" { }
        [Toggle(TCP2_REFLECTIONS_FRESNEL)] _UseFresnelReflections ("Fresnel Reflections", Float) = 1.0
        _FresnelMin ("Fresnel Min", Range(0, 2)) = 0.0
        _FresnelMax ("Fresnel Max", Range(0, 2)) = 1.5
        [Toggle(TCP2_OCCLUSION)] _UseOcclusion ("Occlusion", Float) = 0.0
        _OcclusionStrength ("Strength", Range(0, 1)) = 1.0
        _OcclusionMap ("Texture#Texture (A)", 2D) = "white" { }
        [Enum(Albedo Alpha,0,Custom R,1,Custom G,2,Custom B,3,Custom A,4)] _OcclusionChannel ("Texture Channel", Float) = 0.0
        [ToggleUI] _UseOutline ("Outline", Float) = 0.0
        _OutlineColor ("Color", Color) = (0,0,0,1)
        _OutlineTextureType ("Texture", Float) = 0.0
        _OutlineTextureLOD ("Texture LOD", Range(0, 8)) = 5.0
        _OutlineWidth ("Width", Range(0, 10)) = 1.0
        _OutlinePixelSizeType ("Pixel Size", Float) = 0.0
        _OutlineMinWidth ("Minimum Width (Pixels)", Float) = 1.0
        _OutlineMaxWidth ("Maximum Width (Pixels)", Float) = 1.0
        _NormalsSource ("Outline Normals Source", Float) = 0.0
        _NormalsUVType ("UV Data Type", Float) = 0.0
        _OutlineLightingTypeURP ("Lighting", Float) = 0.0
        _OutlineLightingType ("Lighting", Float) = 0.0
        _DirectIntensityOutline ("Direct Strength", Range(0, 1)) = 1.0
        _IndirectIntensityOutline ("Indirect Strength", Range(0, 1)) = 0.0
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadowsOff ("Receive Shadows", Float) = 1.0
        _RenderingMode ("rendering mode", Float) = 0.0
        _SrcBlend ("blending source", Float) = 1.0
        _DstBlend ("blending destination", Float) = 0.0
        _UseMobileMode ("Mobile mode", Float) = 0.0
    }
    SubShader
    {
        Tags { "BBOutlineTarget"="CubeFamily" "IgnoreProjector"="true" "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Name "Main"
            Tags { "BBOutlineTarget"="CubeFamily" "LightMode"="ForwardBase" "ShadowSupport"="true" }
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest LEqual
            Blend [_SrcBlend] [_DstBlend]
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #pragma shader_feature_local __ TCP2_MOBILE
            #pragma shader_feature_local __ TCP2_REFLECTIONS_FRESNEL
            #pragma shader_feature_local __ TCP2_RIM_LIGHTING
            #pragma shader_feature_local __ TCP2_RIM_LIGHTING_LIGHTMASK
            #pragma shader_feature_local __ TCP2_SHADOW_LIGHT_COLOR
            #pragma shader_feature_local __ TCP2_SPECULAR
            #pragma shader_feature_local __ _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local __ _NORMALMAP
            #pragma shader_feature_local __ _RECEIVE_SHADOWS_OFF
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _BaseColor;
            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            sampler2D _BumpMap;
            float4 _BumpMap_ST;
            float _BumpScale;
            float4 _HColor;
            float _IndirectIntensity;
            float _RampSmoothing;
            float _RampThreshold;
            float4 _RimColor;
            float _RimMax;
            float _RimMin;
            float4 _SColor;
            float _SingleIndirectColor;
            sampler2D _SpecGlossMap;
            float4 _SpecularColor;
            float _SpecularMapType;
            float _SpecularRoughness;
        
            #if defined(TCP2_MOBILE) && defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(TCP2_SPECULAR) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_NORMALMAP) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v0
        #define VTAG_ID 0
            #elif defined(TCP2_MOBILE) && defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(TCP2_SPECULAR) && !defined(TCP2_RIM_LIGHTING) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_NORMALMAP) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v1
        #define VTAG_ID 1
            #elif defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(TCP2_SPECULAR) && defined(_NORMALMAP) && !defined(TCP2_MOBILE) && !defined(TCP2_RIM_LIGHTING) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v2
        #define VTAG_ID 2
            #elif defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(TCP2_SPECULAR) && defined(_ALPHAPREMULTIPLY_ON) && !defined(TCP2_MOBILE) && !defined(TCP2_RIM_LIGHTING) && !defined(_NORMALMAP) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v3
        #define VTAG_ID 3
            #elif defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(_RECEIVE_SHADOWS_OFF) && !defined(TCP2_MOBILE) && !defined(TCP2_RIM_LIGHTING) && !defined(TCP2_SPECULAR) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_NORMALMAP)
                #define VTAG v4
        #define VTAG_ID 4
            #elif defined(TCP2_MOBILE) && defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && !defined(TCP2_RIM_LIGHTING) && !defined(TCP2_SPECULAR) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_NORMALMAP) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v5
        #define VTAG_ID 5
            #elif defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(_NORMALMAP) && !defined(TCP2_MOBILE) && !defined(TCP2_RIM_LIGHTING) && !defined(TCP2_SPECULAR) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v6
        #define VTAG_ID 6
            #elif defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(TCP2_SPECULAR) && !defined(TCP2_MOBILE) && !defined(TCP2_RIM_LIGHTING) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_NORMALMAP) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v7
        #define VTAG_ID 7
            #elif defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(_ALPHAPREMULTIPLY_ON) && !defined(TCP2_MOBILE) && !defined(TCP2_RIM_LIGHTING) && !defined(TCP2_SPECULAR) && !defined(_NORMALMAP) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v8
        #define VTAG_ID 8
            #elif defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && defined(TCP2_SHADOW_LIGHT_COLOR) && !defined(TCP2_MOBILE) && !defined(TCP2_RIM_LIGHTING) && !defined(TCP2_SPECULAR) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_NORMALMAP) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v4
        #define VTAG_ID 4
            #elif defined(TCP2_REFLECTIONS_FRESNEL) && defined(TCP2_RIM_LIGHTING_LIGHTMASK) && !defined(TCP2_MOBILE) && !defined(TCP2_RIM_LIGHTING) && !defined(TCP2_SHADOW_LIGHT_COLOR) && !defined(TCP2_SPECULAR) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_NORMALMAP) && !defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v9
        #define VTAG_ID 9
            #else
                #define VTAG v9
        #define VTAG_ID 9
            #endif
            // ---- v0: s0p0_FORWARDBASE_Vertex_GLES3_104.glsl  |  keyword material: TCP2_MOBILE TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR TCP2_SPECULAR
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD2 : TEXCOORD3;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float3 u_xlat16_2;
        float u_xlat9;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat1.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat1.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat1.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat9 = dot(u_xlat1.xyz, u_xlat1.xyz);
        u_xlat9 = rsqrt(u_xlat9);
        u_xlat1.xyz = ((float3)(u_xlat9)) * u_xlat1.xyz;
        o.vs_NORMAL0.xyz = u_xlat1.xyz;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        u_xlat0.xyz = (-u_xlat0.xyz) + _WorldSpaceCameraPos.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        u_xlat16_2.x = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat9 = max(u_xlat16_2.x, 0.00100000005);
        u_xlat16_2.x = rsqrt(u_xlat9);
        u_xlat16_2.xyz = u_xlat0.xyz * u_xlat16_2.xxx;
        u_xlat0.x = dot(u_xlat16_2.xyz, u_xlat1.xyz);
        u_xlat0.x = max(u_xlat0.x, 0.0);
        o.vs_TEXCOORD2.w = (-u_xlat0.x) + 1.0;
        o.vs_TEXCOORD2.xyz = float3(0.0, 0.0, 0.0);
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float4 u_xlat0;
        float3 u_xlat16_1;
        float4 u_xlat16_2;
        float3 u_xlat16_3;
        float u_xlat4;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float u_xlat16_8;
        float u_xlat10;
        float u_xlat18;
        bool u_xlatb18;
        float u_xlat16_19;
        float u_xlat16_21;
    
        u_xlat0.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_1.x = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat18 = max(u_xlat16_1.x, 0.00100000005);
        u_xlat16_1.x = rsqrt(u_xlat18);
        u_xlat16_1.xyz = u_xlat0.xyz * u_xlat16_1.xxx;
        u_xlat16_19 = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_19 = rsqrt(u_xlat16_19);
        u_xlat16_2.xyz = _WorldSpaceLightPos0.xyz * ((float3)(u_xlat16_19)) + u_xlat16_1.xyz;
        u_xlat16_3.xyz = ((float3)(u_xlat16_19)) * _WorldSpaceLightPos0.xyz;
        u_xlat16_19 = dot(u_xlat16_2.xyz, u_xlat16_2.xyz);
        u_xlat0.x = max(u_xlat16_19, 0.00100000005);
        u_xlat16_19 = rsqrt(u_xlat0.x);
        u_xlat16_2.xyz = ((float3)(u_xlat16_19)) * u_xlat16_2.xyz;
        u_xlat0.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xyz = u_xlat0.xxx * i.vs_NORMAL0.xyz;
        u_xlat18 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat0.xyz = ((float3)(u_xlat18)) * u_xlat0.xyz;
        u_xlat18 = dot(u_xlat0.xyz, u_xlat16_2.xyz);
        u_xlat18 = clamp(u_xlat18, 0.0, 1.0);
        u_xlat16_4.x = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).w;
        u_xlat16_19 = max(_SpecularRoughness, 9.99999975e-06);
        u_xlat16_19 = u_xlat16_4.x * u_xlat16_19;
        u_xlat4 = u_xlat16_19 * u_xlat16_19;
        u_xlat16_2.x = min(u_xlat4, 1.0);
        u_xlat16_8 = u_xlat4 * 0.280000001;
        u_xlat4 = (-u_xlat16_8) * u_xlat16_19 + 1.0;
        u_xlat16_19 = u_xlat16_2.x * u_xlat16_2.x;
        u_xlat16_2.x = u_xlat18 * u_xlat16_19 + (-u_xlat18);
        u_xlat18 = u_xlat16_2.x * u_xlat18 + 1.0;
        u_xlat18 = u_xlat18 * u_xlat18 + 9.99999975e-05;
        u_xlat10 = u_xlat16_19 * 0.318309873;
        u_xlat18 = u_xlat10 / u_xlat18;
        u_xlat16_19 = u_xlat18 * 0.157079637;
        u_xlat18 = max(u_xlat16_19, 9.99999975e-05);
        u_xlat18 = sqrt(u_xlat18);
        u_xlat16_19 = u_xlat4 * u_xlat18;
        u_xlat16_19 = max(u_xlat16_19, 0.0);
        u_xlat18 = dot(u_xlat0.xyz, u_xlat16_3.xyz);
        u_xlat16_2.x = u_xlat18;
        u_xlat16_2.x = clamp(u_xlat16_2.x, 0.0, 1.0);
        u_xlat16_8 = u_xlat18 * 0.5 + 0.5;
        u_xlat16_19 = u_xlat16_19 * u_xlat16_2.x;
        u_xlat16_2.xzw = ((float3)(u_xlat16_19)) * _LightColor0.xyz;
        u_xlat16_2.xzw = u_xlat16_2.xzw * _SpecularColor.xyz;
        u_xlat16_19 = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_8 = (-u_xlat16_19) + u_xlat16_8;
        u_xlat16_3.x = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_19 = (-u_xlat16_19) + u_xlat16_3.x;
        u_xlat16_19 = ((float)(1.0)) / u_xlat16_19;
        u_xlat16_19 = u_xlat16_19 * u_xlat16_8;
        u_xlat16_19 = clamp(u_xlat16_19, 0.0, 1.0);
        u_xlat16_8 = u_xlat16_19 * -2.0 + 3.0;
        u_xlat16_19 = u_xlat16_19 * u_xlat16_19;
        u_xlat16_19 = u_xlat16_19 * u_xlat16_8;
        u_xlat16_3.xyz = ((float3)(u_xlat16_19)) * _LightColor0.xyz;
        u_xlat16_8 = (-_RimMin) + _RimMax;
        u_xlat16_8 = ((float)(1.0)) / u_xlat16_8;
        u_xlat16_21 = i.vs_TEXCOORD2.w + (-_RimMin);
        u_xlat16_8 = u_xlat16_8 * u_xlat16_21;
        u_xlat16_8 = clamp(u_xlat16_8, 0.0, 1.0);
        u_xlat16_21 = u_xlat16_8 * -2.0 + 3.0;
        u_xlat16_8 = u_xlat16_8 * u_xlat16_8;
        u_xlat16_8 = u_xlat16_8 * u_xlat16_21;
        u_xlat16_3.xyz = ((float3)(u_xlat16_8)) * u_xlat16_3.xyz;
        u_xlat16_2.xyz = u_xlat16_3.xyz * _RimColor.xyz + u_xlat16_2.xzw;
        u_xlatb18 = 0.0<_SingleIndirectColor;
        u_xlat0.xyz = (((bool)(u_xlatb18))) ? u_xlat16_1.xyz : u_xlat0.xyz;
        u_xlat0.w = 1.0;
        u_xlat16_1.x = dot(unity_SHAr, u_xlat0);
        u_xlat16_1.y = dot(unity_SHAg, u_xlat0);
        u_xlat16_1.z = dot(unity_SHAb, u_xlat0);
        u_xlat16_1.xyz = max(u_xlat16_1.xyz, float3(0.0, 0.0, 0.0));
        u_xlat0.xyz = log2(u_xlat16_1.xyz);
        u_xlat0.xyz = u_xlat0.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat0.xyz = exp2(u_xlat0.xyz);
        u_xlat0.xyz = u_xlat0.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat0.xyz = max(u_xlat0.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_4.xyz = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy).xyz;
        u_xlat16_1.xyz = u_xlat16_4.xyz * _BaseColor.xyz;
        u_xlat16_3.xyz = u_xlat0.xyz * u_xlat16_1.xyz;
        u_xlat16_3.xyz = u_xlat16_3.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity)));
        u_xlat16_5.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_5.xyz = ((float3)(u_xlat16_19)) * u_xlat16_5.xyz + _SColor.xyz;
        u_xlat16_5.xyz = u_xlat16_5.xyz * _LightColor0.xyz;
        u_xlat16_1.xyz = u_xlat16_1.xyz * u_xlat16_5.xyz + u_xlat16_3.xyz;
        SV_Target0.xyz = u_xlat16_2.xyz + u_xlat16_1.xyz;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v1: s0p0_FORWARDBASE_Vertex_GLES3_103.glsl  |  keyword material: TCP2_MOBILE TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR TCP2_SPECULAR
            #if VTAG_ID == 1
            struct appdata_v1 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v1 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v1 vert_v1(appdata_v1 v) {
        v2f_v1 o = (v2f_v1)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        o.vs_NORMAL0.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v1(v2f_v1 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float4 u_xlat0;
        float3 u_xlat16_1;
        float4 u_xlat16_2;
        float3 u_xlat16_3;
        float u_xlat4;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float u_xlat16_8;
        float u_xlat10;
        float u_xlat18;
        bool u_xlatb18;
        float u_xlat16_19;
        float u_xlat16_21;
    
        u_xlat0.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_1.x = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat18 = max(u_xlat16_1.x, 0.00100000005);
        u_xlat16_1.x = rsqrt(u_xlat18);
        u_xlat16_1.xyz = u_xlat0.xyz * u_xlat16_1.xxx;
        u_xlat16_19 = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_19 = rsqrt(u_xlat16_19);
        u_xlat16_2.xyz = _WorldSpaceLightPos0.xyz * ((float3)(u_xlat16_19)) + u_xlat16_1.xyz;
        u_xlat16_3.xyz = ((float3)(u_xlat16_19)) * _WorldSpaceLightPos0.xyz;
        u_xlat16_19 = dot(u_xlat16_2.xyz, u_xlat16_2.xyz);
        u_xlat0.x = max(u_xlat16_19, 0.00100000005);
        u_xlat16_19 = rsqrt(u_xlat0.x);
        u_xlat16_2.xyz = ((float3)(u_xlat16_19)) * u_xlat16_2.xyz;
        u_xlat0.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xyz = u_xlat0.xxx * i.vs_NORMAL0.xyz;
        u_xlat18 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat0.xyz = ((float3)(u_xlat18)) * u_xlat0.xyz;
        u_xlat18 = dot(u_xlat0.xyz, u_xlat16_2.xyz);
        u_xlat18 = clamp(u_xlat18, 0.0, 1.0);
        u_xlat16_4.x = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).w;
        u_xlat16_19 = max(_SpecularRoughness, 9.99999975e-06);
        u_xlat16_19 = u_xlat16_4.x * u_xlat16_19;
        u_xlat4 = u_xlat16_19 * u_xlat16_19;
        u_xlat16_2.x = min(u_xlat4, 1.0);
        u_xlat16_8 = u_xlat4 * 0.280000001;
        u_xlat4 = (-u_xlat16_8) * u_xlat16_19 + 1.0;
        u_xlat16_19 = u_xlat16_2.x * u_xlat16_2.x;
        u_xlat16_2.x = u_xlat18 * u_xlat16_19 + (-u_xlat18);
        u_xlat18 = u_xlat16_2.x * u_xlat18 + 1.0;
        u_xlat18 = u_xlat18 * u_xlat18 + 9.99999975e-05;
        u_xlat10 = u_xlat16_19 * 0.318309873;
        u_xlat18 = u_xlat10 / u_xlat18;
        u_xlat16_19 = u_xlat18 * 0.157079637;
        u_xlat18 = max(u_xlat16_19, 9.99999975e-05);
        u_xlat18 = sqrt(u_xlat18);
        u_xlat16_19 = u_xlat4 * u_xlat18;
        u_xlat16_19 = max(u_xlat16_19, 0.0);
        u_xlat18 = dot(u_xlat0.xyz, u_xlat16_3.xyz);
        u_xlat16_2.x = u_xlat18;
        u_xlat16_2.x = clamp(u_xlat16_2.x, 0.0, 1.0);
        u_xlat16_8 = u_xlat18 * 0.5 + 0.5;
        u_xlat16_19 = u_xlat16_19 * u_xlat16_2.x;
        u_xlat16_2.xzw = ((float3)(u_xlat16_19)) * _LightColor0.xyz;
        u_xlatb18 = 0.0<_SingleIndirectColor;
        u_xlat0.xyz = (((bool)(u_xlatb18))) ? u_xlat16_1.xyz : u_xlat0.xyz;
        u_xlat0.w = 1.0;
        u_xlat16_1.x = dot(unity_SHAr, u_xlat0);
        u_xlat16_1.y = dot(unity_SHAg, u_xlat0);
        u_xlat16_1.z = dot(unity_SHAb, u_xlat0);
        u_xlat16_1.xyz = max(u_xlat16_1.xyz, float3(0.0, 0.0, 0.0));
        u_xlat0.xyz = log2(u_xlat16_1.xyz);
        u_xlat0.xyz = u_xlat0.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat0.xyz = exp2(u_xlat0.xyz);
        u_xlat0.xyz = u_xlat0.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat0.xyz = max(u_xlat0.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_4.xyz = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy).xyz;
        u_xlat16_1.xyz = u_xlat16_4.xyz * _BaseColor.xyz;
        u_xlat16_3.xyz = u_xlat0.xyz * u_xlat16_1.xyz;
        u_xlat16_3.xyz = u_xlat16_3.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity)));
        u_xlat16_19 = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_8 = (-u_xlat16_19) + u_xlat16_8;
        u_xlat16_21 = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_19 = (-u_xlat16_19) + u_xlat16_21;
        u_xlat16_19 = ((float)(1.0)) / u_xlat16_19;
        u_xlat16_19 = u_xlat16_19 * u_xlat16_8;
        u_xlat16_19 = clamp(u_xlat16_19, 0.0, 1.0);
        u_xlat16_8 = u_xlat16_19 * -2.0 + 3.0;
        u_xlat16_19 = u_xlat16_19 * u_xlat16_19;
        u_xlat16_19 = u_xlat16_19 * u_xlat16_8;
        u_xlat16_5.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_5.xyz = ((float3)(u_xlat16_19)) * u_xlat16_5.xyz + _SColor.xyz;
        u_xlat16_5.xyz = u_xlat16_5.xyz * _LightColor0.xyz;
        u_xlat16_1.xyz = u_xlat16_1.xyz * u_xlat16_5.xyz + u_xlat16_3.xyz;
        SV_Target0.xyz = u_xlat16_2.xzw * _SpecularColor.xyz + u_xlat16_1.xyz;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v2: s0p0_FORWARDBASE_Vertex_GLES3_105.glsl  |  keyword material: TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR TCP2_SPECULAR _NORMALMAP
            #if VTAG_ID == 2
            struct appdata_v2 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TANGENT0 : TANGENT;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v2 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD2 : TEXCOORD3;
        float4 vs_TEXCOORD3 : TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v2 vert_v2(appdata_v2 v) {
        v2f_v2 o = (v2f_v2)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float3 u_xlat2;
        float u_xlat16_3;
        float u_xlat12;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat12 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat12 = rsqrt(u_xlat12);
        u_xlat0.xyz = ((float3)(u_xlat12)) * u_xlat0.xyz;
        o.vs_NORMAL0.xyz = u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        u_xlat1.xyz = v.in_TANGENT0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat1.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_TANGENT0.xxx + u_xlat1.xyz;
        u_xlat1.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_TANGENT0.zzz + u_xlat1.xyz;
        u_xlat12 = dot(u_xlat1.xyz, u_xlat1.xyz);
        u_xlat12 = rsqrt(u_xlat12);
        u_xlat1.xyz = ((float3)(u_xlat12)) * u_xlat1.xyz;
        o.vs_TEXCOORD2.xyz = u_xlat1.xyz;
        o.vs_TEXCOORD2.w = 0.0;
        u_xlat2.xyz = u_xlat0.zxy * u_xlat1.yzx;
        u_xlat0.xyz = u_xlat0.yzx * u_xlat1.zxy + (-u_xlat2.xyz);
        u_xlat16_3 = v.in_TANGENT0.w * unity_WorldTransformParams.w;
        o.vs_TEXCOORD3.xyz = u_xlat0.xyz * ((float3)(u_xlat16_3));
        o.vs_TEXCOORD3.w = 0.0;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v2(v2f_v2 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float4 u_xlat0;
        float u_xlat16_0;
        bool2 u_xlatb0;
        float3 u_xlat1;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float4 u_xlat16_3;
        float3 u_xlat16_4;
        float4 u_xlat16_5;
        float4 u_xlat6;
        float3 u_xlat16_6;
        float3 u_xlat16_7;
        float u_xlat8;
        float u_xlat16_10;
        float u_xlat16_11;
        float u_xlat16;
        float u_xlat16_18;
        float u_xlat16_19;
        float u_xlat24;
        float u_xlat16_26;
    
        u_xlat0.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xyz = u_xlat0.xxx * i.vs_NORMAL0.xyz;
        u_xlat24 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat0.xyz = ((float3)(u_xlat24)) * u_xlat0.xyz;
        u_xlat1.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_2.x = dot(u_xlat1.xyz, u_xlat1.xyz);
        u_xlat24 = max(u_xlat16_2.x, 0.00100000005);
        u_xlat16_2.x = rsqrt(u_xlat24);
        u_xlat16_2.xyz = u_xlat1.xyz * u_xlat16_2.xxx;
        u_xlat16_26 = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_26 = rsqrt(u_xlat16_26);
        u_xlat16_3.xyz = ((float3)(u_xlat16_26)) * _WorldSpaceLightPos0.xyz;
        u_xlat16_1 = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy);
        u_xlat16_4.xyz = u_xlat16_1.xyz * _BaseColor.xyz;
        u_xlat1.xy = i.vs_TEXCOORD1.zw * _BumpMap_ST.xy + _BumpMap_ST.zw;
        u_xlat16_1.xyz = tex2D(_BumpMap, (u_xlat1.xy).xy).xyz;
        u_xlat16_5.xyz = u_xlat16_1.xyz * float3(2.0, 2.0, 2.0) + float3(-1.0, -1.0, -1.0);
        u_xlat16_5.xy = u_xlat16_5.xy * ((float2)(_BumpScale));
        u_xlat16_6.xyz = u_xlat16_5.yyy * i.vs_TEXCOORD3.xyz;
        u_xlat16_5.xyw = u_xlat16_5.xxx * i.vs_TEXCOORD2.xyz + u_xlat16_6.xyz;
        u_xlat16_5.xyz = u_xlat16_5.zzz * u_xlat0.xyz + u_xlat16_5.xyw;
        u_xlat0.x = dot(u_xlat16_5.xyz, u_xlat16_3.xyz);
        u_xlat16_3.x = u_xlat0.x * 0.5 + 0.5;
        u_xlat16_11 = u_xlat0.x;
        u_xlat16_11 = clamp(u_xlat16_11, 0.0, 1.0);
        u_xlat16_19 = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_3.w = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_3.xw = (-((float2)(u_xlat16_19))) + u_xlat16_3.xw;
        u_xlat16_19 = ((float)(1.0)) / u_xlat16_3.w;
        u_xlat16_3.x = u_xlat16_19 * u_xlat16_3.x;
        u_xlat16_3.x = clamp(u_xlat16_3.x, 0.0, 1.0);
        u_xlat16_19 = u_xlat16_3.x * -2.0 + 3.0;
        u_xlat16_3.x = u_xlat16_3.x * u_xlat16_3.x;
        u_xlat16_3.x = u_xlat16_3.x * u_xlat16_19;
        u_xlat16_6.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xxx * u_xlat16_6.xyz + _SColor.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xzw * _LightColor0.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xzw * u_xlat16_4.xyz;
        u_xlatb0.xy = ((float4(0.0, 0.0, 0.0, 0.0)) < (float4(_SingleIndirectColor, _IndirectIntensity, _SingleIndirectColor, _SingleIndirectColor))).xy;
        u_xlat6.xyz = (u_xlatb0.x) ? u_xlat16_2.xyz : u_xlat16_5.xyz;
        u_xlat6.w = 1.0;
        u_xlat16_7.x = dot(unity_SHAr, u_xlat6);
        u_xlat16_7.y = dot(unity_SHAg, u_xlat6);
        u_xlat16_7.z = dot(unity_SHAb, u_xlat6);
        u_xlat16_7.xyz = max(u_xlat16_7.xyz, float3(0.0, 0.0, 0.0));
        u_xlat0.xzw = log2(u_xlat16_7.xyz);
        u_xlat0.xzw = u_xlat0.xzw * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat0.xzw = exp2(u_xlat0.xzw);
        u_xlat0.xzw = u_xlat0.xzw * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat0.xzw = max(u_xlat0.xzw, float3(0.0, 0.0, 0.0));
        u_xlat16_4.xyz = u_xlat16_4.xyz * u_xlat0.xzw;
        u_xlat16_4.xyz = u_xlat16_4.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity))) + u_xlat16_3.xzw;
        u_xlat16_3.xzw = (u_xlatb0.y) ? u_xlat16_4.xyz : u_xlat16_3.xzw;
        u_xlatb0.x = _SpecularMapType>=5.0;
        if(u_xlatb0.x){
            u_xlat16_0 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).w;
            u_xlat16_4.x = u_xlat16_0;
        } else {
            u_xlatb0.x = _SpecularMapType>=4.0;
            if(u_xlatb0.x){
                u_xlat16_0 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).z;
                u_xlat16_4.x = u_xlat16_0;
            } else {
                u_xlatb0.x = _SpecularMapType>=3.0;
                if(u_xlatb0.x){
                    u_xlat16_0 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).y;
                    u_xlat16_4.x = u_xlat16_0;
                } else {
                    u_xlatb0.x = _SpecularMapType>=2.0;
                    if(u_xlatb0.x){
                        u_xlat16_0 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).x;
                        u_xlat16_4.x = u_xlat16_0;
                    } else {
                        u_xlatb0.x = _SpecularMapType>=1.0;
                        u_xlat16_4.x = (u_xlatb0.x) ? u_xlat16_1.w : 1.0;
                    }
                }
            }
        }
        u_xlat16_2.xyz = _WorldSpaceLightPos0.xyz * ((float3)(u_xlat16_26)) + u_xlat16_2.xyz;
        u_xlat16_26 = dot(u_xlat16_2.xyz, u_xlat16_2.xyz);
        u_xlat0.x = max(u_xlat16_26, 0.00100000005);
        u_xlat16_26 = rsqrt(u_xlat0.x);
        u_xlat16_2.xyz = ((float3)(u_xlat16_26)) * u_xlat16_2.xyz;
        u_xlat0.x = dot(u_xlat16_5.xyz, u_xlat16_2.xyz);
        u_xlat0.x = clamp(u_xlat0.x, 0.0, 1.0);
        u_xlat16_2.x = max(_SpecularRoughness, 9.99999975e-06);
        u_xlat16_2.x = u_xlat16_4.x * u_xlat16_2.x;
        u_xlat8 = u_xlat16_2.x * u_xlat16_2.x;
        u_xlat16_10 = min(u_xlat8, 1.0);
        u_xlat16_10 = u_xlat16_10 * u_xlat16_10;
        u_xlat16_18 = u_xlat0.x * u_xlat16_10 + (-u_xlat0.x);
        u_xlat0.x = u_xlat16_18 * u_xlat0.x + 1.0;
        u_xlat16 = u_xlat16_10 * 0.318309873;
        u_xlat0.x = u_xlat0.x * u_xlat0.x + 9.99999975e-05;
        u_xlat0.x = u_xlat16 / u_xlat0.x;
        u_xlat16_10 = u_xlat0.x * 0.157079637;
        u_xlat0.x = max(u_xlat16_10, 9.99999975e-05);
        u_xlat0.x = sqrt(u_xlat0.x);
        u_xlat16_10 = u_xlat8 * 0.280000001;
        u_xlat8 = (-u_xlat16_10) * u_xlat16_2.x + 1.0;
        u_xlat16_2.x = u_xlat8 * u_xlat0.x;
        u_xlat16_2.x = max(u_xlat16_2.x, 0.0);
        u_xlat16_2.x = u_xlat16_11 * u_xlat16_2.x;
        u_xlat16_2.xyz = u_xlat16_2.xxx * _LightColor0.xyz;
        SV_Target0.xyz = u_xlat16_2.xyz * _SpecularColor.xyz + u_xlat16_3.xzw;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v3: s0p0_FORWARDBASE_Vertex_GLES3_107.glsl  |  keyword material: TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR TCP2_SPECULAR _ALPHAPREMULTIPLY_ON
            #if VTAG_ID == 3
            struct appdata_v3 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v3 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v3 vert_v3(appdata_v3 v) {
        v2f_v3 o = (v2f_v3)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        o.vs_NORMAL0.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v3(v2f_v3 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float3 u_xlat0;
        float3 u_xlat1;
        float4 u_xlat16_1;
        bool2 u_xlatb1;
        float3 u_xlat16_2;
        float4 u_xlat16_3;
        float4 u_xlat16_4;
        float4 u_xlat5;
        float3 u_xlat16_5;
        float3 u_xlat16_6;
        float3 u_xlat7;
        float u_xlat8;
        float u_xlat16_10;
        float u_xlat16_11;
        float u_xlat16;
        float u_xlat16_18;
        float u_xlat16_19;
        float u_xlat24;
        float u_xlat16_24;
        bool u_xlatb24;
        float u_xlat16_26;
    
        u_xlat0.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xyz = u_xlat0.xxx * i.vs_NORMAL0.xyz;
        u_xlat24 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat0.xyz = ((float3)(u_xlat24)) * u_xlat0.xyz;
        u_xlat1.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_2.x = dot(u_xlat1.xyz, u_xlat1.xyz);
        u_xlat24 = max(u_xlat16_2.x, 0.00100000005);
        u_xlat16_2.x = rsqrt(u_xlat24);
        u_xlat16_2.xyz = u_xlat1.xyz * u_xlat16_2.xxx;
        u_xlat16_26 = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_26 = rsqrt(u_xlat16_26);
        u_xlat16_3.xyz = ((float3)(u_xlat16_26)) * _WorldSpaceLightPos0.xyz;
        u_xlat16_1 = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy);
        u_xlat16_4 = u_xlat16_1 * _BaseColor;
        u_xlat24 = dot(u_xlat0.xyz, u_xlat16_3.xyz);
        u_xlat16_3.x = u_xlat24 * 0.5 + 0.5;
        u_xlat16_11 = u_xlat24;
        u_xlat16_11 = clamp(u_xlat16_11, 0.0, 1.0);
        u_xlat16_19 = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_3.w = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_3.xw = (-((float2)(u_xlat16_19))) + u_xlat16_3.xw;
        u_xlat16_19 = ((float)(1.0)) / u_xlat16_3.w;
        u_xlat16_3.x = u_xlat16_19 * u_xlat16_3.x;
        u_xlat16_3.x = clamp(u_xlat16_3.x, 0.0, 1.0);
        u_xlat16_19 = u_xlat16_3.x * -2.0 + 3.0;
        u_xlat16_3.x = u_xlat16_3.x * u_xlat16_3.x;
        u_xlat16_3.x = u_xlat16_3.x * u_xlat16_19;
        u_xlat16_5.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xxx * u_xlat16_5.xyz + _SColor.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xzw * _LightColor0.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xzw * u_xlat16_4.xyz;
        u_xlatb1.xy = ((float4(0.0, 0.0, 0.0, 0.0)) < (float4(_SingleIndirectColor, _IndirectIntensity, _SingleIndirectColor, _SingleIndirectColor))).xy;
        u_xlat5.xyz = (u_xlatb1.x) ? u_xlat16_2.xyz : u_xlat0.xyz;
        u_xlat5.w = 1.0;
        u_xlat16_6.x = dot(unity_SHAr, u_xlat5);
        u_xlat16_6.y = dot(unity_SHAg, u_xlat5);
        u_xlat16_6.z = dot(unity_SHAb, u_xlat5);
        u_xlat16_6.xyz = max(u_xlat16_6.xyz, float3(0.0, 0.0, 0.0));
        u_xlat7.xyz = log2(u_xlat16_6.xyz);
        u_xlat7.xyz = u_xlat7.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat7.xyz = exp2(u_xlat7.xyz);
        u_xlat7.xyz = u_xlat7.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat7.xyz = max(u_xlat7.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_4.xyz = u_xlat16_4.xyz * u_xlat7.xyz;
        u_xlat16_4.xyz = u_xlat16_4.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity))) + u_xlat16_3.xzw;
        u_xlat16_3.xzw = (u_xlatb1.y) ? u_xlat16_4.xyz : u_xlat16_3.xzw;
        u_xlatb24 = _SpecularMapType>=5.0;
        if(u_xlatb24){
            u_xlat16_24 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).w;
            u_xlat16_4.x = u_xlat16_24;
        } else {
            u_xlatb24 = _SpecularMapType>=4.0;
            if(u_xlatb24){
                u_xlat16_24 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).z;
                u_xlat16_4.x = u_xlat16_24;
            } else {
                u_xlatb24 = _SpecularMapType>=3.0;
                if(u_xlatb24){
                    u_xlat16_24 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).y;
                    u_xlat16_4.x = u_xlat16_24;
                } else {
                    u_xlatb24 = _SpecularMapType>=2.0;
                    if(u_xlatb24){
                        u_xlat16_24 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).x;
                        u_xlat16_4.x = u_xlat16_24;
                    } else {
                        u_xlatb24 = _SpecularMapType>=1.0;
                        u_xlat16_4.x = (u_xlatb24) ? u_xlat16_1.w : 1.0;
                    }
                }
            }
        }
        u_xlat16_2.xyz = _WorldSpaceLightPos0.xyz * ((float3)(u_xlat16_26)) + u_xlat16_2.xyz;
        u_xlat16_26 = dot(u_xlat16_2.xyz, u_xlat16_2.xyz);
        u_xlat24 = max(u_xlat16_26, 0.00100000005);
        u_xlat16_26 = rsqrt(u_xlat24);
        u_xlat16_2.xyz = ((float3)(u_xlat16_26)) * u_xlat16_2.xyz;
        u_xlat0.x = dot(u_xlat0.xyz, u_xlat16_2.xyz);
        u_xlat0.x = clamp(u_xlat0.x, 0.0, 1.0);
        u_xlat16_2.x = max(_SpecularRoughness, 9.99999975e-06);
        u_xlat16_2.x = u_xlat16_4.x * u_xlat16_2.x;
        u_xlat8 = u_xlat16_2.x * u_xlat16_2.x;
        u_xlat16_10 = min(u_xlat8, 1.0);
        u_xlat16_10 = u_xlat16_10 * u_xlat16_10;
        u_xlat16_18 = u_xlat0.x * u_xlat16_10 + (-u_xlat0.x);
        u_xlat0.x = u_xlat16_18 * u_xlat0.x + 1.0;
        u_xlat16 = u_xlat16_10 * 0.318309873;
        u_xlat0.x = u_xlat0.x * u_xlat0.x + 9.99999975e-05;
        u_xlat0.x = u_xlat16 / u_xlat0.x;
        u_xlat16_10 = u_xlat0.x * 0.157079637;
        u_xlat0.x = max(u_xlat16_10, 9.99999975e-05);
        u_xlat0.x = sqrt(u_xlat0.x);
        u_xlat16_10 = u_xlat8 * 0.280000001;
        u_xlat8 = (-u_xlat16_10) * u_xlat16_2.x + 1.0;
        u_xlat16_2.x = u_xlat8 * u_xlat0.x;
        u_xlat16_2.x = max(u_xlat16_2.x, 0.0);
        u_xlat16_2.x = u_xlat16_11 * u_xlat16_2.x;
        u_xlat16_2.xyz = u_xlat16_2.xxx * _LightColor0.xyz;
        u_xlat16_2.xyz = u_xlat16_2.xyz * _SpecularColor.xyz;
        SV_Target0.xyz = u_xlat16_3.xzw * u_xlat16_4.www + u_xlat16_2.xyz;
        SV_Target0.w = u_xlat16_4.w;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v4: s0p0_FORWARDBASE_Vertex_GLES3_99.glsl  |  keyword material: TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR _RECEIVE_SHADOWS_OFF
            #if VTAG_ID == 4
            struct appdata_v4 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v4 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v4 vert_v4(appdata_v4 v) {
        v2f_v4 o = (v2f_v4)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        o.vs_NORMAL0.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v4(v2f_v4 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float3 u_xlat16_0;
        float4 u_xlat1;
        float3 u_xlat2;
        float3 u_xlat16_2;
        bool2 u_xlatb2;
        float3 u_xlat16_3;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float u_xlat16_10;
        float u_xlat16_15;
        float u_xlat16;
    
        u_xlat16_0.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_0.x = rsqrt(u_xlat16_0.x);
        u_xlat16_0.xyz = u_xlat16_0.xxx * _WorldSpaceLightPos0.xyz;
        u_xlat1.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat1.x = rsqrt(u_xlat1.x);
        u_xlat1.xyz = u_xlat1.xxx * i.vs_NORMAL0.xyz;
        u_xlat16 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat1.xyz = ((float3)(u_xlat16)) * u_xlat1.xyz;
        u_xlat16 = dot(u_xlat1.xyz, u_xlat16_0.xyz);
        u_xlat16_0.x = u_xlat16 * 0.5 + 0.5;
        u_xlat16_5.x = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_0.x = (-u_xlat16_5.x) + u_xlat16_0.x;
        u_xlat16_10 = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_5.x = (-u_xlat16_5.x) + u_xlat16_10;
        u_xlat16_5.x = ((float)(1.0)) / u_xlat16_5.x;
        u_xlat16_0.x = u_xlat16_5.x * u_xlat16_0.x;
        u_xlat16_0.x = clamp(u_xlat16_0.x, 0.0, 1.0);
        u_xlat16_5.x = u_xlat16_0.x * -2.0 + 3.0;
        u_xlat16_0.x = u_xlat16_0.x * u_xlat16_0.x;
        u_xlat16_0.x = u_xlat16_0.x * u_xlat16_5.x;
        u_xlat16_5.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_0.xyz = u_xlat16_0.xxx * u_xlat16_5.xyz + _SColor.xyz;
        u_xlat16_0.xyz = u_xlat16_0.xyz * _LightColor0.xyz;
        u_xlat16_2.xyz = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy).xyz;
        u_xlat16_3.xyz = u_xlat16_2.xyz * _BaseColor.xyz;
        u_xlat16_0.xyz = u_xlat16_0.xyz * u_xlat16_3.xyz;
        u_xlat2.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_15 = dot(u_xlat2.xyz, u_xlat2.xyz);
        u_xlat16 = max(u_xlat16_15, 0.00100000005);
        u_xlat16_15 = rsqrt(u_xlat16);
        u_xlat16_4.xyz = ((float3)(u_xlat16_15)) * u_xlat2.xyz;
        u_xlatb2.xy = ((float4(0.0, 0.0, 0.0, 0.0)) < (float4(_SingleIndirectColor, _IndirectIntensity, _SingleIndirectColor, _SingleIndirectColor))).xy;
        u_xlat1.xyz = (u_xlatb2.x) ? u_xlat16_4.xyz : u_xlat1.xyz;
        u_xlat1.w = 1.0;
        u_xlat16_4.x = dot(unity_SHAr, u_xlat1);
        u_xlat16_4.y = dot(unity_SHAg, u_xlat1);
        u_xlat16_4.z = dot(unity_SHAb, u_xlat1);
        u_xlat16_4.xyz = max(u_xlat16_4.xyz, float3(0.0, 0.0, 0.0));
        u_xlat1.xyz = log2(u_xlat16_4.xyz);
        u_xlat1.xyz = u_xlat1.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat1.xyz = exp2(u_xlat1.xyz);
        u_xlat1.xyz = u_xlat1.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat1.xyz = max(u_xlat1.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_3.xyz = u_xlat16_3.xyz * u_xlat1.xyz;
        u_xlat16_3.xyz = u_xlat16_3.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity))) + u_xlat16_0.xyz;
        SV_Target0.xyz = (u_xlatb2.y) ? u_xlat16_3.xyz : u_xlat16_0.xyz;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v5: s0p0_FORWARDBASE_Vertex_GLES3_100.glsl  |  keyword material: TCP2_MOBILE TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR
            #if VTAG_ID == 5
            struct appdata_v5 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v5 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v5 vert_v5(appdata_v5 v) {
        v2f_v5 o = (v2f_v5)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        o.vs_NORMAL0.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v5(v2f_v5 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float3 u_xlat16_0;
        float4 u_xlat1;
        float3 u_xlat2;
        float3 u_xlat16_2;
        float3 u_xlat16_3;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float u_xlat16_10;
        float u_xlat16_15;
        float u_xlat16;
        bool u_xlatb16;
    
        u_xlat16_0.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_0.x = rsqrt(u_xlat16_0.x);
        u_xlat16_0.xyz = u_xlat16_0.xxx * _WorldSpaceLightPos0.xyz;
        u_xlat1.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat1.x = rsqrt(u_xlat1.x);
        u_xlat1.xyz = u_xlat1.xxx * i.vs_NORMAL0.xyz;
        u_xlat16 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat1.xyz = ((float3)(u_xlat16)) * u_xlat1.xyz;
        u_xlat16 = dot(u_xlat1.xyz, u_xlat16_0.xyz);
        u_xlat16_0.x = u_xlat16 * 0.5 + 0.5;
        u_xlat16_5.x = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_0.x = (-u_xlat16_5.x) + u_xlat16_0.x;
        u_xlat16_10 = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_5.x = (-u_xlat16_5.x) + u_xlat16_10;
        u_xlat16_5.x = ((float)(1.0)) / u_xlat16_5.x;
        u_xlat16_0.x = u_xlat16_5.x * u_xlat16_0.x;
        u_xlat16_0.x = clamp(u_xlat16_0.x, 0.0, 1.0);
        u_xlat16_5.x = u_xlat16_0.x * -2.0 + 3.0;
        u_xlat16_0.x = u_xlat16_0.x * u_xlat16_0.x;
        u_xlat16_0.x = u_xlat16_0.x * u_xlat16_5.x;
        u_xlat16_5.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_0.xyz = u_xlat16_0.xxx * u_xlat16_5.xyz + _SColor.xyz;
        u_xlat16_0.xyz = u_xlat16_0.xyz * _LightColor0.xyz;
        u_xlat2.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_15 = dot(u_xlat2.xyz, u_xlat2.xyz);
        u_xlat16 = max(u_xlat16_15, 0.00100000005);
        u_xlat16_15 = rsqrt(u_xlat16);
        u_xlat16_3.xyz = ((float3)(u_xlat16_15)) * u_xlat2.xyz;
        u_xlatb16 = 0.0<_SingleIndirectColor;
        u_xlat1.xyz = (((bool)(u_xlatb16))) ? u_xlat16_3.xyz : u_xlat1.xyz;
        u_xlat1.w = 1.0;
        u_xlat16_3.x = dot(unity_SHAr, u_xlat1);
        u_xlat16_3.y = dot(unity_SHAg, u_xlat1);
        u_xlat16_3.z = dot(unity_SHAb, u_xlat1);
        u_xlat16_3.xyz = max(u_xlat16_3.xyz, float3(0.0, 0.0, 0.0));
        u_xlat1.xyz = log2(u_xlat16_3.xyz);
        u_xlat1.xyz = u_xlat1.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat1.xyz = exp2(u_xlat1.xyz);
        u_xlat1.xyz = u_xlat1.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat1.xyz = max(u_xlat1.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_2.xyz = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy).xyz;
        u_xlat16_3.xyz = u_xlat16_2.xyz * _BaseColor.xyz;
        u_xlat16_4.xyz = u_xlat1.xyz * u_xlat16_3.xyz;
        u_xlat16_4.xyz = u_xlat16_4.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity)));
        SV_Target0.xyz = u_xlat16_3.xyz * u_xlat16_0.xyz + u_xlat16_4.xyz;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v6: s0p0_FORWARDBASE_Vertex_GLES3_101.glsl  |  keyword material: TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR _NORMALMAP
            #if VTAG_ID == 6
            struct appdata_v6 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TANGENT0 : TANGENT;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v6 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD2 : TEXCOORD3;
        float4 vs_TEXCOORD3 : TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v6 vert_v6(appdata_v6 v) {
        v2f_v6 o = (v2f_v6)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float3 u_xlat2;
        float u_xlat16_3;
        float u_xlat12;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat12 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat12 = rsqrt(u_xlat12);
        u_xlat0.xyz = ((float3)(u_xlat12)) * u_xlat0.xyz;
        o.vs_NORMAL0.xyz = u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        u_xlat1.xyz = v.in_TANGENT0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat1.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_TANGENT0.xxx + u_xlat1.xyz;
        u_xlat1.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_TANGENT0.zzz + u_xlat1.xyz;
        u_xlat12 = dot(u_xlat1.xyz, u_xlat1.xyz);
        u_xlat12 = rsqrt(u_xlat12);
        u_xlat1.xyz = ((float3)(u_xlat12)) * u_xlat1.xyz;
        o.vs_TEXCOORD2.xyz = u_xlat1.xyz;
        o.vs_TEXCOORD2.w = 0.0;
        u_xlat2.xyz = u_xlat0.zxy * u_xlat1.yzx;
        u_xlat0.xyz = u_xlat0.yzx * u_xlat1.zxy + (-u_xlat2.xyz);
        u_xlat16_3 = v.in_TANGENT0.w * unity_WorldTransformParams.w;
        o.vs_TEXCOORD3.xyz = u_xlat0.xyz * ((float3)(u_xlat16_3));
        o.vs_TEXCOORD3.w = 0.0;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v6(v2f_v6 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool2 u_xlatb0;
        float4 u_xlat1;
        float3 u_xlat16_1;
        float4 u_xlat16_2;
        float3 u_xlat16_3;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float u_xlat16_9;
        float u_xlat18;
        float u_xlat16_20;
    
        u_xlat0.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xyz = u_xlat0.xxx * i.vs_NORMAL0.xyz;
        u_xlat18 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat0.xyz = ((float3)(u_xlat18)) * u_xlat0.xyz;
        u_xlat1.xy = i.vs_TEXCOORD1.zw * _BumpMap_ST.xy + _BumpMap_ST.zw;
        u_xlat16_1.xyz = tex2D(_BumpMap, (u_xlat1.xy).xy).xyz;
        u_xlat16_2.xyz = u_xlat16_1.xyz * float3(2.0, 2.0, 2.0) + float3(-1.0, -1.0, -1.0);
        u_xlat16_2.xy = u_xlat16_2.xy * ((float2)(_BumpScale));
        u_xlat16_3.xyz = u_xlat16_2.yyy * i.vs_TEXCOORD3.xyz;
        u_xlat16_2.xyw = u_xlat16_2.xxx * i.vs_TEXCOORD2.xyz + u_xlat16_3.xyz;
        u_xlat16_2.xyz = u_xlat16_2.zzz * u_xlat0.xyz + u_xlat16_2.xyw;
        u_xlat16_20 = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_20 = rsqrt(u_xlat16_20);
        u_xlat16_3.xyz = ((float3)(u_xlat16_20)) * _WorldSpaceLightPos0.xyz;
        u_xlat0.x = dot(u_xlat16_2.xyz, u_xlat16_3.xyz);
        u_xlat16_20 = u_xlat0.x * 0.5 + 0.5;
        u_xlat16_3.x = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_20 = u_xlat16_20 + (-u_xlat16_3.x);
        u_xlat16_9 = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_3.x = (-u_xlat16_3.x) + u_xlat16_9;
        u_xlat16_3.x = ((float)(1.0)) / u_xlat16_3.x;
        u_xlat16_20 = u_xlat16_20 * u_xlat16_3.x;
        u_xlat16_20 = clamp(u_xlat16_20, 0.0, 1.0);
        u_xlat16_3.x = u_xlat16_20 * -2.0 + 3.0;
        u_xlat16_20 = u_xlat16_20 * u_xlat16_20;
        u_xlat16_20 = u_xlat16_20 * u_xlat16_3.x;
        u_xlat16_3.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_3.xyz = ((float3)(u_xlat16_20)) * u_xlat16_3.xyz + _SColor.xyz;
        u_xlat16_3.xyz = u_xlat16_3.xyz * _LightColor0.xyz;
        u_xlat16_0.xyz = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy).xyz;
        u_xlat16_4.xyz = u_xlat16_0.xyz * _BaseColor.xyz;
        u_xlat16_3.xyz = u_xlat16_3.xyz * u_xlat16_4.xyz;
        u_xlat0.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_20 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat18 = max(u_xlat16_20, 0.00100000005);
        u_xlat16_20 = rsqrt(u_xlat18);
        u_xlat16_5.xyz = u_xlat0.xyz * ((float3)(u_xlat16_20));
        u_xlatb0.xy = ((float4(0.0, 0.0, 0.0, 0.0)) < (float4(_SingleIndirectColor, _IndirectIntensity, _SingleIndirectColor, _SingleIndirectColor))).xy;
        u_xlat1.xyz = (u_xlatb0.x) ? u_xlat16_5.xyz : u_xlat16_2.xyz;
        u_xlat1.w = 1.0;
        u_xlat16_2.x = dot(unity_SHAr, u_xlat1);
        u_xlat16_2.y = dot(unity_SHAg, u_xlat1);
        u_xlat16_2.z = dot(unity_SHAb, u_xlat1);
        u_xlat16_2.xyz = max(u_xlat16_2.xyz, float3(0.0, 0.0, 0.0));
        u_xlat0.xzw = log2(u_xlat16_2.xyz);
        u_xlat0.xzw = u_xlat0.xzw * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat0.xzw = exp2(u_xlat0.xzw);
        u_xlat0.xzw = u_xlat0.xzw * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat0.xzw = max(u_xlat0.xzw, float3(0.0, 0.0, 0.0));
        u_xlat16_2.xyz = u_xlat16_4.xyz * u_xlat0.xzw;
        u_xlat16_2.xyz = u_xlat16_2.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity))) + u_xlat16_3.xyz;
        SV_Target0.xyz = (u_xlatb0.y) ? u_xlat16_2.xyz : u_xlat16_3.xyz;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v7: s0p0_FORWARDBASE_Vertex_GLES3_102.glsl  |  keyword material: TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR TCP2_SPECULAR
            #if VTAG_ID == 7
            struct appdata_v7 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v7 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v7 vert_v7(appdata_v7 v) {
        v2f_v7 o = (v2f_v7)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        o.vs_NORMAL0.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v7(v2f_v7 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float3 u_xlat0;
        float3 u_xlat1;
        float4 u_xlat16_1;
        bool2 u_xlatb1;
        float3 u_xlat16_2;
        float4 u_xlat16_3;
        float3 u_xlat16_4;
        float4 u_xlat5;
        float3 u_xlat16_5;
        float3 u_xlat16_6;
        float3 u_xlat7;
        float u_xlat8;
        float u_xlat16_10;
        float u_xlat16_11;
        float u_xlat16;
        float u_xlat16_18;
        float u_xlat16_19;
        float u_xlat24;
        float u_xlat16_24;
        bool u_xlatb24;
        float u_xlat16_26;
    
        u_xlat0.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xyz = u_xlat0.xxx * i.vs_NORMAL0.xyz;
        u_xlat24 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat0.xyz = ((float3)(u_xlat24)) * u_xlat0.xyz;
        u_xlat1.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_2.x = dot(u_xlat1.xyz, u_xlat1.xyz);
        u_xlat24 = max(u_xlat16_2.x, 0.00100000005);
        u_xlat16_2.x = rsqrt(u_xlat24);
        u_xlat16_2.xyz = u_xlat1.xyz * u_xlat16_2.xxx;
        u_xlat16_26 = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_26 = rsqrt(u_xlat16_26);
        u_xlat16_3.xyz = ((float3)(u_xlat16_26)) * _WorldSpaceLightPos0.xyz;
        u_xlat16_1 = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy);
        u_xlat16_4.xyz = u_xlat16_1.xyz * _BaseColor.xyz;
        u_xlat24 = dot(u_xlat0.xyz, u_xlat16_3.xyz);
        u_xlat16_3.x = u_xlat24 * 0.5 + 0.5;
        u_xlat16_11 = u_xlat24;
        u_xlat16_11 = clamp(u_xlat16_11, 0.0, 1.0);
        u_xlat16_19 = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_3.w = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_3.xw = (-((float2)(u_xlat16_19))) + u_xlat16_3.xw;
        u_xlat16_19 = ((float)(1.0)) / u_xlat16_3.w;
        u_xlat16_3.x = u_xlat16_19 * u_xlat16_3.x;
        u_xlat16_3.x = clamp(u_xlat16_3.x, 0.0, 1.0);
        u_xlat16_19 = u_xlat16_3.x * -2.0 + 3.0;
        u_xlat16_3.x = u_xlat16_3.x * u_xlat16_3.x;
        u_xlat16_3.x = u_xlat16_3.x * u_xlat16_19;
        u_xlat16_5.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xxx * u_xlat16_5.xyz + _SColor.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xzw * _LightColor0.xyz;
        u_xlat16_3.xzw = u_xlat16_3.xzw * u_xlat16_4.xyz;
        u_xlatb1.xy = ((float4(0.0, 0.0, 0.0, 0.0)) < (float4(_SingleIndirectColor, _IndirectIntensity, _SingleIndirectColor, _SingleIndirectColor))).xy;
        u_xlat5.xyz = (u_xlatb1.x) ? u_xlat16_2.xyz : u_xlat0.xyz;
        u_xlat5.w = 1.0;
        u_xlat16_6.x = dot(unity_SHAr, u_xlat5);
        u_xlat16_6.y = dot(unity_SHAg, u_xlat5);
        u_xlat16_6.z = dot(unity_SHAb, u_xlat5);
        u_xlat16_6.xyz = max(u_xlat16_6.xyz, float3(0.0, 0.0, 0.0));
        u_xlat7.xyz = log2(u_xlat16_6.xyz);
        u_xlat7.xyz = u_xlat7.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat7.xyz = exp2(u_xlat7.xyz);
        u_xlat7.xyz = u_xlat7.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat7.xyz = max(u_xlat7.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_4.xyz = u_xlat16_4.xyz * u_xlat7.xyz;
        u_xlat16_4.xyz = u_xlat16_4.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity))) + u_xlat16_3.xzw;
        u_xlat16_3.xzw = (u_xlatb1.y) ? u_xlat16_4.xyz : u_xlat16_3.xzw;
        u_xlatb24 = _SpecularMapType>=5.0;
        if(u_xlatb24){
            u_xlat16_24 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).w;
            u_xlat16_4.x = u_xlat16_24;
        } else {
            u_xlatb24 = _SpecularMapType>=4.0;
            if(u_xlatb24){
                u_xlat16_24 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).z;
                u_xlat16_4.x = u_xlat16_24;
            } else {
                u_xlatb24 = _SpecularMapType>=3.0;
                if(u_xlatb24){
                    u_xlat16_24 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).y;
                    u_xlat16_4.x = u_xlat16_24;
                } else {
                    u_xlatb24 = _SpecularMapType>=2.0;
                    if(u_xlatb24){
                        u_xlat16_24 = tex2D(_SpecGlossMap, (i.vs_TEXCOORD1.xy).xy).x;
                        u_xlat16_4.x = u_xlat16_24;
                    } else {
                        u_xlatb24 = _SpecularMapType>=1.0;
                        u_xlat16_4.x = (u_xlatb24) ? u_xlat16_1.w : 1.0;
                    }
                }
            }
        }
        u_xlat16_2.xyz = _WorldSpaceLightPos0.xyz * ((float3)(u_xlat16_26)) + u_xlat16_2.xyz;
        u_xlat16_26 = dot(u_xlat16_2.xyz, u_xlat16_2.xyz);
        u_xlat24 = max(u_xlat16_26, 0.00100000005);
        u_xlat16_26 = rsqrt(u_xlat24);
        u_xlat16_2.xyz = ((float3)(u_xlat16_26)) * u_xlat16_2.xyz;
        u_xlat0.x = dot(u_xlat0.xyz, u_xlat16_2.xyz);
        u_xlat0.x = clamp(u_xlat0.x, 0.0, 1.0);
        u_xlat16_2.x = max(_SpecularRoughness, 9.99999975e-06);
        u_xlat16_2.x = u_xlat16_4.x * u_xlat16_2.x;
        u_xlat8 = u_xlat16_2.x * u_xlat16_2.x;
        u_xlat16_10 = min(u_xlat8, 1.0);
        u_xlat16_10 = u_xlat16_10 * u_xlat16_10;
        u_xlat16_18 = u_xlat0.x * u_xlat16_10 + (-u_xlat0.x);
        u_xlat0.x = u_xlat16_18 * u_xlat0.x + 1.0;
        u_xlat16 = u_xlat16_10 * 0.318309873;
        u_xlat0.x = u_xlat0.x * u_xlat0.x + 9.99999975e-05;
        u_xlat0.x = u_xlat16 / u_xlat0.x;
        u_xlat16_10 = u_xlat0.x * 0.157079637;
        u_xlat0.x = max(u_xlat16_10, 9.99999975e-05);
        u_xlat0.x = sqrt(u_xlat0.x);
        u_xlat16_10 = u_xlat8 * 0.280000001;
        u_xlat8 = (-u_xlat16_10) * u_xlat16_2.x + 1.0;
        u_xlat16_2.x = u_xlat8 * u_xlat0.x;
        u_xlat16_2.x = max(u_xlat16_2.x, 0.0);
        u_xlat16_2.x = u_xlat16_11 * u_xlat16_2.x;
        u_xlat16_2.xyz = u_xlat16_2.xxx * _LightColor0.xyz;
        SV_Target0.xyz = u_xlat16_2.xyz * _SpecularColor.xyz + u_xlat16_3.xzw;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v8: s0p0_FORWARDBASE_Vertex_GLES3_106.glsl  |  keyword material: TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK TCP2_SHADOW_LIGHT_COLOR _ALPHAPREMULTIPLY_ON
            #if VTAG_ID == 8
            struct appdata_v8 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v8 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v8 vert_v8(appdata_v8 v) {
        v2f_v8 o = (v2f_v8)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        o.vs_NORMAL0.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v8(v2f_v8 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float3 u_xlat16_0;
        float4 u_xlat1;
        float4 u_xlat16_2;
        float3 u_xlat3;
        bool2 u_xlatb3;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float u_xlat16_10;
        float u_xlat16_15;
        float u_xlat16;
    
        u_xlat16_0.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_0.x = rsqrt(u_xlat16_0.x);
        u_xlat16_0.xyz = u_xlat16_0.xxx * _WorldSpaceLightPos0.xyz;
        u_xlat1.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat1.x = rsqrt(u_xlat1.x);
        u_xlat1.xyz = u_xlat1.xxx * i.vs_NORMAL0.xyz;
        u_xlat16 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat1.xyz = ((float3)(u_xlat16)) * u_xlat1.xyz;
        u_xlat16 = dot(u_xlat1.xyz, u_xlat16_0.xyz);
        u_xlat16_0.x = u_xlat16 * 0.5 + 0.5;
        u_xlat16_5.x = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_0.x = (-u_xlat16_5.x) + u_xlat16_0.x;
        u_xlat16_10 = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_5.x = (-u_xlat16_5.x) + u_xlat16_10;
        u_xlat16_5.x = ((float)(1.0)) / u_xlat16_5.x;
        u_xlat16_0.x = u_xlat16_5.x * u_xlat16_0.x;
        u_xlat16_0.x = clamp(u_xlat16_0.x, 0.0, 1.0);
        u_xlat16_5.x = u_xlat16_0.x * -2.0 + 3.0;
        u_xlat16_0.x = u_xlat16_0.x * u_xlat16_0.x;
        u_xlat16_0.x = u_xlat16_0.x * u_xlat16_5.x;
        u_xlat16_5.xyz = (-_SColor.xyz) + _HColor.xyz;
        u_xlat16_0.xyz = u_xlat16_0.xxx * u_xlat16_5.xyz + _SColor.xyz;
        u_xlat16_0.xyz = u_xlat16_0.xyz * _LightColor0.xyz;
        u_xlat16_2 = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy);
        u_xlat16_2 = u_xlat16_2 * _BaseColor;
        u_xlat16_0.xyz = u_xlat16_0.xyz * u_xlat16_2.xyz;
        u_xlat3.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_15 = dot(u_xlat3.xyz, u_xlat3.xyz);
        u_xlat16 = max(u_xlat16_15, 0.00100000005);
        u_xlat16_15 = rsqrt(u_xlat16);
        u_xlat16_4.xyz = ((float3)(u_xlat16_15)) * u_xlat3.xyz;
        u_xlatb3.xy = ((float4(0.0, 0.0, 0.0, 0.0)) < (float4(_SingleIndirectColor, _IndirectIntensity, _SingleIndirectColor, _SingleIndirectColor))).xy;
        u_xlat1.xyz = (u_xlatb3.x) ? u_xlat16_4.xyz : u_xlat1.xyz;
        u_xlat1.w = 1.0;
        u_xlat16_4.x = dot(unity_SHAr, u_xlat1);
        u_xlat16_4.y = dot(unity_SHAg, u_xlat1);
        u_xlat16_4.z = dot(unity_SHAb, u_xlat1);
        u_xlat16_4.xyz = max(u_xlat16_4.xyz, float3(0.0, 0.0, 0.0));
        u_xlat1.xyz = log2(u_xlat16_4.xyz);
        u_xlat1.xyz = u_xlat1.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat1.xyz = exp2(u_xlat1.xyz);
        u_xlat1.xyz = u_xlat1.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat1.xyz = max(u_xlat1.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_4.xyz = u_xlat16_2.xyz * u_xlat1.xyz;
        u_xlat16_4.xyz = u_xlat16_4.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity))) + u_xlat16_0.xyz;
        u_xlat16_0.xyz = (u_xlatb3.y) ? u_xlat16_4.xyz : u_xlat16_0.xyz;
        SV_Target0.xyz = u_xlat16_2.www * u_xlat16_0.xyz;
        SV_Target0.w = u_xlat16_2.w;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v9: s0p0_FORWARDBASE_Vertex_GLES3_97.glsl  |  keyword material: TCP2_REFLECTIONS_FRESNEL TCP2_RIM_LIGHTING_LIGHTMASK
            #if VTAG_ID == 9
            struct appdata_v9 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v9 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_TEXCOORD0 : TEXCOORD1;
        float4 vs_TEXCOORD1 : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v9 vert_v9(appdata_v9 v) {
        v2f_v9 o = (v2f_v9)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 vs_TEXCOORD7;
        float3 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0.xyz = v.in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[0].xyz * v.in_POSITION0.xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[2].xyz * v.in_POSITION0.zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.pos = u_xlat1 + transpose(unity_MatrixVP)[3];
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        o.vs_NORMAL0.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v9(v2f_v9 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float3 u_xlat0;
        float4 u_xlat1;
        float3 u_xlat16_1;
        float4 u_xlat2;
        bool2 u_xlatb2;
        float3 u_xlat16_3;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float3 u_xlat16_6;
        float u_xlat21;
        float u_xlat16_24;
        float u_xlat16_26;
    
        u_xlat0.xyz = (-i.vs_TEXCOORD0.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_1.x = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat21 = max(u_xlat16_1.x, 0.00100000005);
        u_xlat16_1.x = rsqrt(u_xlat21);
        u_xlat16_1.xyz = u_xlat0.xyz * u_xlat16_1.xxx;
        u_xlat0.x = dot(i.vs_NORMAL0.xyz, i.vs_NORMAL0.xyz);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xyz = u_xlat0.xxx * i.vs_NORMAL0.xyz;
        u_xlat21 = (((vface > 0) ? 0xffffffffu : ((uint)(0))) != ((uint)(0))) ? 1.0 : -1.0;
        u_xlat0.xyz = ((float3)(u_xlat21)) * u_xlat0.xyz;
        u_xlatb2.xy = ((float4(0.0, 0.0, 0.0, 0.0)) < (float4(_SingleIndirectColor, _IndirectIntensity, _SingleIndirectColor, _SingleIndirectColor))).xy;
        u_xlat1.xyz = (u_xlatb2.x) ? u_xlat16_1.xyz : u_xlat0.xyz;
        u_xlat1.w = 1.0;
        u_xlat16_3.x = dot(unity_SHAr, u_xlat1);
        u_xlat16_3.y = dot(unity_SHAg, u_xlat1);
        u_xlat16_3.z = dot(unity_SHAb, u_xlat1);
        u_xlat16_3.xyz = max(u_xlat16_3.xyz, float3(0.0, 0.0, 0.0));
        u_xlat2.xzw = log2(u_xlat16_3.xyz);
        u_xlat2.xzw = u_xlat2.xzw * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat2.xzw = exp2(u_xlat2.xzw);
        u_xlat2.xzw = u_xlat2.xzw * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat2.xzw = max(u_xlat2.xzw, float3(0.0, 0.0, 0.0));
        u_xlat16_4.xyz = tex2D(_BaseMap, (i.vs_TEXCOORD1.xy).xy).xyz;
        u_xlat16_3.xyz = u_xlat16_4.xyz * _BaseColor.xyz;
        u_xlat16_5.xyz = u_xlat2.xzw * u_xlat16_3.xyz;
        u_xlat16_24 = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_24 = rsqrt(u_xlat16_24);
        u_xlat16_6.xyz = ((float3)(u_xlat16_24)) * _WorldSpaceLightPos0.xyz;
        u_xlat0.x = dot(u_xlat0.xyz, u_xlat16_6.xyz);
        u_xlat16_24 = u_xlat0.x * 0.5 + 0.5;
        u_xlat16_26 = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_24 = u_xlat16_24 + (-u_xlat16_26);
        u_xlat16_6.x = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_26 = (-u_xlat16_26) + u_xlat16_6.x;
        u_xlat16_26 = ((float)(1.0)) / u_xlat16_26;
        u_xlat16_24 = u_xlat16_24 * u_xlat16_26;
        u_xlat16_24 = clamp(u_xlat16_24, 0.0, 1.0);
        u_xlat16_26 = u_xlat16_24 * -2.0 + 3.0;
        u_xlat16_24 = u_xlat16_24 * u_xlat16_24;
        u_xlat16_24 = u_xlat16_24 * u_xlat16_26;
        u_xlat16_6.xyz = _HColor.xyz * _LightColor0.xyz + (-_SColor.xyz);
        u_xlat16_6.xyz = ((float3)(u_xlat16_24)) * u_xlat16_6.xyz + _SColor.xyz;
        u_xlat16_3.xyz = u_xlat16_3.xyz * u_xlat16_6.xyz;
        u_xlat16_5.xyz = u_xlat16_5.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity))) + u_xlat16_3.xyz;
        SV_Target0.xyz = (u_xlatb2.y) ? u_xlat16_5.xyz : u_xlat16_3.xyz;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #define BB_CAT2(a, b) a##b
            #define BB_CAT(a, b) BB_CAT2(a, b)
            BB_CAT(v2f_, VTAG) vert_entry(BB_CAT(appdata_, VTAG) v) { return BB_CAT(vert_, VTAG)(v); }
            float4 frag_entry(BB_CAT(v2f_, VTAG) i, float vface : VFACE) : SV_Target { return BB_CAT(frag_, VTAG)(i, vface); }
            ENDCG
        }
    }
    Fallback Off
}