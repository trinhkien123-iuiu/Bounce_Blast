// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "UI/FeatheredFill"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" { }
        _Color ("Tint", Color) = (1,1,1,1)
        _Fill ("Fill (0-1)", Range(0, 1)) = 1.0
        _Feather ("Feather (UV width)", Range(0, 0.2)) = 0.019999999552965164
        [Enum(Horizontal,0,Vertical,1)] _Direction ("Direction", Float) = 0.0
        [Enum(LeftToRight,0,RightToLeft,1,BottomToTop,2,TopToBottom,3)] _Mode ("Mode", Float) = 0.0
    }
    SubShader
    {
        Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "CanUseSpriteAtlas"="true" "PreviewType"="Plane" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _Color;
            float _Direction;
            float _Feather;
            float _Fill;
            sampler2D _MainTex;
            float _Mode;
        
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
        float2 u_xlat0;
        float4 u_xlat1;
        float4 u_xlat16_1;
        bool3 u_xlatb1;
        float u_xlat2;
        float2 u_xlat4;
        bool u_xlatb4;
    
        u_xlat4.x = _Fill;
        u_xlat0.x = (-_Fill) + 1.0;
        u_xlatb1.xyz = ((((float4)(float4(_Mode, _Mode, _Mode, _Mode)))) == (float4(1.0, 2.0, 3.0, 0.0))).xyz;
        u_xlat0.y = ((float)(-1.0));
        u_xlat4.y = ((float)(1.0));
        u_xlat1.xw = (u_xlatb1.x) ? u_xlat0.xy : u_xlat4.xy;
        u_xlat4.xy = (u_xlatb1.y) ? u_xlat4.xy : u_xlat1.xw;
        u_xlat0.xy = (u_xlatb1.z) ? u_xlat0.xy : u_xlat4.xy;
        u_xlatb4 = _Direction<0.5;
        u_xlat4.x = (u_xlatb4) ? i.vs_TEXCOORD0.x : i.vs_TEXCOORD0.y;
        u_xlat0.x = (-u_xlat4.x) + u_xlat0.x;
        u_xlat0.x = u_xlat0.x * u_xlat0.y + _Feather;
        u_xlat2 = ((float)(1.0)) / _Feather;
        u_xlat0.x = u_xlat2 * u_xlat0.x;
        u_xlat0.x = clamp(u_xlat0.x, 0.0, 1.0);
        u_xlat2 = u_xlat0.x * -2.0 + 3.0;
        u_xlat0.x = u_xlat0.x * u_xlat0.x;
        u_xlat0.x = u_xlat0.x * u_xlat2;
        u_xlat16_1 = tex2D(_MainTex, (i.vs_TEXCOORD0.xy).xy);
        u_xlat1 = u_xlat16_1 * i.vs_COLOR0;
        u_xlat1.w = u_xlat0.x * u_xlat1.w;
        SV_Target0 = u_xlat1;
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