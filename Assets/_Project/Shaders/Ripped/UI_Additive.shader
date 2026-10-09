// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "UI/Additive"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" { }
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8.0
        _Stencil ("Stencil ID", Float) = 0.0
        _StencilOp ("Stencil Operation", Float) = 0.0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255.0
        _StencilReadMask ("Stencil Read Mask", Float) = 255.0
        _ColorMask ("Color Mask", Float) = 15.0
        _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
    }
    SubShader
    {
        Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "CanUseSpriteAtlas"="true" "PreviewType"="Plane" }
            Cull Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend One One
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
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _ClipRect;
            float4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
        
                #define VTAG v0
        #define VTAG_ID 0
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
        float4 vs_TEXCOORD1 : TEXCOORD2;
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
        u_xlat0 = v.in_COLOR0 * _Color;
        o.vs_COLOR0 = u_xlat0;
        u_xlat0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        o.vs_TEXCOORD0.xy = u_xlat0.xy;
        o.vs_TEXCOORD1 = v.in_POSITION0;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        bool4 u_xlatb0;
        float4 u_xlat1;
        float4 u_xlat16_1;
    
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
        u_xlat1 = u_xlat16_1 * i.vs_COLOR0;
        u_xlat0.x = u_xlat0.x * u_xlat1.w;
        SV_Target0.xyz = u_xlat0.xxx * u_xlat1.xyz;
        SV_Target0.w = u_xlat0.x;
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