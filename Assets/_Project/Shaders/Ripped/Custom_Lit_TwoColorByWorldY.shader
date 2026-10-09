// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "Custom/Lit/TwoColorByWorldY"
{
    Properties
    {
        _ColorAbove ("Above Color", Color) = (1,1,1,1)
        _ColorBelow ("Below Color", Color) = (0,0,0,1)
        _ShadowColor ("Shadow Color", Color) = (0.3,0.3,0.3,1)
        _SplitY ("World-space Y split", Float) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" "ShadowSupport"="true" }
            Cull Back
            ZWrite On
            ZTest LEqual
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _ColorAbove;
            float4 _ColorBelow;
            float _SplitY;
        
                #define VTAG v0
        #define VTAG_ID 0
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float3 vs_TEXCOORD0 : TEXCOORD0;
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
        float4 u_xlat0;
        float4 u_xlat1;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat1 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        o.vs_TEXCOORD0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat0 = u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat0 = transpose(unity_MatrixVP)[0] * u_xlat1.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_MatrixVP)[2] * u_xlat1.zzzz + u_xlat0;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat1.wwww + u_xlat0;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        bool u_xlatb0;
    
        u_xlatb0 = _SplitY<i.vs_TEXCOORD0.y;
        SV_Target0 = (((bool)(u_xlatb0))) ? _ColorAbove : _ColorBelow;
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
    Fallback "Diffuse"
}