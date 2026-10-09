// Upgrade NOTE: replaced 'glstate_matrix_projection' with 'UNITY_MATRIX_P'

// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "Custom/Outline Fill"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 0.0
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2.0
    }
    SubShader
    {
        Tags { "DisableBatching"="true" "Queue"="Transparent+110" "RenderType"="Transparent" }
        Pass
        {
            Name "Fill"
            Tags { "DisableBatching"="true" }
            Cull Off
            ZWrite Off
            ZTest [_ZTest]
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            Stencil {
                Ref 1
                ReadMask 255
                WriteMask 255
                Comp NotEqual
                Pass Keep
                Fail Keep
                ZFail Keep
            }
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _OutlineColor;
            float _OutlineWidth;
        
                #define VTAG v0
        #define VTAG_ID 0
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float3 in_TEXCOORD3 : TEXCOORD3;
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float4 vs_COLOR0 : TEXCOORD0;
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
        float4 u_xlat0;
        float4 u_xlat1;
        float3 u_xlat2;
        float u_xlat9;
        bool u_xlatb9;
    
        u_xlat0.xyz = transpose(unity_WorldToObject)[1].xyz * transpose(unity_MatrixInvV)[0].yyy;
        u_xlat0.xyz = transpose(unity_WorldToObject)[0].xyz * transpose(unity_MatrixInvV)[0].xxx + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_WorldToObject)[2].xyz * transpose(unity_MatrixInvV)[0].zzz + u_xlat0.xyz;
        u_xlat0.xyz = transpose(unity_WorldToObject)[3].xyz * transpose(unity_MatrixInvV)[0].www + u_xlat0.xyz;
        u_xlat9 = dot(v.in_TEXCOORD3.xyz, v.in_TEXCOORD3.xyz);
        u_xlatb9 = u_xlat9!=0.0;
        u_xlat1.xyz = (((bool)(u_xlatb9))) ? v.in_TEXCOORD3.xyz : v.in_NORMAL0.xyz;
        u_xlat0.x = dot(u_xlat0.xyz, u_xlat1.xyz);
        u_xlat2.xyz = transpose(unity_WorldToObject)[1].xyz * transpose(unity_MatrixInvV)[1].yyy;
        u_xlat2.xyz = transpose(unity_WorldToObject)[0].xyz * transpose(unity_MatrixInvV)[1].xxx + u_xlat2.xyz;
        u_xlat2.xyz = transpose(unity_WorldToObject)[2].xyz * transpose(unity_MatrixInvV)[1].zzz + u_xlat2.xyz;
        u_xlat2.xyz = transpose(unity_WorldToObject)[3].xyz * transpose(unity_MatrixInvV)[1].www + u_xlat2.xyz;
        u_xlat0.y = dot(u_xlat2.xyz, u_xlat1.xyz);
        u_xlat2.xyz = transpose(unity_WorldToObject)[1].xyz * transpose(unity_MatrixInvV)[2].yyy;
        u_xlat2.xyz = transpose(unity_WorldToObject)[0].xyz * transpose(unity_MatrixInvV)[2].xxx + u_xlat2.xyz;
        u_xlat2.xyz = transpose(unity_WorldToObject)[2].xyz * transpose(unity_MatrixInvV)[2].zzz + u_xlat2.xyz;
        u_xlat2.xyz = transpose(unity_WorldToObject)[3].xyz * transpose(unity_MatrixInvV)[2].www + u_xlat2.xyz;
        u_xlat0.z = dot(u_xlat2.xyz, u_xlat1.xyz);
        u_xlat9 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat9 = rsqrt(u_xlat9);
        u_xlat0.xyz = ((float3)(u_xlat9)) * u_xlat0.xyz;
        u_xlat1 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat1 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat1;
        u_xlat1 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat1;
        u_xlat1 = u_xlat1 + transpose(unity_ObjectToWorld)[3];
        u_xlat2.xyz = u_xlat1.yyy * transpose(unity_MatrixV)[1].xyz;
        u_xlat2.xyz = transpose(unity_MatrixV)[0].xyz * u_xlat1.xxx + u_xlat2.xyz;
        u_xlat1.xyz = transpose(unity_MatrixV)[2].xyz * u_xlat1.zzz + u_xlat2.xyz;
        u_xlat1.xyz = transpose(unity_MatrixV)[3].xyz * u_xlat1.www + u_xlat1.xyz;
        u_xlat0.xyz = u_xlat0.xyz * (-u_xlat1.zzz);
        u_xlat0.xyz = u_xlat0.xyz * ((float3)(_OutlineWidth));
        u_xlat0.xyz = u_xlat0.xyz * float3(0.00100000005, 0.00100000005, 0.00100000005) + u_xlat1.xyz;
        u_xlat1 = u_xlat0.yyyy * transpose(UNITY_MATRIX_P)[1];
        u_xlat1 = transpose(UNITY_MATRIX_P)[0] * u_xlat0.xxxx + u_xlat1;
        u_xlat0 = transpose(UNITY_MATRIX_P)[2] * u_xlat0.zzzz + u_xlat1;
        o.pos = u_xlat0 + transpose(UNITY_MATRIX_P)[3];
        o.vs_COLOR0 = _OutlineColor;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
    
        SV_Target0 = i.vs_COLOR0;
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