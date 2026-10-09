// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "URP/QMark"
{
    Properties
    {
        _MainTex ("Question Mark Texture", 2D) = "white" { }
        _Color ("Background Color", Color) = (0.5,0.5,0.5,1)
        _QuestionColor ("Question Mark Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.3,0.3,0.3,1)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Speed ("Animation Speed", Range(0.1, 5)) = 1.0
        _Density ("Question Mark Density", Range(1, 20)) = 8.0
        _RotationVariance ("Rotation Variance", Range(0, 360)) = 180.0
        _Scale ("Question Mark Scale", Range(0.1, 1)) = 0.6000000238418579
        _Direction ("Movement Direction", Vector) = (1,0,0,0)
        _Spacing ("Question Mark Spacing", Range(0.1, 0.9)) = 0.5
        _VerticalDissolve ("Vertical Dissolve", Range(0, 1)) = 0.0
        _DissolveYMin ("Dissolve Min Y (Object Space)", Float) = 0.0
        _DissolveYMax ("Dissolve Max Y (Object Space)", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On
            ZTest LEqual
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _Color;
            float _Density;
            float4 _Direction;
            float _DissolveYMax;
            float _DissolveYMin;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _QuestionColor;
            float _RotationVariance;
            float _Scale;
            float4 _ShadowColor;
            float _Spacing;
            float _Speed;
            float _VerticalDissolve;
        
                #define VTAG v0
        #define VTAG_ID 0
            #if VTAG_ID == 0
            struct appdata_v0 {
        float4 in_POSITION0 : POSITION;
        float3 in_NORMAL0 : NORMAL;
        float2 in_TEXCOORD0 : TEXCOORD0;
    };
    struct v2f_v0 {
        float4 pos : SV_POSITION;
        float2 vs_TEXCOORD0 : TEXCOORD0;
        float3 vs_TEXCOORD1 : TEXCOORD1;
        float3 vs_TEXCOORD2 : TEXCOORD2;
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
        o.vs_TEXCOORD2.xyz = transpose(unity_ObjectToWorld)[3].xyz * v.in_POSITION0.www + u_xlat0.xyz;
        u_xlat0 = u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
        u_xlat0 = transpose(unity_MatrixVP)[0] * u_xlat1.xxxx + u_xlat0;
        u_xlat0 = transpose(unity_MatrixVP)[2] * u_xlat1.zzzz + u_xlat0;
        o.pos = transpose(unity_MatrixVP)[3] * u_xlat1.wwww + u_xlat0;
        o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
        u_xlat0.x = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[0].xyz);
        u_xlat0.y = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[1].xyz);
        u_xlat0.z = dot(v.in_NORMAL0.xyz, transpose(unity_WorldToObject)[2].xyz);
        u_xlat6 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat6 = rsqrt(u_xlat6);
        o.vs_TEXCOORD1.xyz = ((float3)(u_xlat6)) * u_xlat0.xyz;
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float3 u_xlat0;
        float u_xlat1;
        float3 u_xlat2;
        float4 u_xlat3;
        float3 u_xlat4;
        float2 u_xlat5;
        float4 u_xlat16_5;
        bool2 u_xlatb5;
        float2 u_xlat6;
        float3 u_xlat7;
        float3 u_xlat8;
        float u_xlat9;
        float2 u_xlat16;
        int u_xlati17;
        float2 u_xlat19;
        bool2 u_xlatb21;
        float u_xlat24;
        bool u_xlatb24;
        int u_xlati25;
        bool u_xlatb25;
        float u_xlat26;
        int u_xlati26;
        bool u_xlatb26;
        bool u_xlatb28;
    
        u_xlat0.x = dot(_Direction.xy, _Direction.xy);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xy = u_xlat0.xx * _Direction.xy;
        u_xlat16.x = _Time.y * _Speed;
        u_xlat24 = i.vs_TEXCOORD2.y * transpose(unity_WorldToObject)[1].y;
        u_xlat24 = transpose(unity_WorldToObject)[0].y * i.vs_TEXCOORD2.x + u_xlat24;
        u_xlat24 = transpose(unity_WorldToObject)[2].y * i.vs_TEXCOORD2.z + u_xlat24;
        u_xlat24 = u_xlat24 + transpose(unity_WorldToObject)[3].y;
        u_xlat1 = (-_DissolveYMin) + _DissolveYMax;
        u_xlat1 = max(u_xlat1, 9.99999975e-06);
        u_xlat1 = ((float)(1.0)) / u_xlat1;
        u_xlat24 = u_xlat24 + (-_DissolveYMin);
        u_xlat24 = u_xlat1 * u_xlat24;
        u_xlat24 = clamp(u_xlat24, 0.0, 1.0);
        u_xlat24 = u_xlat24 + (-_VerticalDissolve);
        u_xlat24 = u_xlat24 + -9.99999975e-06;
        u_xlatb24 = u_xlat24<0.0;
        if(u_xlatb24){discard;}
        u_xlat0.xy = u_xlat16.xx * u_xlat0.xy;
        u_xlat0.xy = u_xlat0.xy / ((float2)(float2(_Density, _Density)));
        u_xlat0.xy = u_xlat0.xy + i.vs_TEXCOORD0.xy;
        u_xlat16.x = _Density * 0.707099974;
        u_xlat0.xy = u_xlat16.xx * u_xlat0.xy;
        u_xlat16.xy = floor(u_xlat0.xy);
        u_xlat0.xy = frac(u_xlat0.xy);
        u_xlat1 = _Spacing * 0.300000012 + 0.100000001;
        u_xlat9 = (-u_xlat1) * 2.0 + 1.0;
        u_xlat2.xyz = _Color.xyz;
        [unroll(8)] for(int u_xlati_loop_1 = ((int)(0xFFFFFFFFu)) ; u_xlati_loop_1<=1 ; u_xlati_loop_1++)
        {
            u_xlat3.y = ((float)(u_xlati_loop_1));
            u_xlat4.xyz = u_xlat2.xyz;
            u_xlati25 = ((int)(0xFFFFFFFFu));
            [unroll(8)] while(true){
                u_xlatb26 = 1<u_xlati25;
                if(u_xlatb26){break;}
                u_xlat3.x = ((float)(u_xlati25));
                u_xlat19.xy = u_xlat16.xy + u_xlat3.xy;
                u_xlat26 = u_xlat19.y + u_xlat19.x;
                u_xlat26 = u_xlat26 * 0.5;
                u_xlatb28 = u_xlat26>=(-u_xlat26);
                u_xlat26 = frac(abs(u_xlat26));
                u_xlat26 = (u_xlatb28) ? u_xlat26 : (-u_xlat26);
                u_xlatb26 = 0.25>=u_xlat26;
                if(u_xlatb26){
                    u_xlati26 = u_xlati25 + 1;
                    u_xlati25 = u_xlati26;
                    continue;
                }
                u_xlat26 = dot(u_xlat19.xy, float2(127.099998, 311.700012));
                u_xlat19.x = dot(u_xlat19.xy, float2(269.5, 183.300003));
                u_xlat5.x = sin(u_xlat26);
                u_xlat5.y = sin(u_xlat19.x);
                u_xlat19.xy = u_xlat5.xy * float2(43758.5469, 43758.5469);
                u_xlat19.xy = frac(u_xlat19.xy);
                u_xlat5.xy = u_xlat19.xy * ((float2)(u_xlat9)) + ((float2)(u_xlat1));
                u_xlat3.xz = u_xlat0.xy + (-u_xlat3.xy);
                u_xlat3.xz = (-u_xlat5.xy) + u_xlat3.xz;
                u_xlat26 = u_xlat19.y * _RotationVariance;
                u_xlat26 = u_xlat26 * 0.0174532924;
                u_xlat5.x = sin(u_xlat26);
                u_xlat6.x = cos(u_xlat26);
                u_xlat7.x = (-u_xlat5.x);
                u_xlat7.y = u_xlat6.x;
                u_xlat6.x = dot(u_xlat7.yx, u_xlat3.xz);
                u_xlat7.z = u_xlat5.x;
                u_xlat6.y = dot(u_xlat7.zy, u_xlat3.xz);
                u_xlat3.xz = u_xlat6.xy / ((float2)(float2(_Scale, _Scale)));
                u_xlat26 = dot(u_xlat3.xz, u_xlat3.xz);
                u_xlat26 = sqrt(u_xlat26);
                u_xlatb26 = u_xlat26<0.800000012;
                if(u_xlatb26){
                    u_xlat3.xz = u_xlat3.xz * float2(0.5, 0.5) + float2(0.5, 0.5);
                    u_xlatb5.xy = ((u_xlat3.xzxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                    u_xlatb21.xy = ((float4(1.0, 1.0, 1.0, 1.0)) >= (u_xlat3.xzxz)).xy;
                    u_xlatb26 = u_xlatb21.x && u_xlatb5.x;
                    u_xlatb26 = u_xlatb5.y && u_xlatb26;
                    u_xlatb26 = u_xlatb21.y && u_xlatb26;
                    if(u_xlatb26){
                        u_xlat16_5 = tex2D(_MainTex, (u_xlat3.xz).xy);
                        u_xlatb26 = 0.100000001<u_xlat16_5.w;
                        u_xlat3.xzw = _QuestionColor.xyz * u_xlat16_5.xyz + (-u_xlat4.xyz);
                        u_xlat3.xzw = u_xlat16_5.www * u_xlat3.xzw + u_xlat4.xyz;
                        u_xlat4.xyz = (((bool)(u_xlatb26))) ? u_xlat3.xzw : u_xlat4.xyz;
                    }
                }
                u_xlati25 = u_xlati25 + 1;
            }
            u_xlat2.xyz = u_xlat4.xyz;
        }
        u_xlat0.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat0.x = rsqrt(u_xlat0.x);
        u_xlat0.xyz = u_xlat0.xxx * _WorldSpaceLightPos0.xyz;
        u_xlat0.x = dot(i.vs_TEXCOORD1.xyz, u_xlat0.xyz);
        u_xlat0.x = max(u_xlat0.x, 0.0);
        u_xlat0.x = (-u_xlat0.x) + 1.0;
        u_xlat0.x = u_xlat0.x * _ShadowColor.w;
        u_xlat8.xyz = (-u_xlat2.xyz) + _ShadowColor.xyz;
        u_xlat0.xyz = u_xlat0.xxx * u_xlat8.xyz + u_xlat2.xyz;
        SV_Target0.xyz = u_xlat0.xyz;
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
    Fallback "Diffuse"
}