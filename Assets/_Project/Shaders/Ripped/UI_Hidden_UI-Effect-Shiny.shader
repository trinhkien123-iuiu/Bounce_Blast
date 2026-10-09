// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "UI/Hidden/UI-Effect-Shiny"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" { }
        _Color ("Tint", Color) = (1,1,1,1)
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
        Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Default"
            Tags { "CanUseSpriteAtlas"="true" "PreviewType"="Plane" }
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
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _ClipRect;
            float4 _Color;
            sampler2D _MainTex;
            float4 _TextureSampleAdd;
        
            #if defined(UNITY_UI_ALPHACLIP)
                #define VTAG v0
        #define VTAG_ID 0
            #else
                #define VTAG v1
        #define VTAG_ID 1
            #endif
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
        float2 in_TEXCOORD1 : TEXCOORD1;
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD3 : TEXCOORD2;
        float4 vs_TEXCOORD1 : TEXCOORD3;
        float4 vs_TEXCOORD2 : TEXCOORD4;
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
        float4 u_xlat0;
        bool u_xlatb0;
        float4 u_xlat1;
        bool4 u_xlatb1;
        float4 u_xlat2;
        bool4 u_xlatb3;
        float3 u_xlat4;
        bool u_xlatb4;
        float u_xlat8;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        u_xlat0 = v.in_COLOR0 * _Color;
        o.vs_COLOR0 = u_xlat0;
        o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy;
        o.vs_TEXCOORD1 = v.in_POSITION0;
        u_xlat0 = v.in_TEXCOORD1.xxyy * float4(64.0, 0.015625, 4096.0, 0.000244140625);
        u_xlat4.xz = floor(u_xlat0.yw);
        u_xlatb1 = ((u_xlat0.xxzz) >= ((-u_xlat0.xxzz)));
        u_xlat1.x = (u_xlatb1.x) ? ((float)(64.0)) : ((float)(-64.0));
        u_xlat1.y = (u_xlatb1.y) ? ((float)(0.015625)) : ((float)(-0.015625));
        u_xlat1.z = (u_xlatb1.z) ? ((float)(4096.0)) : ((float)(-4096.0));
        u_xlat1.w = (u_xlatb1.w) ? ((float)(0.000244140625)) : ((float)(-0.000244140625));
        u_xlat2.xyz = u_xlat4.xxz * float3(64.0, 0.015625, 4096.0);
        u_xlatb3 = ((u_xlat2.xxzz) >= ((-u_xlat2.xxzz)));
        u_xlat0.x = floor(u_xlat2.y);
        u_xlat2.x = (u_xlatb3.x) ? ((float)(64.0)) : ((float)(-64.0));
        u_xlat2.y = (u_xlatb3.y) ? ((float)(0.015625)) : ((float)(-0.015625));
        u_xlat2.z = (u_xlatb3.z) ? ((float)(4096.0)) : ((float)(-4096.0));
        u_xlat2.w = (u_xlatb3.w) ? ((float)(0.000244140625)) : ((float)(-0.000244140625));
        u_xlat4.xy = u_xlat4.xz * u_xlat2.yw;
        u_xlat4.xy = frac(u_xlat4.xy);
        u_xlat4.xy = u_xlat4.xy * u_xlat2.xz;
        u_xlat2.xy = u_xlat4.yx * float2(0.000244200259, 0.0158730168);
        o.vs_TEXCOORD3.y = u_xlat2.x;
        u_xlat4.xy = u_xlat1.yw * v.in_TEXCOORD1.xy;
        u_xlat4.xy = frac(u_xlat4.xy);
        u_xlat4.xy = u_xlat4.xy * u_xlat1.xz;
        u_xlat2.x = u_xlat4.x * 0.0158730168;
        o.vs_TEXCOORD3.x = u_xlat4.y * 0.000488400517 + -0.5;
        u_xlat4.xy = u_xlat0.xx * float2(64.0, 0.015625);
        u_xlatb4 = u_xlat4.x>=(-u_xlat4.x);
        u_xlat8 = floor(u_xlat4.y);
        u_xlat4.xz = (((bool)(u_xlatb4))) ? float2(64.0, 0.015625) : float2(-64.0, -0.015625);
        u_xlat0.x = u_xlat4.z * u_xlat0.x;
        u_xlat0.x = frac(u_xlat0.x);
        u_xlat0.x = u_xlat0.x * u_xlat4.x;
        u_xlat2.z = u_xlat0.x * 0.0158730168;
        u_xlat0.x = u_xlat8 * 64.0;
        u_xlatb0 = u_xlat0.x>=(-u_xlat0.x);
        u_xlat0.xy = (((bool)(u_xlatb0))) ? float2(64.0, 0.015625) : float2(-64.0, -0.015625);
        u_xlat4.x = u_xlat0.y * u_xlat8;
        u_xlat4.x = frac(u_xlat4.x);
        u_xlat0.x = u_xlat4.x * u_xlat0.x;
        u_xlat2.w = u_xlat0.x * 0.0158730168;
        o.vs_TEXCOORD2 = u_xlat2;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        bool4 u_xlatb0;
        float4 u_xlat1;
        float4 u_xlat16_1;
        float4 u_xlat16_2;
        float3 u_xlat16_3;
        float u_xlat16_14;
    
        u_xlatb0.xy = ((i.vs_TEXCOORD1.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb0.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD1.xxxy)).zw;
        u_xlat0.x = u_xlatb0.x ? ((float)(1.0)) : 0.0;
        u_xlat0.y = u_xlatb0.y ? ((float)(1.0)) : 0.0;
        u_xlat0.z = u_xlatb0.z ? ((float)(1.0)) : 0.0;
        u_xlat0.w = u_xlatb0.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat0.xy = u_xlat0.zw * u_xlat0.xy;
        u_xlat0.x = u_xlat0.y * u_xlat0.x;
        u_xlat16_1 = tex2D(_MainTex, (i.vs_TEXCOORD0.xy).xy);
        u_xlat1 = u_xlat16_1 + _TextureSampleAdd;
        u_xlat16_2 = u_xlat1 * i.vs_COLOR0;
        u_xlat16_3.x = u_xlat16_2.w * u_xlat0.x + -0.00100000005;
        u_xlat0.x = u_xlat0.x * u_xlat16_2.w;
        SV_Target0.w = u_xlat0.x;
        u_xlatb0.x = u_xlat16_3.x<0.0;
        if(u_xlatb0.x){discard;}
        u_xlat16_14 = (-i.vs_TEXCOORD3.x) + i.vs_TEXCOORD2.x;
        u_xlat16_14 = u_xlat16_14 / i.vs_TEXCOORD2.z;
        u_xlat16_14 = min(abs(u_xlat16_14), 1.0);
        u_xlat16_14 = (-u_xlat16_14) + 1.0;
        u_xlat16_3.x = i.vs_TEXCOORD2.y + i.vs_TEXCOORD2.y;
        u_xlat16_3.x = ((float)(1.0)) / u_xlat16_3.x;
        u_xlat16_14 = u_xlat16_14 * u_xlat16_3.x;
        u_xlat16_14 = clamp(u_xlat16_14, 0.0, 1.0);
        u_xlat16_3.x = u_xlat16_14 * -2.0 + 3.0;
        u_xlat16_14 = u_xlat16_14 * u_xlat16_14;
        u_xlat16_14 = u_xlat16_14 * u_xlat16_3.x;
        u_xlat16_14 = u_xlat16_14 * 0.5;
        u_xlat16_14 = u_xlat1.w * u_xlat16_14;
        u_xlat16_14 = u_xlat16_14 * i.vs_TEXCOORD2.w;
        u_xlat16_3.xyz = u_xlat16_2.xyz * float3(10.0, 10.0, 10.0) + float3(-1.0, -1.0, -1.0);
        u_xlat16_3.xyz = i.vs_TEXCOORD3.yyy * u_xlat16_3.xyz + float3(1.0, 1.0, 1.0);
        SV_Target0.xyz = ((float3)(u_xlat16_14)) * u_xlat16_3.xyz + u_xlat16_2.xyz;
        return SV_Target0;
    
        return SV_Target0;
    }
            #endif
            #if VTAG_ID == 1
            struct appdata_v1 {
        float4 in_POSITION0 : POSITION;
        float4 in_COLOR0 : COLOR;
        float2 in_TEXCOORD0 : TEXCOORD0;
        float2 in_TEXCOORD1 : TEXCOORD1;
    };
    struct v2f_v1 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
        float2 vs_TEXCOORD0 : TEXCOORD1;
        float2 vs_TEXCOORD3 : TEXCOORD2;
        float4 vs_TEXCOORD1 : TEXCOORD3;
        float4 vs_TEXCOORD2 : TEXCOORD4;
    };
    v2f_v1 vert_v1(appdata_v1 v) {
        v2f_v1 o = (v2f_v1)0;
        float4 u_xlat0;
        bool u_xlatb0;
        float4 u_xlat1;
        bool4 u_xlatb1;
        float4 u_xlat2;
        bool4 u_xlatb3;
        float3 u_xlat4;
        bool u_xlatb4;
        float u_xlat8;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat0 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        u_xlat1 = u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat1 = transpose(unity_MatrixVP)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_MatrixVP)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat0.wwww + u_xlat1;
        u_xlat0 = v.in_COLOR0 * _Color;
        o.vs_COLOR0 = u_xlat0;
        o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy;
        o.vs_TEXCOORD1 = v.in_POSITION0;
        u_xlat0 = v.in_TEXCOORD1.xxyy * float4(64.0, 0.015625, 4096.0, 0.000244140625);
        u_xlat4.xz = floor(u_xlat0.yw);
        u_xlatb1 = ((u_xlat0.xxzz) >= ((-u_xlat0.xxzz)));
        u_xlat1.x = (u_xlatb1.x) ? ((float)(64.0)) : ((float)(-64.0));
        u_xlat1.y = (u_xlatb1.y) ? ((float)(0.015625)) : ((float)(-0.015625));
        u_xlat1.z = (u_xlatb1.z) ? ((float)(4096.0)) : ((float)(-4096.0));
        u_xlat1.w = (u_xlatb1.w) ? ((float)(0.000244140625)) : ((float)(-0.000244140625));
        u_xlat2.xyz = u_xlat4.xxz * float3(64.0, 0.015625, 4096.0);
        u_xlatb3 = ((u_xlat2.xxzz) >= ((-u_xlat2.xxzz)));
        u_xlat0.x = floor(u_xlat2.y);
        u_xlat2.x = (u_xlatb3.x) ? ((float)(64.0)) : ((float)(-64.0));
        u_xlat2.y = (u_xlatb3.y) ? ((float)(0.015625)) : ((float)(-0.015625));
        u_xlat2.z = (u_xlatb3.z) ? ((float)(4096.0)) : ((float)(-4096.0));
        u_xlat2.w = (u_xlatb3.w) ? ((float)(0.000244140625)) : ((float)(-0.000244140625));
        u_xlat4.xy = u_xlat4.xz * u_xlat2.yw;
        u_xlat4.xy = frac(u_xlat4.xy);
        u_xlat4.xy = u_xlat4.xy * u_xlat2.xz;
        u_xlat2.xy = u_xlat4.yx * float2(0.000244200259, 0.0158730168);
        o.vs_TEXCOORD3.y = u_xlat2.x;
        u_xlat4.xy = u_xlat1.yw * v.in_TEXCOORD1.xy;
        u_xlat4.xy = frac(u_xlat4.xy);
        u_xlat4.xy = u_xlat4.xy * u_xlat1.xz;
        u_xlat2.x = u_xlat4.x * 0.0158730168;
        o.vs_TEXCOORD3.x = u_xlat4.y * 0.000488400517 + -0.5;
        u_xlat4.xy = u_xlat0.xx * float2(64.0, 0.015625);
        u_xlatb4 = u_xlat4.x>=(-u_xlat4.x);
        u_xlat8 = floor(u_xlat4.y);
        u_xlat4.xz = (((bool)(u_xlatb4))) ? float2(64.0, 0.015625) : float2(-64.0, -0.015625);
        u_xlat0.x = u_xlat4.z * u_xlat0.x;
        u_xlat0.x = frac(u_xlat0.x);
        u_xlat0.x = u_xlat0.x * u_xlat4.x;
        u_xlat2.z = u_xlat0.x * 0.0158730168;
        u_xlat0.x = u_xlat8 * 64.0;
        u_xlatb0 = u_xlat0.x>=(-u_xlat0.x);
        u_xlat0.xy = (((bool)(u_xlatb0))) ? float2(64.0, 0.015625) : float2(-64.0, -0.015625);
        u_xlat4.x = u_xlat0.y * u_xlat8;
        u_xlat4.x = frac(u_xlat4.x);
        u_xlat0.x = u_xlat4.x * u_xlat0.x;
        u_xlat2.w = u_xlat0.x * 0.0158730168;
        o.vs_TEXCOORD2 = u_xlat2;
        return o;
    
        return o;
    }
    float4 frag_v1(v2f_v1 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float u_xlat16_0;
        float4 u_xlat1;
        float4 u_xlat16_1;
        float4 u_xlat2;
        bool4 u_xlatb2;
        float3 u_xlat16_3;
    
        u_xlat16_0 = (-i.vs_TEXCOORD3.x) + i.vs_TEXCOORD2.x;
        u_xlat16_0 = u_xlat16_0 / i.vs_TEXCOORD2.z;
        u_xlat16_0 = min(abs(u_xlat16_0), 1.0);
        u_xlat16_0 = (-u_xlat16_0) + 1.0;
        u_xlat16_3.x = i.vs_TEXCOORD2.y + i.vs_TEXCOORD2.y;
        u_xlat16_3.x = ((float)(1.0)) / u_xlat16_3.x;
        u_xlat16_0 = u_xlat16_3.x * u_xlat16_0;
        u_xlat16_0 = clamp(u_xlat16_0, 0.0, 1.0);
        u_xlat16_3.x = u_xlat16_0 * -2.0 + 3.0;
        u_xlat16_0 = u_xlat16_0 * u_xlat16_0;
        u_xlat16_0 = u_xlat16_0 * u_xlat16_3.x;
        u_xlat16_0 = u_xlat16_0 * 0.5;
        u_xlat16_1 = tex2D(_MainTex, (i.vs_TEXCOORD0.xy).xy);
        u_xlat1 = u_xlat16_1 + _TextureSampleAdd;
        u_xlat16_0 = u_xlat16_0 * u_xlat1.w;
        u_xlat16_1 = u_xlat1 * i.vs_COLOR0;
        u_xlat16_0 = u_xlat16_0 * i.vs_TEXCOORD2.w;
        u_xlat16_3.xyz = u_xlat16_1.xyz * float3(10.0, 10.0, 10.0) + float3(-1.0, -1.0, -1.0);
        u_xlat16_3.xyz = i.vs_TEXCOORD3.yyy * u_xlat16_3.xyz + float3(1.0, 1.0, 1.0);
        SV_Target0.xyz = ((float3)(u_xlat16_0)) * u_xlat16_3.xyz + u_xlat16_1.xyz;
        u_xlatb2.xy = ((i.vs_TEXCOORD1.xyxx) >= (_ClipRect.xyxx)).xy;
        u_xlatb2.zw = ((_ClipRect.zzzw) >= (i.vs_TEXCOORD1.xxxy)).zw;
        u_xlat2.x = u_xlatb2.x ? ((float)(1.0)) : 0.0;
        u_xlat2.y = u_xlatb2.y ? ((float)(1.0)) : 0.0;
        u_xlat2.z = u_xlatb2.z ? ((float)(1.0)) : 0.0;
        u_xlat2.w = u_xlatb2.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat2.xy = u_xlat2.zw * u_xlat2.xy;
        u_xlat2.x = u_xlat2.y * u_xlat2.x;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        SV_Target0.w = u_xlat2.x;
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