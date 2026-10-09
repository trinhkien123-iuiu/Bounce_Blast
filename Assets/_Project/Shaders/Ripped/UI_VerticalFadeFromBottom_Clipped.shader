// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "UI/VerticalFadeFromBottom_Clipped"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" { }
        _Color ("Tint", Color) = (1,1,1,1)
        _Fade ("Fade", Range(0, 1)) = 0.0
        _FadeSoftness ("Fade Softness", Range(0.001, 0.2)) = 0.019999999552965164
        _SpriteUVMinY ("Sprite UV Min Y", Float) = 0.0
        _SpriteUVMaxY ("Sprite UV Max Y", Float) = 1.0
        _StencilComp ("Stencil Comparison", Float) = 8.0
        _Stencil ("Stencil ID", Float) = 0.0
        _StencilOp ("Stencil Operation", Float) = 0.0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255.0
        _StencilReadMask ("Stencil Read Mask", Float) = 255.0
        _ColorMask ("Color Mask", Float) = 15.0
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
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _Color;
            float _Fade;
            float _FadeSoftness;
            sampler2D _MainTex;
            float _SpriteUVMaxY;
            float _SpriteUVMinY;
        
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
        o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float u_xlat16_0;
        float u_xlat1;
        float4 u_xlat16_1;
        bool u_xlatb1;
        float u_xlat16_2;
        float u_xlat3;
        float u_xlat16_4;
    
        u_xlat16_0 = (-_SpriteUVMinY) + _SpriteUVMaxY;
        u_xlat1 = max(u_xlat16_0, 9.99999975e-05);
        u_xlat3 = i.vs_TEXCOORD0.y + (-_SpriteUVMinY);
        u_xlat1 = u_xlat3 / u_xlat1;
        u_xlat1 = clamp(u_xlat1, 0.0, 1.0);
        u_xlat16_0 = u_xlat1 + (-_Fade);
        u_xlat1 = max(_FadeSoftness, 9.99999975e-05);
        u_xlat16_2 = ((float)(1.0)) / u_xlat1;
        u_xlat16_0 = u_xlat16_2 * u_xlat16_0;
        u_xlat16_0 = clamp(u_xlat16_0, 0.0, 1.0);
        u_xlat16_2 = u_xlat16_0 * -2.0 + 3.0;
        u_xlat16_0 = u_xlat16_0 * u_xlat16_0;
        u_xlat16_4 = u_xlat16_2 * u_xlat16_0 + -0.00100000005;
        u_xlat16_0 = u_xlat16_0 * u_xlat16_2;
        u_xlatb1 = u_xlat16_4<0.0;
        if(u_xlatb1){discard;}
        u_xlat16_1 = tex2D(_MainTex, (i.vs_TEXCOORD0.xy).xy);
        u_xlat16_1 = u_xlat16_1 * i.vs_COLOR0;
        SV_Target0.w = u_xlat16_0 * u_xlat16_1.w;
        SV_Target0.xyz = u_xlat16_1.xyz;
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
    Fallback "UI/Default"
}