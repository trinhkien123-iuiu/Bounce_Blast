// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "BounceBlast/CubeIsolatedOptimized"
{
    Properties
    {
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull ("Render Face", Float) = 2.0
        _BaseColor ("Color", Color) = (1,1,1,1)
        _HColor ("Highlight Color", Color) = (1,1,1,1)
        _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
        _RampThreshold ("Ramp Threshold", Range(0.01, 1)) = 0.6800000071525574
        _RampSmoothing ("Ramp Smoothing", Range(0, 1)) = 0.25
        _IndirectIntensity ("Indirect Strength", Range(0, 1)) = 0.5979999899864197
        _SingleIndirectColor ("Single Indirect Color", Float) = 0.0
        _SpecularRoughness ("Specular Roughness", Range(0.001, 1)) = 0.4000000059604645
        _SpecularColor ("Specular Color", Color) = (0.886275,0.886275,0.886275,1)
        _CubeShadeOffset ("Cube Shade Offset", Range(-0.25, 0.25)) = 0.0
        _UseBakedCubeShadeOffset ("Use Baked Cube Shade Offset", Float) = 0.0
    }
    SubShader
    {
        Tags { "BBOutlineTarget"="CubeFamily" "IgnoreProjector"="true" "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Name "Main"
            Tags { "BBOutlineTarget"="CubeFamily" "LightMode"="ForwardBase" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _BaseColor;
            float4 _HColor;
            float _IndirectIntensity;
            float _RampSmoothing;
            float _RampThreshold;
            float4 _SColor;
            float _SingleIndirectColor;
            float4 _SpecularColor;
            float _SpecularRoughness;
            UNITY_INSTANCING_BUFFER_START(CubeProps)
                UNITY_DEFINE_INSTANCED_PROP(float, _CubeShadeOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseBakedCubeShadeOffset)
            UNITY_INSTANCING_BUFFER_END(CubeProps)
        
                #define VTAG v0
        #define VTAG_ID 0
            // ---- v0: s0p0_FORWARDBASE_Vertex_GLES3_2.glsl  |  keyword material: (không keyword)
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float4 in_COLOR0 : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float3 vs_TEXCOORD0 : TEXCOORD0;
        float vs_TEXCOORD3 : TEXCOORD1;
        float3 vs_TEXCOORD1 : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float4 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat1 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        o.vs_TEXCOORD1.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat0 = u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat0 = transpose(unity_MatrixVP)[0] * u_xlat1.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_MatrixVP)[2] * u_xlat1.zzzz + u_xlat0;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat1.wwww + u_xlat0;
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        u_xlat0.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        o.vs_TEXCOORD3 = v.in_COLOR0.w;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        UNITY_SETUP_INSTANCE_ID(i);
        float3 u_xlat0;
        float4 u_xlat16_0;
        bool u_xlatb0;
        float3 u_xlat16_1;
        float3 u_xlat16_2;
        float4 u_xlat16_3;
        float3 u_xlat16_4;
        float3 u_xlat5;
        bool u_xlatb5;
        float u_xlat16_7;
        float3 u_xlat11;
        bool u_xlatb11;
        float u_xlat16_13;
        float u_xlat17;
        bool u_xlatb17;
        float u_xlat16_19;
        float u_xlat16_20;
        float u_xlat16_21;
    
        u_xlat0.xyz = (-i.vs_TEXCOORD1.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat16_1.x = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat16_1.x = max(u_xlat16_1.x, 0.00100000005);
        u_xlat16_1.x = rsqrt(u_xlat16_1.x);
        u_xlat16_1.xyz = u_xlat0.xyz * u_xlat16_1.xxx;
        u_xlatb0 = 0.5<_SingleIndirectColor;
        u_xlat16_19 = dot(i.vs_TEXCOORD0.xyz, i.vs_TEXCOORD0.xyz);
        u_xlat16_19 = rsqrt(u_xlat16_19);
        u_xlat16_2.xyz = ((float3)(u_xlat16_19)) * i.vs_TEXCOORD0.xyz;
        u_xlat16_0.xyz = (((bool)(u_xlatb0))) ? u_xlat16_1.xyz : u_xlat16_2.xyz;
        u_xlat16_19 = u_xlat16_0.y * u_xlat16_0.y;
        u_xlat16_19 = u_xlat16_0.x * u_xlat16_0.x + (-u_xlat16_19);
        u_xlat16_3 = u_xlat16_0.yzzx * u_xlat16_0.xyzz;
        u_xlat16_4.x = dot(unity_SHBr, u_xlat16_3);
        u_xlat16_4.y = dot(unity_SHBg, u_xlat16_3);
        u_xlat16_4.z = dot(unity_SHBb, u_xlat16_3);
        u_xlat16_3.xyz = unity_SHC.xyz * ((float3)(u_xlat16_19)) + u_xlat16_4.xyz;
        u_xlat16_0.w = 1.0;
        u_xlat16_4.x = dot(unity_SHAr, u_xlat16_0);
        u_xlat16_4.y = dot(unity_SHAg, u_xlat16_0);
        u_xlat16_4.z = dot(unity_SHAb, u_xlat16_0);
        u_xlat16_3.xyz = u_xlat16_3.xyz + u_xlat16_4.xyz;
        u_xlat16_3.xyz = max(u_xlat16_3.xyz, float3(0.0, 0.0, 0.0));
        u_xlat5.xyz = log2(u_xlat16_3.xyz);
        u_xlat5.xyz = u_xlat5.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat5.xyz = exp2(u_xlat5.xyz);
        u_xlat5.xyz = u_xlat5.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat5.xyz = max(u_xlat5.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_3.xyz = u_xlat5.xyz * _BaseColor.xyz;
        u_xlat16_3.xyz = u_xlat16_3.xyz * ((float3)(float3(_IndirectIntensity, _IndirectIntensity, _IndirectIntensity)));
        u_xlat5.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat5.x = rsqrt(u_xlat5.x);
        u_xlat11.xyz = u_xlat5.xxx * _WorldSpaceLightPos0.xyz;
        u_xlat16_1.xyz = _WorldSpaceLightPos0.xyz * u_xlat5.xxx + u_xlat16_1.xyz;
        u_xlat16_19 = dot(u_xlat16_2.xyz, u_xlat11.xyz);
        u_xlat5.x = u_xlat16_19 * 0.5 + 0.5;
        u_xlat16_19 = u_xlat16_19;
        u_xlat16_19 = clamp(u_xlat16_19, 0.0, 1.0);
        u_xlat16_20 = (-_RampSmoothing) * 0.5 + _RampThreshold;
        u_xlat16_21 = (-u_xlat16_20) + u_xlat5.x;
        u_xlat16_4.x = _RampSmoothing * 0.5 + _RampThreshold;
        u_xlat16_20 = (-u_xlat16_20) + u_xlat16_4.x;
        u_xlat16_20 = ((float)(1.0)) / u_xlat16_20;
        u_xlat16_20 = u_xlat16_20 * u_xlat16_21;
        u_xlat16_20 = clamp(u_xlat16_20, 0.0, 1.0);
        u_xlat16_21 = u_xlat16_20 * -2.0 + 3.0;
        u_xlat16_20 = u_xlat16_20 * u_xlat16_20;
        u_xlat16_20 = u_xlat16_20 * u_xlat16_21;
        u_xlat16_4.xyz = _HColor.xyz + (-_SColor.xyz);
        u_xlat16_4.xyz = ((float3)(u_xlat16_20)) * u_xlat16_4.xyz + _SColor.xyz;
        u_xlat16_4.xyz = u_xlat16_4.xyz * _LightColor0.xyz;
        u_xlat16_3.xyz = _BaseColor.xyz * u_xlat16_4.xyz + u_xlat16_3.xyz;
        u_xlat16_20 = dot(u_xlat16_1.xyz, u_xlat16_1.xyz);
        u_xlat16_20 = max(u_xlat16_20, 0.00100000005);
        u_xlat16_20 = rsqrt(u_xlat16_20);
        u_xlat16_1.xyz = u_xlat16_1.xyz * ((float3)(u_xlat16_20));
        u_xlat16_1.x = dot(u_xlat16_2.xyz, u_xlat16_1.xyz);
        u_xlat16_1.x = clamp(u_xlat16_1.x, 0.0, 1.0);
        u_xlat5.x = max(_SpecularRoughness, 9.99999975e-06);
        u_xlat16_7 = u_xlat5.x * u_xlat5.x;
        u_xlat16_13 = min(u_xlat16_7, 1.0);
        u_xlat11.x = u_xlat16_7 * 0.280000001;
        u_xlat5.x = (-u_xlat11.x) * u_xlat5.x + 1.0;
        u_xlat16_7 = u_xlat16_13 * u_xlat16_13;
        u_xlat16_13 = u_xlat16_1.x * u_xlat16_7 + (-u_xlat16_1.x);
        u_xlat11.x = u_xlat16_13 * u_xlat16_1.x + 1.0;
        u_xlat11.x = u_xlat11.x * u_xlat11.x + 9.99999975e-05;
        u_xlat17 = u_xlat16_7 * 0.318309873;
        u_xlat11.x = u_xlat17 / u_xlat11.x;
        u_xlat11.x = u_xlat11.x * 0.157079637;
        u_xlat11.x = max(u_xlat11.x, 9.99999975e-05);
        u_xlat11.x = sqrt(u_xlat11.x);
        u_xlat16_1.x = u_xlat5.x * u_xlat11.x;
        u_xlat16_1.x = u_xlat16_19 * u_xlat16_1.x;
        u_xlat16_1.xyz = u_xlat16_1.xxx * _LightColor0.xyz;
        u_xlat16_1.xyz = u_xlat16_1.xyz * _SpecularColor.xyz + u_xlat16_3.xyz;
        u_xlat5.x = i.vs_TEXCOORD3 * 255.0 + 0.5;
        u_xlat5.x = floor(u_xlat5.x);
        u_xlatb11 = 0.0>=u_xlat5.x;
        u_xlatb17 = u_xlat5.x>=255.0;
        u_xlat5.x = u_xlat5.x + -1.0;
        u_xlat5.x = u_xlat5.x * 0.00197628466 + -0.25;
        u_xlatb11 = u_xlatb17 || u_xlatb11;
        u_xlat16_19 = (u_xlatb11) ? 0.0 : u_xlat5.x;
        u_xlatb5 = 0.5<UNITY_ACCESS_INSTANCED_PROP(CubeProps, _UseBakedCubeShadeOffset);
        u_xlat5.x = (u_xlatb5) ? u_xlat16_19 : 0.0;
        u_xlat16_19 = u_xlat5.x + UNITY_ACCESS_INSTANCED_PROP(CubeProps, _CubeShadeOffset);
        u_xlat5.x = max(u_xlat16_19, -0.25);
        u_xlat5.x = min(u_xlat5.x, 0.25);
        u_xlatb11 = u_xlat5.x<0.0;
        u_xlat16_19 = (u_xlatb11) ? 1.0 : 0.0;
        u_xlat16_2.xyz = (-u_xlat16_1.xyz) + ((float3)(u_xlat16_19));
        SV_Target0.xyz = abs(u_xlat5.xxx) * u_xlat16_2.xyz + u_xlat16_1.xyz;
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