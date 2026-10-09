// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "Custom/UltimateFlow_Maskable"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" { }
        _GradientTex ("Flow Doku (Gradient)", 2D) = "white" { }
        _ScrollSpeed ("Ana Akış Hızı (X, Y)", Vector) = (0.5,0,0,0)
        _GradScrollSpeed ("Flow Hızı (X, Y)", Vector) = (0.2,0.2,0,0)
        _Intensity ("Flow Şiddeti", Range(0, 5)) = 1.0
        [KeywordEnum(Additive, Multiply, Screen, Overlay, Darken)] _BlendMode ("Karışım Modu", Float) = 0.0
        _StencilComp ("Stencil Comparison", Float) = 8.0
        _Stencil ("Stencil ID", Float) = 0.0
        _StencilOp ("Stencil Operation", Float) = 0.0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255.0
        _StencilReadMask ("Stencil Read Mask", Float) = 255.0
        _ColorMask ("Color Mask", Float) = 15.0
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0.0
    }
    SubShader
    {
        Tags { "CanUseSpriteAtlas"="False" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "CanUseSpriteAtlas"="False" "PreviewType"="Plane" }
            Cull Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask [_ColorMask]
            Stencil {
                Ref [_Stencil]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilOp]
                Fail Keep
                ZFail Keep
            }
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #pragma shader_feature_local __ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local __ _BLENDMODE_ADDITIVE
            #pragma shader_feature_local __ _BLENDMODE_DARKEN
            #pragma shader_feature_local __ _BLENDMODE_MULTIPLY
            #pragma shader_feature_local __ _BLENDMODE_OVERLAY
            #pragma shader_feature_local __ _BLENDMODE_SCREEN
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _ClipRect;
            float2 _GradScrollSpeed;
            sampler2D _GradientTex;
            float4 _GradientTex_ST;
            float _Intensity;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _ScrollSpeed;
        
            #if defined(UNITY_UI_ALPHACLIP) && defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_OVERLAY) && !defined(_BLENDMODE_SCREEN)
                #define VTAG v0
        #define VTAG_ID 0
            #elif defined(UNITY_UI_ALPHACLIP) && defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_OVERLAY) && !defined(_BLENDMODE_SCREEN)
                #define VTAG v1
        #define VTAG_ID 1
            #elif defined(UNITY_UI_ALPHACLIP) && defined(_BLENDMODE_SCREEN) && !defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_OVERLAY)
                #define VTAG v2
        #define VTAG_ID 2
            #elif defined(UNITY_UI_ALPHACLIP) && defined(_BLENDMODE_OVERLAY) && !defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_SCREEN)
                #define VTAG v3
        #define VTAG_ID 3
            #elif defined(UNITY_UI_ALPHACLIP) && defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_OVERLAY) && !defined(_BLENDMODE_SCREEN)
                #define VTAG v4
        #define VTAG_ID 4
            #elif defined(_BLENDMODE_ADDITIVE) && !defined(UNITY_UI_ALPHACLIP) && !defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_OVERLAY) && !defined(_BLENDMODE_SCREEN)
                #define VTAG v5
        #define VTAG_ID 5
            #elif defined(_BLENDMODE_MULTIPLY) && !defined(UNITY_UI_ALPHACLIP) && !defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_OVERLAY) && !defined(_BLENDMODE_SCREEN)
                #define VTAG v6
        #define VTAG_ID 6
            #elif defined(_BLENDMODE_SCREEN) && !defined(UNITY_UI_ALPHACLIP) && !defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_OVERLAY)
                #define VTAG v7
        #define VTAG_ID 7
            #elif defined(_BLENDMODE_OVERLAY) && !defined(UNITY_UI_ALPHACLIP) && !defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_DARKEN) && !defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_SCREEN)
                #define VTAG v8
        #define VTAG_ID 8
            #elif defined(_BLENDMODE_DARKEN) && !defined(UNITY_UI_ALPHACLIP) && !defined(_BLENDMODE_ADDITIVE) && !defined(_BLENDMODE_MULTIPLY) && !defined(_BLENDMODE_OVERLAY) && !defined(_BLENDMODE_SCREEN)
                #define VTAG v9
        #define VTAG_ID 9
            #else
                #define VTAG v0
        #define VTAG_ID 0
            #endif
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float2 u_xlat3;
        float u_xlat16_5;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat3.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat3.xy = frac(u_xlat3.xy);
        u_xlat3.xy = u_xlat3.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat3.xy = -abs(u_xlat3.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat3.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat16_5 = u_xlat16_2.x * u_xlat0.x + -0.00100000005;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlatb0.x = u_xlat16_5<0.0;
        if(u_xlatb0.x){discard;}
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat16_2.xyz = u_xlat16_0.xyz * ((float3)(_Intensity)) + u_xlat16_1.xyz;
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 1
            struct appdata_v1 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v1 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v1 vert_v1(appdata_v1 v) {
        v2f_v1 o = (v2f_v1)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v1(v2f_v1 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float2 u_xlat3;
        float u_xlat16_5;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat3.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat3.xy = frac(u_xlat3.xy);
        u_xlat3.xy = u_xlat3.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat3.xy = -abs(u_xlat3.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat3.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat16_5 = u_xlat16_2.x * u_xlat0.x + -0.00100000005;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlatb0.x = u_xlat16_5<0.0;
        if(u_xlatb0.x){discard;}
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat0.xyz = u_xlat16_0.xyz * ((float3)(_Intensity));
        u_xlat16_2.xyz = u_xlat0.xyz * u_xlat16_1.xyz;
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 2
            struct appdata_v2 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v2 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v2 vert_v2(appdata_v2 v) {
        v2f_v2 o = (v2f_v2)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v2(v2f_v2 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float3 u_xlat16_3;
        float2 u_xlat4;
        float3 u_xlat16_6;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat4.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat4.xy = frac(u_xlat4.xy);
        u_xlat4.xy = u_xlat4.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat4.xy = -abs(u_xlat4.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat4.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat16_6.xyz = (-u_xlat16_1.xyz) + float3(1.0, 1.0, 1.0);
        u_xlat16_3.x = u_xlat16_2.x * u_xlat0.x + -0.00100000005;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlatb0.x = u_xlat16_3.x<0.0;
        if(u_xlatb0.x){discard;}
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat16_3.xyz = (-u_xlat16_0.xyz) * ((float3)(_Intensity)) + float3(1.0, 1.0, 1.0);
        u_xlat16_2.xyz = (-u_xlat16_6.xyz) * u_xlat16_3.xyz + float3(1.0, 1.0, 1.0);
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 3
            struct appdata_v3 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v3 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v3 vert_v3(appdata_v3 v) {
        v2f_v3 o = (v2f_v3)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v3(v2f_v3 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        bool3 u_xlatb1;
        float3 u_xlat16_2;
        float3 u_xlat16_3;
        float2 u_xlat4;
        float u_xlat16_6;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat4.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat4.xy = frac(u_xlat4.xy);
        u_xlat4.xy = u_xlat4.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat4.xy = -abs(u_xlat4.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat4.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat16_6 = u_xlat16_2.x * u_xlat0.x + -0.00100000005;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlatb0.x = u_xlat16_6<0.0;
        if(u_xlatb0.x){discard;}
        u_xlat16_2.xyz = (-u_xlat16_1.xyz) + float3(1.0, 1.0, 1.0);
        u_xlat16_2.xyz = u_xlat16_2.xyz + u_xlat16_2.xyz;
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat16_3.xyz = (-u_xlat16_0.xyz) * ((float3)(_Intensity)) + float3(1.0, 1.0, 1.0);
        u_xlat0.xyz = u_xlat16_0.xyz * ((float3)(_Intensity));
        u_xlat16_2.xyz = (-u_xlat16_2.xyz) * u_xlat16_3.xyz + float3(1.0, 1.0, 1.0);
        u_xlat16_3.xyz = u_xlat16_1.xyz + u_xlat16_1.xyz;
        u_xlatb1.xyz = ((u_xlat16_1.xyzx) < (float4(0.5, 0.5, 0.5, 0.0))).xyz;
        u_xlat16_3.xyz = u_xlat0.xyz * u_xlat16_3.xyz;
        {
            float3 hlslcc_movcTemp = u_xlat16_2;
            hlslcc_movcTemp.x = (u_xlatb1.x) ? u_xlat16_3.x : u_xlat16_2.x;
            hlslcc_movcTemp.y = (u_xlatb1.y) ? u_xlat16_3.y : u_xlat16_2.y;
            hlslcc_movcTemp.z = (u_xlatb1.z) ? u_xlat16_3.z : u_xlat16_2.z;
            u_xlat16_2 = hlslcc_movcTemp;
        }
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 4
            struct appdata_v4 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v4 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v4 vert_v4(appdata_v4 v) {
        v2f_v4 o = (v2f_v4)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v4(v2f_v4 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float2 u_xlat3;
        float u_xlat16_5;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat3.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat3.xy = frac(u_xlat3.xy);
        u_xlat3.xy = u_xlat3.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat3.xy = -abs(u_xlat3.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat3.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat16_5 = u_xlat16_2.x * u_xlat0.x + -0.00100000005;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlatb0.x = u_xlat16_5<0.0;
        if(u_xlatb0.x){discard;}
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat0.xyz = u_xlat16_0.xyz * ((float3)(_Intensity));
        u_xlat16_2.xyz = min(u_xlat0.xyz, u_xlat16_1.xyz);
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 5
            struct appdata_v5 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v5 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v5 vert_v5(appdata_v5 v) {
        v2f_v5 o = (v2f_v5)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v5(v2f_v5 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float2 u_xlat3;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat3.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat3.xy = frac(u_xlat3.xy);
        u_xlat3.xy = u_xlat3.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat3.xy = -abs(u_xlat3.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat3.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat16_2.xyz = u_xlat16_0.xyz * ((float3)(_Intensity)) + u_xlat16_1.xyz;
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 6
            struct appdata_v6 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v6 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v6 vert_v6(appdata_v6 v) {
        v2f_v6 o = (v2f_v6)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v6(v2f_v6 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float2 u_xlat3;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat3.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat3.xy = frac(u_xlat3.xy);
        u_xlat3.xy = u_xlat3.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat3.xy = -abs(u_xlat3.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat3.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat0.xyz = u_xlat16_0.xyz * ((float3)(_Intensity));
        u_xlat16_2.xyz = u_xlat0.xyz * u_xlat16_1.xyz;
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 7
            struct appdata_v7 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v7 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v7 vert_v7(appdata_v7 v) {
        v2f_v7 o = (v2f_v7)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v7(v2f_v7 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float3 u_xlat16_3;
        float2 u_xlat4;
        float3 u_xlat16_6;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat4.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat4.xy = frac(u_xlat4.xy);
        u_xlat4.xy = u_xlat4.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat4.xy = -abs(u_xlat4.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat4.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat16_6.xyz = (-u_xlat16_1.xyz) + float3(1.0, 1.0, 1.0);
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat16_3.xyz = (-u_xlat16_0.xyz) * ((float3)(_Intensity)) + float3(1.0, 1.0, 1.0);
        u_xlat16_2.xyz = (-u_xlat16_6.xyz) * u_xlat16_3.xyz + float3(1.0, 1.0, 1.0);
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 8
            struct appdata_v8 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v8 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v8 vert_v8(appdata_v8 v) {
        v2f_v8 o = (v2f_v8)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v8(v2f_v8 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float4 u_xlat16_0;
        bool4 u_xlatb0;
        float3 u_xlat16_1;
        float3 u_xlat2;
        float3 u_xlat16_2;
        float3 u_xlat16_3;
        float u_xlat16_13;
    
        u_xlat0.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat0.xy = frac(u_xlat0.xy);
        u_xlat0.xy = u_xlat0.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat0.xy = -abs(u_xlat0.xy) + float2(1.0, 1.0);
        u_xlat16_0 = tex2D(_MainTex, (u_xlat0.xy).xy);
        u_xlat16_1.xyz = (-u_xlat16_0.xyz) + float3(1.0, 1.0, 1.0);
        u_xlat16_1.xyz = u_xlat16_1.xyz + u_xlat16_1.xyz;
        u_xlat2.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_2.xyz = tex2D(_GradientTex, (u_xlat2.xy).xy).xyz;
        u_xlat16_3.xyz = (-u_xlat16_2.xyz) * ((float3)(_Intensity)) + float3(1.0, 1.0, 1.0);
        u_xlat2.xyz = u_xlat16_2.xyz * ((float3)(_Intensity));
        u_xlat16_1.xyz = (-u_xlat16_1.xyz) * u_xlat16_3.xyz + float3(1.0, 1.0, 1.0);
        u_xlat16_3.xyz = u_xlat16_0.xyz + u_xlat16_0.xyz;
        u_xlat16_3.xyz = u_xlat2.xyz * u_xlat16_3.xyz;
        u_xlatb0.xyz = ((u_xlat16_0.xyzx) < (float4(0.5, 0.5, 0.5, 0.0))).xyz;
        u_xlat16_13 = u_xlat16_0.w * i.vs_COLOR0.w;
        {
            float3 hlslcc_movcTemp = u_xlat16_1;
            hlslcc_movcTemp.x = (u_xlatb0.x) ? u_xlat16_3.x : u_xlat16_1.x;
            hlslcc_movcTemp.y = (u_xlatb0.y) ? u_xlat16_3.y : u_xlat16_1.y;
            hlslcc_movcTemp.z = (u_xlatb0.z) ? u_xlat16_3.z : u_xlat16_1.z;
            u_xlat16_1 = hlslcc_movcTemp;
        }
        SV_Target0.xyz = u_xlat16_1.xyz * i.vs_COLOR0.xyz;
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat0.x = u_xlat0.x * u_xlat16_13;
        SV_Target0.w = u_xlat0.x;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 9
            struct appdata_v9 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v9 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD1 : TEXCOORD2;
        float4 vs_TEXCOORD3 : TEXCOORD3;
    };
    v2f_v9 vert_v9(appdata_v9 v) {
        v2f_v9 o = (v2f_v9)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        o.vs_COLOR0 = v.in_COLOR0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = _ScrollSpeed.xy * _Time.yy + u_xlat0.xy;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _GradientTex_ST.xy + _GradientTex_ST.zw;
        o.vs_TEXCOORD1.xy = float2(_GradScrollSpeed.x, _GradScrollSpeed.y) * _Time.yy + u_xlat0.xy;
        o.vs_TEXCOORD3 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v9(v2f_v9 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float3 u_xlat16_0;
        bool4 u_xlatb0;
        float4 u_xlat16_1;
        float3 u_xlat16_2;
        float2 u_xlat3;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD3.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD3.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat3.xy = i.vs_TEXCOORD0.xy * float2(0.5, 0.5);
        u_xlat3.xy = frac(u_xlat3.xy);
        u_xlat3.xy = u_xlat3.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
        u_xlat3.xy = -abs(u_xlat3.xy) + float2(1.0, 1.0);
        u_xlat16_1 = tex2D(_MainTex, (u_xlat3.xy).xy);
        u_xlat16_2.x = u_xlat16_1.w * i.vs_COLOR0.w;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.x;
        SV_Target0.w = u_xlat0.x;
        u_xlat0.xy = frac(i.vs_TEXCOORD1.xy);
        u_xlat16_0.xyz = tex2D(_GradientTex, (u_xlat0.xy).xy).xyz;
        u_xlat0.xyz = u_xlat16_0.xyz * ((float3)(_Intensity));
        u_xlat16_2.xyz = min(u_xlat0.xyz, u_xlat16_1.xyz);
        SV_Target0.xyz = u_xlat16_2.xyz * i.vs_COLOR0.xyz;
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