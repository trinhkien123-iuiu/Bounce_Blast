// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "Toony Colors Pro 2/Hybrid Shader 2 Directional Cube"
{
    Properties
    {
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull ("Render Face", Float) = 2.0
        [ToggleUI] _ZWrite ("Depth Write", Float) = 1.0
        [Toggle(_ALPHATEST_ON)] _UseAlphaTest ("Alpha Clipping", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _BaseColor ("Color", Color) = (1,1,1,1)
        _CubeShadeOffset ("Cube Shade Offset", Range(-0.25, 0.25)) = 0.0
        _UseBakedCubeShadeOffset ("Use Baked Cube Shade Offset", Float) = 0.0
        _BaseMap ("Albedo", 2D) = "white" { }
        _HColor ("Highlight Color", Color) = (1,1,1,1)
        _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
        [Toggle(TCP2_SHADOW_LIGHT_COLOR)] _ShadowColorLightAtten ("Main Light affects Shadow Color", Float) = 1.0
        _RampType ("Ramp Type", Float) = 0.0
        _Ramp ("Ramp Texture (RGB)", 2D) = "gray" { }
        _RampScale ("Scale", Float) = 1.0
        _RampOffset ("Offset", Float) = 0.0
        [PowerSlider(0.415)] _RampThreshold ("Threshold", Range(0.01, 1)) = 0.75
        _RampSmoothing ("Smoothing", Range(0, 1)) = 0.10000000149011612
        [IntRange] _RampBands ("Bands Count", Range(1, 20)) = 4.0
        _RampBandsSmoothing ("Bands Smoothing", Range(0, 1)) = 0.10000000149011612
        [Toggle(TCP2_SPECULAR)] _UseSpecular ("Specular", Float) = 0.0
        _SpecularType ("Type", Float) = 0.0
        _SpecularColor ("Color", Color) = (0.75,0.75,0.75,1)
        [PowerSlider(5.0)] _SpecularToonSize ("Size", Range(0.001, 1)) = 0.25
        _SpecularToonSmoothness ("Smoothing", Range(0, 1)) = 0.05000000074505806
        _SpecularRoughness ("Roughness", Range(0, 1)) = 0.5
        [Enum(Disabled,0,Albedo Alpha,1,Custom R,2,Custom G,3,Custom B,4,Custom A,5)] _SpecularMapType ("Specular Map#Specular Map (A)", Float) = 0.0
        _SpecGlossMap ("Specular Texture", 2D) = "white" { }
        _IndirectIntensity ("Strength", Range(0, 1)) = 1.0
        [ToggleUI] _SingleIndirectColor ("Single Indirect Color", Float) = 0.0
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
            #pragma shader_feature_local __ TCP2_SHADOW_LIGHT_COLOR
            #pragma shader_feature_local __ TCP2_SPECULAR
            #pragma shader_feature_local __ _RECEIVE_SHADOWS_OFF
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _BaseColor;
            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _HColor;
            float _IndirectIntensity;
            float _RampSmoothing;
            float _RampThreshold;
            float4 _SColor;
            float _SingleIndirectColor;
            sampler2D _SpecGlossMap;
            float4 _SpecularColor;
            float _SpecularRoughness;
            UNITY_INSTANCING_BUFFER_START(CubeProps)
                UNITY_DEFINE_INSTANCED_PROP(float, _CubeShadeOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseBakedCubeShadeOffset)
            UNITY_INSTANCING_BUFFER_END(CubeProps)
        
            #if defined(TCP2_MOBILE) && defined(TCP2_SHADOW_LIGHT_COLOR) && defined(TCP2_SPECULAR) && defined(_RECEIVE_SHADOWS_OFF)
                #define VTAG v0
        #define VTAG_ID 0
            #else
                #define VTAG v1
        #define VTAG_ID 1
            #endif
            // ---- v0: s0p0_FORWARDBASE_Vertex_GLES3_13.glsl  |  keyword material: TCP2_MOBILE TCP2_SHADOW_LIGHT_COLOR TCP2_SPECULAR _RECEIVE_SHADOWS_OFF
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_COLOR0 : COLOR;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_COLOR0 : TEXCOORD1;
        float4 vs_TEXCOORD0 : TEXCOORD2;
        float4 vs_TEXCOORD1 : TEXCOORD3;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
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
        o.vs_COLOR0 = v.in_COLOR0;
        o.vs_TEXCOORD0.w = 0.0;
        o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
        o.vs_TEXCOORD1.zw = v.in_TEXCOORD0.xy;
        vs_TEXCOORD7 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float4 u_xlat0;
        bool u_xlatb0;
        float3 u_xlat16_1;
        float4 u_xlat16_2;
        float3 u_xlat16_3;
        float u_xlat4;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        bool u_xlatb6;
        float u_xlat16_8;
        float u_xlat10;
        bool u_xlatb12;
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
        u_xlat16_1.xyz = u_xlat16_2.xzw * _SpecularColor.xyz + u_xlat16_1.xyz;
        u_xlat0.x = i.vs_COLOR0.w * 255.0 + 0.5;
        u_xlat0.x = floor(u_xlat0.x);
        u_xlatb6 = 0.0>=u_xlat0.x;
        u_xlatb12 = u_xlat0.x>=255.0;
        u_xlat0.x = u_xlat0.x + -1.0;
        u_xlat0.x = u_xlat0.x * 0.00197628466 + -0.25;
        u_xlatb6 = u_xlatb12 || u_xlatb6;
        u_xlat16_19 = (u_xlatb6) ? 0.0 : u_xlat0.x;
        u_xlatb0 = 0.5<UNITY_ACCESS_INSTANCED_PROP(CubeProps, _UseBakedCubeShadeOffset);
        u_xlat0.x = (u_xlatb0) ? u_xlat16_19 : 0.0;
        u_xlat16_19 = u_xlat0.x + UNITY_ACCESS_INSTANCED_PROP(CubeProps, _CubeShadeOffset);
        u_xlat0.x = max(u_xlat16_19, -0.25);
        u_xlat0.x = min(u_xlat0.x, 0.25);
        u_xlatb6 = u_xlat0.x<0.0;
        u_xlat16_19 = (u_xlatb6) ? 1.0 : 0.0;
        u_xlat16_2.xyz = (-u_xlat16_1.xyz) + ((float3)(u_xlat16_19));
        SV_Target0.xyz = abs(u_xlat0.xxx) * u_xlat16_2.xyz + u_xlat16_1.xyz;
        SV_Target0.w = 1.0;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            // ---- v1: s0p0_FORWARDBASE_Vertex_GLES3_14.glsl  |  keyword material: (không keyword)
            #if VTAG_ID == 1
            struct appdata_v1 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_COLOR0 : COLOR;
        float4 in_TEXCOORD0 : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v1 {
        float4 pos : SV_POSITION;
        float3 vs_NORMAL0 : TEXCOORD0;
        float4 vs_COLOR0 : TEXCOORD1;
        float4 vs_TEXCOORD0 : TEXCOORD2;
        float4 vs_TEXCOORD1 : TEXCOORD3;
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
        o.vs_COLOR0 = v.in_COLOR0;
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
        float3 u_xlat0;
        bool u_xlatb0;
        float4 u_xlat1;
        float3 u_xlat16_1;
        float4 u_xlat2;
        bool2 u_xlatb2;
        float3 u_xlat16_3;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float3 u_xlat16_6;
        bool u_xlatb7;
        bool u_xlatb14;
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
        u_xlat16_3.xyz = (u_xlatb2.y) ? u_xlat16_5.xyz : u_xlat16_3.xyz;
        u_xlat0.x = i.vs_COLOR0.w * 255.0 + 0.5;
        u_xlat0.x = floor(u_xlat0.x);
        u_xlatb7 = 0.0>=u_xlat0.x;
        u_xlatb14 = u_xlat0.x>=255.0;
        u_xlat0.x = u_xlat0.x + -1.0;
        u_xlat0.x = u_xlat0.x * 0.00197628466 + -0.25;
        u_xlatb7 = u_xlatb14 || u_xlatb7;
        u_xlat16_24 = (u_xlatb7) ? 0.0 : u_xlat0.x;
        u_xlatb0 = 0.5<UNITY_ACCESS_INSTANCED_PROP(CubeProps, _UseBakedCubeShadeOffset);
        u_xlat0.x = (u_xlatb0) ? u_xlat16_24 : 0.0;
        u_xlat16_24 = u_xlat0.x + UNITY_ACCESS_INSTANCED_PROP(CubeProps, _CubeShadeOffset);
        u_xlat0.x = max(u_xlat16_24, -0.25);
        u_xlat0.x = min(u_xlat0.x, 0.25);
        u_xlatb7 = u_xlat0.x<0.0;
        u_xlat16_24 = (u_xlatb7) ? 1.0 : 0.0;
        u_xlat16_5.xyz = (-u_xlat16_3.xyz) + ((float3)(u_xlat16_24));
        SV_Target0.xyz = abs(u_xlat0.xxx) * u_xlat16_5.xyz + u_xlat16_3.xyz;
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