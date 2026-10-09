// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "ELCAPITAN/IceBlockerWorld"
{
    Properties
    {
        [Header(Base Ice)] _IceBaseColor ("Color", Color) = (0.04,0.72,1,0.99)
        [Header(Global Stone Texture)] _GlobalStoneTex ("Global Stone Texture", 2D) = "white" { }
        _GlobalStoneStrength ("Stone Texture Strength", Range(0, 1)) = 1.0
        [Header(Rim Light)] _RimStrength ("Rim Strength", Range(0, 2)) = 1.0
        _RimPower ("Rim Power", Range(0.25, 8)) = 2.0
        [Header(Rendering)] _ZWrite ("Depth Writing", Float) = 1.0
        [Enum(Front,2,Back,1,Both,0)] _Cull ("Render Face", Float) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="ForwardBase" }
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float _GlobalStoneStrength;
            sampler2D _GlobalStoneTex;
            float4 _GlobalStoneTex_ST;
            float4 _IceBaseColor;
            float _RimPower;
            float _RimStrength;
        
                #define VTAG v0
        #define VTAG_ID 0
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float3 vs_TEXCOORD0 : TEXCOORD0;
        float3 vs_TEXCOORD1 : TEXCOORD1;
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
        float4 u_xlat0;
        float4 u_xlat1;
        float u_xlat6;
    
        u_xlat0 = v.in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
        u_xlat0 = transpose(unity_ObjectToWorld)[0] * v.in_POSITION0.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_ObjectToWorld)[2] * v.in_POSITION0.zzzz + u_xlat0;
        u_xlat1 = u_xlat0 + transpose(unity_ObjectToWorld)[3];
        o.vs_TEXCOORD0.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
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
        o.vs_TEXCOORD1.xyz = u_xlat0.xyz;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float4 u_xlat0;
        float4 u_xlat16_1;
        float4 u_xlat2;
        float3 u_xlat16_2;
        float3 u_xlat3;
        float3 u_xlat16_3;
        float3 u_xlat16_4;
        float3 u_xlat16_5;
        float u_xlat6;
        float u_xlat16_13;
        float u_xlat18;
        float u_xlat16_19;
    
        u_xlat0.x = transpose(unity_MatrixV)[0].x;
        u_xlat0.y = transpose(unity_MatrixV)[1].x;
        u_xlat0.z = transpose(unity_MatrixV)[2].x;
        u_xlat16_1.x = dot(i.vs_TEXCOORD1.xyz, i.vs_TEXCOORD1.xyz);
        u_xlat16_1.x = rsqrt(u_xlat16_1.x);
        u_xlat16_1.xyz = u_xlat16_1.xxx * i.vs_TEXCOORD1.xyz;
        u_xlat16_2.x = dot(u_xlat0.xyz, u_xlat16_1.xyz);
        u_xlat0.x = transpose(unity_MatrixV)[0].y;
        u_xlat0.y = transpose(unity_MatrixV)[1].y;
        u_xlat0.z = transpose(unity_MatrixV)[2].y;
        u_xlat16_2.y = dot(u_xlat0.xyz, u_xlat16_1.xyz);
        u_xlat0.x = transpose(unity_MatrixV)[0].z;
        u_xlat0.y = transpose(unity_MatrixV)[1].z;
        u_xlat0.z = transpose(unity_MatrixV)[2].z;
        u_xlat16_2.z = dot(u_xlat0.xyz, u_xlat16_1.xyz);
        u_xlat16_1.xyz = abs(u_xlat16_1.xyz) * abs(u_xlat16_1.xyz);
        u_xlat16_19 = dot(u_xlat16_2.xyz, u_xlat16_2.xyz);
        u_xlat16_19 = rsqrt(u_xlat16_19);
        u_xlat16_19 = u_xlat16_19 * u_xlat16_2.z;
        u_xlat16_19 = clamp(u_xlat16_19, 0.0, 1.0);
        u_xlat0.x = log2(u_xlat16_19);
        u_xlat6 = max(_RimPower, 0.00100000005);
        u_xlat0.x = u_xlat0.x * u_xlat6;
        u_xlat0.x = exp2(u_xlat0.x);
        u_xlat0.x = u_xlat0.x * _RimStrength;
        u_xlat0.xyz = u_xlat0.xxx * _IceBaseColor.xyz;
        u_xlat16_1.xyw = u_xlat16_1.xyz * u_xlat16_1.xyz;
        u_xlat16_2.x = u_xlat16_1.y + u_xlat16_1.x;
        u_xlat16_13 = u_xlat16_1.z * u_xlat16_1.z + u_xlat16_2.x;
        u_xlat16_13 = max(u_xlat16_13, 9.99999975e-05);
        u_xlat16_1.xyz = u_xlat16_1.xyw / ((float3)(u_xlat16_13));
        u_xlat2 = i.vs_TEXCOORD0.zyxz * _GlobalStoneTex_ST.xyxy + _GlobalStoneTex_ST.zwzw;
        u_xlat16_3.xyz = tex2D(_GlobalStoneTex, (u_xlat2.zw).xy).xyz;
        u_xlat16_4.xyz = tex2D(_GlobalStoneTex, (u_xlat2.xy).xy).xyz;
        u_xlat16_5.xyz = u_xlat16_1.yyy * u_xlat16_3.xyz;
        u_xlat16_1.xyw = u_xlat16_4.xyz * u_xlat16_1.xxx + u_xlat16_5.xyz;
        u_xlat3.xy = i.vs_TEXCOORD0.xy * _GlobalStoneTex_ST.xy + _GlobalStoneTex_ST.zw;
        u_xlat16_3.xyz = tex2D(_GlobalStoneTex, (u_xlat3.xy).xy).xyz;
        u_xlat16_1.xyz = u_xlat16_3.xyz * u_xlat16_1.zzz + u_xlat16_1.xyw;
        u_xlat3.xyz = u_xlat16_1.xyz + float3(-1.0, -1.0, -1.0);
        u_xlat18 = _GlobalStoneStrength;
        u_xlat18 = clamp(u_xlat18, 0.0, 1.0);
        u_xlat3.xyz = ((float3)(u_xlat18)) * u_xlat3.xyz + float3(1.0, 1.0, 1.0);
        u_xlat0.xyz = _IceBaseColor.xyz * u_xlat3.xyz + u_xlat0.xyz;
        u_xlat0.xyz = clamp(u_xlat0.xyz, 0.0, 1.0);
        u_xlat0.w = _IceBaseColor.w;
        SV_Target0 = u_xlat0;
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