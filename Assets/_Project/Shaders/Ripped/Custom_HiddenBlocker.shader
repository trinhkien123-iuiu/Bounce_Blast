// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.
Shader "Custom/HiddenBlocker"
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
        [Header(Dissolve)] _DissolveTex ("Dissolve Texture", 2D) = "white" { }
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.0
        _DissolveScale ("Dissolve Scale", Range(0, 10)) = 3.0
        _DissolveColor ("Dissolve Edge Color", Color) = (1,0,0,1)
        _DissolveWidth ("Dissolve Edge Width", Range(0, 0.2)) = 0.05000000074505806
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" "ShadowSupport"="true" }
            Cull Back
            ZWrite On
            ZTest LEqual
            Offset 0.0010000000474974513, 0.0010000000474974513
        
            CGPROGRAM
            #pragma vertex vert_entry
            #pragma fragment frag_entry
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            float4 _Color;
            float _Density;
            float4 _Direction;
            float _DissolveAmount;
            float4 _DissolveColor;
            float _DissolveScale;
            sampler2D _DissolveTex;
            float _DissolveWidth;
            float _Glossiness;
            sampler2D _MainTex;
            float _Metallic;
            float4 _QuestionColor;
            float _RotationVariance;
            float _Scale;
            float4 _ShadowColor;
            float _Spacing;
            float _Speed;
            sampler2D unity_NHxRoughness;
        
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
        float3 vs_TEXCOORD2 : TEXCOORD2;
    };
    v2f_v0 vert_v0(appdata_v0 v) {
        v2f_v0 o = (v2f_v0)0;
        float4 vs_TEXCOORD4;
        float4 vs_TEXCOORD5;
        float4 u_xlat0;
        float4 u_xlat16_0;
        float4 u_xlat1;
        float u_xlat16_2;
        float3 u_xlat16_3;
        float u_xlat12;
    
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
        u_xlat12 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat12 = rsqrt(u_xlat12);
        u_xlat0.xyz = ((float3)(u_xlat12)) * u_xlat0.xyz;
        o.vs_TEXCOORD0.xyz = u_xlat0.xyz;
        u_xlat16_2 = u_xlat0.y * u_xlat0.y;
        u_xlat16_2 = u_xlat0.x * u_xlat0.x + (-u_xlat16_2);
        u_xlat16_0 = u_xlat0.yzzx * u_xlat0.xyzz;
        u_xlat16_3.x = dot(unity_SHBr, u_xlat16_0);
        u_xlat16_3.y = dot(unity_SHBg, u_xlat16_0);
        u_xlat16_3.z = dot(unity_SHBb, u_xlat16_0);
        o.vs_TEXCOORD2.xyz = unity_SHC.xyz * ((float3)(u_xlat16_2)) + u_xlat16_3.xyz;
        vs_TEXCOORD4 = float4(0.0, 0.0, 0.0, 0.0);
        vs_TEXCOORD5 = float4(0.0, 0.0, 0.0, 0.0);
        return o;
    
        return o;
    }
    float4 frag_v0(v2f_v0 i, float vface : VFACE) : SV_Target {
        float4 SV_Target0 = 0;
        float3 u_xlat0;
        float4 u_xlat1;
        float4 u_xlat16_1;
        float4 u_xlat2;
        bool4 u_xlatb2;
        float4 u_xlat3;
        float4 u_xlat16_3;
        int3 u_xlati3;
        bool4 u_xlatb3;
        float4 u_xlat4;
        float4 u_xlat16_4;
        bool4 u_xlatb4;
        float4 u_xlat5;
        float4 u_xlat16_5;
        bool2 u_xlatb5;
        float4 u_xlat6;
        float4 u_xlat16_6;
        float3 u_xlat7;
        float3 u_xlat16_8;
        float3 u_xlat16_9;
        float3 u_xlat16_10;
        float3 u_xlat16_11;
        float3 u_xlat16_12;
        float3 u_xlat16_13;
        float3 u_xlat16_14;
        float u_xlat15;
        float u_xlat16;
        float3 u_xlat17;
        int u_xlati17;
        bool u_xlatb17;
        float u_xlat18;
        bool2 u_xlatb18;
        float u_xlat19;
        bool2 u_xlatb19;
        bool2 u_xlatb20;
        float2 u_xlat31;
        float u_xlat32;
        bool u_xlatb32;
        float2 u_xlat33;
        bool2 u_xlatb33;
        float2 u_xlat34;
        bool2 u_xlatb34;
        float u_xlat45;
        float u_xlat16_45;
        bool u_xlatb45;
        bool u_xlatb46;
        float u_xlat47;
        bool u_xlatb47;
        float u_xlat48;
        bool u_xlatb48;
        float u_xlat16_53;
        float u_xlat16_54;
        float4 TempArray0[9];
        float4 TempArray1[9];
    
        u_xlat0.xyz = (-i.vs_TEXCOORD1.xyz) + _WorldSpaceCameraPos.xyz;
        u_xlat45 = dot(u_xlat0.xyz, u_xlat0.xyz);
        u_xlat45 = rsqrt(u_xlat45);
        u_xlat0.xyz = ((float3)(u_xlat45)) * u_xlat0.xyz;
        u_xlat45 = dot(_Direction.xy, _Direction.xy);
        u_xlat45 = rsqrt(u_xlat45);
        u_xlat1.xy = ((float2)(u_xlat45)) * _Direction.xy;
        u_xlat45 = _Time.y * _Speed + 0.00100000005;
        u_xlat1.xy = ((float2)(u_xlat45)) * u_xlat1.xy;
        u_xlat2.xyz = abs(i.vs_TEXCOORD0.xyz) + float3(9.99999975e-06, 9.99999975e-06, 9.99999975e-06);
        u_xlatb2.xyz = ((u_xlat2.yzzy) < (u_xlat2.xxyx)).xyz;
        u_xlatb45 = u_xlatb2.y && u_xlatb2.x;
        u_xlat31.xy = (u_xlatb2.z) ? i.vs_TEXCOORD1.xz : i.vs_TEXCOORD1.xy;
        u_xlat31.xy = (((bool)(u_xlatb45))) ? i.vs_TEXCOORD1.yz : u_xlat31.xy;
        u_xlat2.xy = u_xlat31.xy / ((float2)(float2(_DissolveScale, _DissolveScale)));
        u_xlat16_45 = tex2D(_DissolveTex, (u_xlat2.xy).xy).x;
        u_xlat45 = u_xlat16_45 + (-_DissolveAmount);
        u_xlatb2.x = u_xlat45<0.0;
        if(u_xlatb2.x){discard;}
        u_xlat2.x = ((float)(1.0)) / _Density;
        u_xlat1.xy = u_xlat1.xy * u_xlat2.xx + u_xlat31.xy;
        u_xlat31.x = _Density * 0.707099974;
        u_xlat1.xy = u_xlat31.xx * u_xlat1.xy;
        u_xlat1.zw = floor(u_xlat1.xy);
        u_xlat1.xy = frac(u_xlat1.xy);
        u_xlat2 = u_xlat1.zwzz + float4(999.0, 999.0, 1000.0, 1001.0);
        u_xlat3 = u_xlat2 * float4(0.5, 0.5, 0.5, 0.5);
        u_xlat3 = floor(u_xlat3);
        u_xlat2 = (-u_xlat3) * float4(2.0, 2.0, 2.0, 2.0) + u_xlat2;
        u_xlatb2 = ((u_xlat2) >= (float4(1.0, 1.0, 1.0, 1.0)));
        u_xlat2.x = u_xlatb2.x ? ((float)(1.0)) : 0.0;
        u_xlat2.y = u_xlatb2.y ? ((float)(1.0)) : 0.0;
        u_xlat2.z = u_xlatb2.z ? ((float)(1.0)) : 0.0;
        u_xlat2.w = u_xlatb2.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat3.xyz = u_xlat2.yyy + u_xlat2.xzw;
        u_xlat4.xyz = u_xlat3.xyz * float3(0.5, 0.5, 0.5);
        u_xlat4.xyz = floor(u_xlat4.xyz);
        u_xlat3.xyz = (-u_xlat4.xyz) * float3(2.0, 2.0, 2.0) + u_xlat3.xyz;
        u_xlati3.xyz = ((int3)(((uint3)(((float4(0.5, 0.5, 0.5, 0.0)) >= (u_xlat3.xyzx)).xyz)) * 0xFFFFFFFFu));
        TempArray0[0] = float4(0.0, 0.0, 0.0, 0.0);
        TempArray1[0].x = 0.0;
        u_xlati17 = ((int)(((uint)(((uint)(u_xlati3.x)) & 1u))));
        if(u_xlati3.x == 0) {
            u_xlat3.xw = u_xlat1.zw + float2(9999.0, 9999.0);
            u_xlat4.x = dot(u_xlat3.xw, float2(127.099998, 311.700012));
            u_xlat3.x = dot(u_xlat3.xw, float2(269.5, 183.300003));
            u_xlat4.x = sin(u_xlat4.x);
            u_xlat4.y = sin(u_xlat3.x);
            u_xlat3.xw = u_xlat4.xy * float2(43758.5469, 43758.5469);
            u_xlat3.xw = frac(u_xlat3.xw);
            u_xlat4.x = _Spacing * 0.300000012 + 0.100000001;
            u_xlat19 = (-u_xlat4.x) * 2.0 + 1.0;
            u_xlat4.xy = u_xlat3.xw * ((float2)(u_xlat19)) + u_xlat4.xx;
            u_xlat34.xy = u_xlat1.xy + float2(1.0, 1.0);
            u_xlat4.xy = (-u_xlat4.xy) + u_xlat34.xy;
            u_xlat3.x = u_xlat3.w * _RotationVariance;
            u_xlat3.x = u_xlat3.x * 0.0174532924;
            u_xlat5.x = cos(u_xlat3.x);
            u_xlat3.x = sin(u_xlat3.x);
            u_xlat6.x = (-u_xlat3.x);
            u_xlat6.y = u_xlat5.x;
            u_xlat5.x = dot(u_xlat6.yx, u_xlat4.xy);
            u_xlat6.z = u_xlat3.x;
            u_xlat5.y = dot(u_xlat6.zy, u_xlat4.xy);
            u_xlat3.xw = u_xlat5.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat4.x = dot(u_xlat3.xw, u_xlat3.xw);
            u_xlat4.x = sqrt(u_xlat4.x);
            u_xlatb19.x = u_xlat4.x<0.800000012;
            if(u_xlatb19.x){
                u_xlat3.xw = u_xlat3.xw * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb19.xy = ((u_xlat3.xwxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                u_xlatb19.x = u_xlatb19.y && u_xlatb19.x;
                u_xlatb34.xy = ((float4(1.0, 1.0, 1.0, 1.0)) >= (u_xlat3.xwxw)).xy;
                u_xlatb34.x = u_xlatb34.y && u_xlatb34.x;
                u_xlatb19.x = u_xlatb34.x && u_xlatb19.x;
                if(u_xlatb19.x){
                    u_xlat16_5 = tex2Dlod(_MainTex, float4((u_xlat3.xw).xy, 0.0, 0.0));
                    u_xlat3.x = (-u_xlat4.x) * 0.714285731 + 1.0;
                    u_xlat3.x = max(u_xlat3.x, 0.0);
                    u_xlat48 = u_xlat3.x * -2.0 + 3.0;
                    u_xlat3.x = u_xlat3.x * u_xlat3.x;
                    u_xlat3.x = u_xlat3.x * u_xlat48;
                    u_xlat4 = u_xlat16_5;
                } else {
                    u_xlat16_4.x = ((float)(0.0));
                    u_xlat16_4.y = ((float)(0.0));
                    u_xlat16_4.z = ((float)(0.0));
                    u_xlat16_4.w = ((float)(0.0));
                    u_xlat3.x = 0.0;
                    u_xlat4 = u_xlat16_4;
                }
                u_xlat5 = u_xlat4;
            } else {
                u_xlat16_5.x = ((float)(0.0));
                u_xlat16_5.y = ((float)(0.0));
                u_xlat16_5.z = ((float)(0.0));
                u_xlat16_5.w = ((float)(0.0));
                u_xlat3.x = 0.0;
                u_xlat5 = u_xlat16_5;
            }
            TempArray1[u_xlati17].x = u_xlat3.x;
            TempArray0[u_xlati17] = u_xlat5;
            u_xlati17 = 1;
        }
        u_xlat16_4 = TempArray0[u_xlati17];
        u_xlat16_4 = (u_xlati3.y != 0) ? float4(0.0, 0.0, 0.0, 0.0) : u_xlat16_4;
        TempArray0[u_xlati17] = u_xlat16_4;
        u_xlat3.x = TempArray1[u_xlati17].x;
        u_xlat3.x = (u_xlati3.y != 0) ? 0.0 : u_xlat3.x;
        TempArray1[u_xlati17].x = u_xlat3.x;
        u_xlati17 = (-u_xlati3.y) + u_xlati17;
        if(u_xlati3.y == 0) {
            u_xlat3.xy = u_xlat1.zw + float2(10000.0, 9999.0);
            u_xlat3.w = dot(u_xlat3.xy, float2(127.099998, 311.700012));
            u_xlat3.x = dot(u_xlat3.xy, float2(269.5, 183.300003));
            u_xlat5.xy = sin(u_xlat3.wx);
            u_xlat3.xy = u_xlat5.xy * float2(43758.5469, 43758.5469);
            u_xlat3.xy = frac(u_xlat3.xy);
            u_xlat48 = _Spacing * 0.300000012 + 0.100000001;
            u_xlat5.x = (-u_xlat48) * 2.0 + 1.0;
            u_xlat3.xw = u_xlat3.xy * u_xlat5.xx + ((float2)(u_xlat48));
            u_xlat5.xy = u_xlat1.xy + float2(-0.0, 1.0);
            u_xlat3.xw = (-u_xlat3.xw) + u_xlat5.xy;
            u_xlat18 = u_xlat3.y * _RotationVariance;
            u_xlat18 = u_xlat18 * 0.0174532924;
            u_xlat5.x = sin(u_xlat18);
            u_xlat6.x = cos(u_xlat18);
            u_xlat7.x = (-u_xlat5.x);
            u_xlat7.y = u_xlat6.x;
            u_xlat6.x = dot(u_xlat7.yx, u_xlat3.xw);
            u_xlat7.z = u_xlat5.x;
            u_xlat6.y = dot(u_xlat7.zy, u_xlat3.xw);
            u_xlat3.xy = u_xlat6.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat48 = dot(u_xlat3.xy, u_xlat3.xy);
            u_xlat48 = sqrt(u_xlat48);
            u_xlatb5.x = u_xlat48<0.800000012;
            if(u_xlatb5.x){
                u_xlat3.xy = u_xlat3.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb5.xy = ((u_xlat3.xyxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                u_xlatb5.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb20.xy = ((float4(1.0, 1.0, 0.0, 0.0)) >= (u_xlat3.xyxx)).xy;
                u_xlatb20.x = u_xlatb20.y && u_xlatb20.x;
                u_xlatb5.x = u_xlatb20.x && u_xlatb5.x;
                if(u_xlatb5.x){
                    u_xlat16_4 = tex2Dlod(_MainTex, float4((u_xlat3.xy).xy, 0.0, 0.0));
                    u_xlat3.x = (-u_xlat48) * 0.714285731 + 1.0;
                    u_xlat3.x = max(u_xlat3.x, 0.0);
                    u_xlat18 = u_xlat3.x * -2.0 + 3.0;
                    u_xlat3.x = u_xlat3.x * u_xlat3.x;
                    u_xlat3.x = u_xlat3.x * u_xlat18;
                    u_xlat5 = u_xlat16_4;
                } else {
                    u_xlat16_5.x = ((float)(0.0));
                    u_xlat16_5.y = ((float)(0.0));
                    u_xlat16_5.z = ((float)(0.0));
                    u_xlat16_5.w = ((float)(0.0));
                    u_xlat3.x = 0.0;
                    u_xlat5 = u_xlat16_5;
                }
                u_xlat4 = u_xlat5;
            } else {
                u_xlat16_4.x = ((float)(0.0));
                u_xlat16_4.y = ((float)(0.0));
                u_xlat16_4.z = ((float)(0.0));
                u_xlat16_4.w = ((float)(0.0));
                u_xlat3.x = 0.0;
                u_xlat4 = u_xlat16_4;
            }
            TempArray1[u_xlati17].x = u_xlat3.x;
            TempArray0[u_xlati17] = u_xlat4;
            u_xlati17 = u_xlati17 + 1;
        }
        u_xlat16_4 = TempArray0[u_xlati17];
        u_xlat16_4 = (u_xlati3.z != 0) ? float4(0.0, 0.0, 0.0, 0.0) : u_xlat16_4;
        TempArray0[u_xlati17] = u_xlat16_4;
        u_xlat3.x = TempArray1[u_xlati17].x;
        u_xlat3.x = (u_xlati3.z != 0) ? 0.0 : u_xlat3.x;
        TempArray1[u_xlati17].x = u_xlat3.x;
        u_xlati17 = (-u_xlati3.z) + u_xlati17;
        if(u_xlati3.z == 0) {
            u_xlat3.xy = u_xlat1.zw + float2(10001.0, 9999.0);
            u_xlat3.z = dot(u_xlat3.xy, float2(127.099998, 311.700012));
            u_xlat3.x = dot(u_xlat3.xy, float2(269.5, 183.300003));
            u_xlat5.xy = sin(u_xlat3.zx);
            u_xlat3.xy = u_xlat5.xy * float2(43758.5469, 43758.5469);
            u_xlat3.xy = frac(u_xlat3.xy);
            u_xlat33.x = _Spacing * 0.300000012 + 0.100000001;
            u_xlat48 = (-u_xlat33.x) * 2.0 + 1.0;
            u_xlat3.xz = u_xlat3.xy * ((float2)(u_xlat48)) + u_xlat33.xx;
            u_xlat5.xy = u_xlat1.xy + float2(-1.0, 1.0);
            u_xlat3.xz = (-u_xlat3.xz) + u_xlat5.xy;
            u_xlat18 = u_xlat3.y * _RotationVariance;
            u_xlat18 = u_xlat18 * 0.0174532924;
            u_xlat5.x = sin(u_xlat18);
            u_xlat6.x = cos(u_xlat18);
            u_xlat7.x = (-u_xlat5.x);
            u_xlat7.y = u_xlat6.x;
            u_xlat6.x = dot(u_xlat7.yx, u_xlat3.xz);
            u_xlat7.z = u_xlat5.x;
            u_xlat6.y = dot(u_xlat7.zy, u_xlat3.xz);
            u_xlat3.xy = u_xlat6.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat33.x = dot(u_xlat3.xy, u_xlat3.xy);
            u_xlat33.x = sqrt(u_xlat33.x);
            u_xlatb48 = u_xlat33.x<0.800000012;
            if(u_xlatb48){
                u_xlat3.xy = u_xlat3.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb5.xy = ((u_xlat3.xyxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                u_xlatb48 = u_xlatb5.y && u_xlatb5.x;
                u_xlatb5.xy = ((float4(1.0, 1.0, 0.0, 0.0)) >= (u_xlat3.xyxx)).xy;
                u_xlatb5.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb48 = u_xlatb48 && u_xlatb5.x;
                if(u_xlatb48){
                    u_xlat16_4 = tex2Dlod(_MainTex, float4((u_xlat3.xy).xy, 0.0, 0.0));
                    u_xlat3.x = (-u_xlat33.x) * 0.714285731 + 1.0;
                    u_xlat3.x = max(u_xlat3.x, 0.0);
                    u_xlat18 = u_xlat3.x * -2.0 + 3.0;
                    u_xlat3.x = u_xlat3.x * u_xlat3.x;
                    u_xlat3.x = u_xlat3.x * u_xlat18;
                    u_xlat5 = u_xlat16_4;
                } else {
                    u_xlat16_5.x = ((float)(0.0));
                    u_xlat16_5.y = ((float)(0.0));
                    u_xlat16_5.z = ((float)(0.0));
                    u_xlat16_5.w = ((float)(0.0));
                    u_xlat3.x = 0.0;
                    u_xlat5 = u_xlat16_5;
                }
                u_xlat4 = u_xlat5;
            } else {
                u_xlat16_4.x = ((float)(0.0));
                u_xlat16_4.y = ((float)(0.0));
                u_xlat16_4.z = ((float)(0.0));
                u_xlat16_4.w = ((float)(0.0));
                u_xlat3.x = 0.0;
                u_xlat4 = u_xlat16_4;
            }
            TempArray1[u_xlati17].x = u_xlat3.x;
            TempArray0[u_xlati17] = u_xlat4;
            u_xlati17 = u_xlati17 + 1;
        }
        u_xlat3 = u_xlat1.wwww + float4(1000.0, 1000.0, 1000.0, 1001.0);
        u_xlat4 = u_xlat3.zzzw * float4(0.5, 0.5, 0.5, 0.5);
        u_xlat4 = floor(u_xlat4);
        u_xlat3 = (-u_xlat4) * float4(2.0, 2.0, 2.0, 2.0) + u_xlat3;
        u_xlatb3 = ((u_xlat3) >= (float4(1.0, 1.0, 1.0, 1.0)));
        u_xlat3.x = u_xlatb3.x ? ((float)(1.0)) : 0.0;
        u_xlat3.y = u_xlatb3.y ? ((float)(1.0)) : 0.0;
        u_xlat3.z = u_xlatb3.z ? ((float)(1.0)) : 0.0;
        u_xlat3.w = u_xlatb3.w ? ((float)(1.0)) : 0.0;
    ;
        u_xlat4 = u_xlat2.xzwx + u_xlat3;
        u_xlat5 = u_xlat4 * float4(0.5, 0.5, 0.5, 0.5);
        u_xlat5 = floor(u_xlat5);
        u_xlat4 = (-u_xlat5) * float4(2.0, 2.0, 2.0, 2.0) + u_xlat4;
        u_xlatb4 = ((float4(0.5, 0.5, 0.5, 0.5)) >= (u_xlat4));
        if(u_xlatb4.x){
            TempArray0[u_xlati17] = float4(0.0, 0.0, 0.0, 0.0);
            TempArray1[u_xlati17].x = 0.0;
            u_xlati17 = u_xlati17 + 1;
        }
        if(!u_xlatb4.x){
            u_xlat3.xy = u_xlat1.zw + float2(9999.0, 10000.0);
            u_xlat2.x = dot(u_xlat3.xy, float2(127.099998, 311.700012));
            u_xlat3.x = dot(u_xlat3.xy, float2(269.5, 183.300003));
            u_xlat5.x = sin(u_xlat2.x);
            u_xlat5.y = sin(u_xlat3.x);
            u_xlat3.xy = u_xlat5.xy * float2(43758.5469, 43758.5469);
            u_xlat3.xy = frac(u_xlat3.xy);
            u_xlat2.x = _Spacing * 0.300000012 + 0.100000001;
            u_xlat33.x = (-u_xlat2.x) * 2.0 + 1.0;
            u_xlat3.xz = u_xlat3.xy * u_xlat33.xx + u_xlat2.xx;
            u_xlat5.xy = u_xlat1.xy + float2(1.0, -0.0);
            u_xlat3.xz = (-u_xlat3.xz) + u_xlat5.xy;
            u_xlat2.x = u_xlat3.y * _RotationVariance;
            u_xlat2.x = u_xlat2.x * 0.0174532924;
            u_xlat5.x = cos(u_xlat2.x);
            u_xlat2.x = sin(u_xlat2.x);
            u_xlat6.x = (-u_xlat2.x);
            u_xlat6.y = u_xlat5.x;
            u_xlat5.x = dot(u_xlat6.yx, u_xlat3.xz);
            u_xlat6.z = u_xlat2.x;
            u_xlat5.y = dot(u_xlat6.zy, u_xlat3.xz);
            u_xlat3.xy = u_xlat5.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat2.x = dot(u_xlat3.xy, u_xlat3.xy);
            u_xlat2.x = sqrt(u_xlat2.x);
            u_xlatb33.x = u_xlat2.x<0.800000012;
            if(u_xlatb33.x){
                u_xlat3.xy = u_xlat3.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb5.xy = ((u_xlat3.xyxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                u_xlatb33.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb5.xy = ((float4(1.0, 1.0, 0.0, 0.0)) >= (u_xlat3.xyxx)).xy;
                u_xlatb5.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb33.x = u_xlatb33.x && u_xlatb5.x;
                if(u_xlatb33.x){
                    u_xlat16_5 = tex2Dlod(_MainTex, float4((u_xlat3.xy).xy, 0.0, 0.0));
                    u_xlat2.x = (-u_xlat2.x) * 0.714285731 + 1.0;
                    u_xlat2.x = max(u_xlat2.x, 0.0);
                    u_xlat3.x = u_xlat2.x * -2.0 + 3.0;
                    u_xlat2.x = u_xlat2.x * u_xlat2.x;
                    u_xlat2.x = u_xlat2.x * u_xlat3.x;
                    u_xlat6 = u_xlat16_5;
                } else {
                    u_xlat16_6.x = ((float)(0.0));
                    u_xlat16_6.y = ((float)(0.0));
                    u_xlat16_6.z = ((float)(0.0));
                    u_xlat16_6.w = ((float)(0.0));
                    u_xlat2.x = 0.0;
                    u_xlat6 = u_xlat16_6;
                }
                u_xlat5 = u_xlat6;
            } else {
                u_xlat16_5.x = ((float)(0.0));
                u_xlat16_5.y = ((float)(0.0));
                u_xlat16_5.z = ((float)(0.0));
                u_xlat16_5.w = ((float)(0.0));
                u_xlat2.x = 0.0;
                u_xlat5 = u_xlat16_5;
            }
            TempArray1[u_xlati17].x = u_xlat2.x;
            TempArray0[u_xlati17] = u_xlat5;
            u_xlati17 = u_xlati17 + 1;
        }
        if(u_xlatb4.y){
            TempArray0[u_xlati17] = float4(0.0, 0.0, 0.0, 0.0);
            TempArray1[u_xlati17].x = 0.0;
            u_xlati17 = u_xlati17 + 1;
        }
        if(!u_xlatb4.y){
            u_xlat3.xy = u_xlat1.zw + float2(10000.0, 10000.0);
            u_xlat2.x = dot(u_xlat3.xy, float2(127.099998, 311.700012));
            u_xlat3.x = dot(u_xlat3.xy, float2(269.5, 183.300003));
            u_xlat5.x = sin(u_xlat2.x);
            u_xlat5.y = sin(u_xlat3.x);
            u_xlat3.xy = u_xlat5.xy * float2(43758.5469, 43758.5469);
            u_xlat3.xy = frac(u_xlat3.xy);
            u_xlat2.x = _Spacing * 0.300000012 + 0.100000001;
            u_xlat33.x = (-u_xlat2.x) * 2.0 + 1.0;
            u_xlat3.xz = u_xlat3.xy * u_xlat33.xx + u_xlat2.xx;
            u_xlat3.xz = u_xlat1.xy + (-u_xlat3.xz);
            u_xlat2.x = u_xlat3.y * _RotationVariance;
            u_xlat2.x = u_xlat2.x * 0.0174532924;
            u_xlat5.x = cos(u_xlat2.x);
            u_xlat2.x = sin(u_xlat2.x);
            u_xlat6.x = (-u_xlat2.x);
            u_xlat6.y = u_xlat5.x;
            u_xlat5.x = dot(u_xlat6.yx, u_xlat3.xz);
            u_xlat6.z = u_xlat2.x;
            u_xlat5.y = dot(u_xlat6.zy, u_xlat3.xz);
            u_xlat3.xy = u_xlat5.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat2.x = dot(u_xlat3.xy, u_xlat3.xy);
            u_xlat2.x = sqrt(u_xlat2.x);
            u_xlatb33.x = u_xlat2.x<0.800000012;
            if(u_xlatb33.x){
                u_xlat3.xy = u_xlat3.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb5.xy = ((u_xlat3.xyxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                u_xlatb33.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb5.xy = ((float4(1.0, 1.0, 0.0, 0.0)) >= (u_xlat3.xyxx)).xy;
                u_xlatb5.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb33.x = u_xlatb33.x && u_xlatb5.x;
                if(u_xlatb33.x){
                    u_xlat16_5 = tex2Dlod(_MainTex, float4((u_xlat3.xy).xy, 0.0, 0.0));
                    u_xlat2.x = (-u_xlat2.x) * 0.714285731 + 1.0;
                    u_xlat2.x = max(u_xlat2.x, 0.0);
                    u_xlat3.x = u_xlat2.x * -2.0 + 3.0;
                    u_xlat2.x = u_xlat2.x * u_xlat2.x;
                    u_xlat2.x = u_xlat2.x * u_xlat3.x;
                    u_xlat6 = u_xlat16_5;
                } else {
                    u_xlat16_6.x = ((float)(0.0));
                    u_xlat16_6.y = ((float)(0.0));
                    u_xlat16_6.z = ((float)(0.0));
                    u_xlat16_6.w = ((float)(0.0));
                    u_xlat2.x = 0.0;
                    u_xlat6 = u_xlat16_6;
                }
                u_xlat5 = u_xlat6;
            } else {
                u_xlat16_5.x = ((float)(0.0));
                u_xlat16_5.y = ((float)(0.0));
                u_xlat16_5.z = ((float)(0.0));
                u_xlat16_5.w = ((float)(0.0));
                u_xlat2.x = 0.0;
                u_xlat5 = u_xlat16_5;
            }
            TempArray1[u_xlati17].x = u_xlat2.x;
            TempArray0[u_xlati17] = u_xlat5;
            u_xlati17 = u_xlati17 + 1;
        }
        if(u_xlatb4.z){
            TempArray0[u_xlati17] = float4(0.0, 0.0, 0.0, 0.0);
            TempArray1[u_xlati17].x = 0.0;
            u_xlati17 = u_xlati17 + 1;
        }
        if(!u_xlatb4.z){
            u_xlat3.xy = u_xlat1.zw + float2(10001.0, 10000.0);
            u_xlat2.x = dot(u_xlat3.xy, float2(127.099998, 311.700012));
            u_xlat3.x = dot(u_xlat3.xy, float2(269.5, 183.300003));
            u_xlat5.x = sin(u_xlat2.x);
            u_xlat5.y = sin(u_xlat3.x);
            u_xlat3.xy = u_xlat5.xy * float2(43758.5469, 43758.5469);
            u_xlat3.xy = frac(u_xlat3.xy);
            u_xlat2.x = _Spacing * 0.300000012 + 0.100000001;
            u_xlat33.x = (-u_xlat2.x) * 2.0 + 1.0;
            u_xlat3.xz = u_xlat3.xy * u_xlat33.xx + u_xlat2.xx;
            u_xlat5.xy = u_xlat1.xy + float2(-1.0, -0.0);
            u_xlat3.xz = (-u_xlat3.xz) + u_xlat5.xy;
            u_xlat2.x = u_xlat3.y * _RotationVariance;
            u_xlat2.x = u_xlat2.x * 0.0174532924;
            u_xlat5.x = cos(u_xlat2.x);
            u_xlat2.x = sin(u_xlat2.x);
            u_xlat6.x = (-u_xlat2.x);
            u_xlat6.y = u_xlat5.x;
            u_xlat5.x = dot(u_xlat6.yx, u_xlat3.xz);
            u_xlat6.z = u_xlat2.x;
            u_xlat5.y = dot(u_xlat6.zy, u_xlat3.xz);
            u_xlat3.xy = u_xlat5.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat2.x = dot(u_xlat3.xy, u_xlat3.xy);
            u_xlat2.x = sqrt(u_xlat2.x);
            u_xlatb33.x = u_xlat2.x<0.800000012;
            if(u_xlatb33.x){
                u_xlat3.xy = u_xlat3.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb5.xy = ((u_xlat3.xyxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                u_xlatb33.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb5.xy = ((float4(1.0, 1.0, 0.0, 0.0)) >= (u_xlat3.xyxx)).xy;
                u_xlatb5.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb33.x = u_xlatb33.x && u_xlatb5.x;
                if(u_xlatb33.x){
                    u_xlat16_5 = tex2Dlod(_MainTex, float4((u_xlat3.xy).xy, 0.0, 0.0));
                    u_xlat2.x = (-u_xlat2.x) * 0.714285731 + 1.0;
                    u_xlat2.x = max(u_xlat2.x, 0.0);
                    u_xlat3.x = u_xlat2.x * -2.0 + 3.0;
                    u_xlat2.x = u_xlat2.x * u_xlat2.x;
                    u_xlat2.x = u_xlat2.x * u_xlat3.x;
                    u_xlat6 = u_xlat16_5;
                } else {
                    u_xlat16_6.x = ((float)(0.0));
                    u_xlat16_6.y = ((float)(0.0));
                    u_xlat16_6.z = ((float)(0.0));
                    u_xlat16_6.w = ((float)(0.0));
                    u_xlat2.x = 0.0;
                    u_xlat6 = u_xlat16_6;
                }
                u_xlat5 = u_xlat6;
            } else {
                u_xlat16_5.x = ((float)(0.0));
                u_xlat16_5.y = ((float)(0.0));
                u_xlat16_5.z = ((float)(0.0));
                u_xlat16_5.w = ((float)(0.0));
                u_xlat2.x = 0.0;
                u_xlat5 = u_xlat16_5;
            }
            TempArray1[u_xlati17].x = u_xlat2.x;
            TempArray0[u_xlati17] = u_xlat5;
            u_xlati17 = u_xlati17 + 1;
        }
        if(u_xlatb4.w){
            TempArray0[u_xlati17] = float4(0.0, 0.0, 0.0, 0.0);
            TempArray1[u_xlati17].x = 0.0;
            u_xlati17 = u_xlati17 + 1;
        }
        if(!u_xlatb4.w){
            u_xlat3.xy = u_xlat1.zw + float2(9999.0, 10001.0);
            u_xlat2.x = dot(u_xlat3.xy, float2(127.099998, 311.700012));
            u_xlat3.x = dot(u_xlat3.xy, float2(269.5, 183.300003));
            u_xlat5.x = sin(u_xlat2.x);
            u_xlat5.y = sin(u_xlat3.x);
            u_xlat3.xy = u_xlat5.xy * float2(43758.5469, 43758.5469);
            u_xlat3.xy = frac(u_xlat3.xy);
            u_xlat2.x = _Spacing * 0.300000012 + 0.100000001;
            u_xlat33.x = (-u_xlat2.x) * 2.0 + 1.0;
            u_xlat3.xz = u_xlat3.xy * u_xlat33.xx + u_xlat2.xx;
            u_xlat5.xy = u_xlat1.xy + float2(1.0, -1.0);
            u_xlat3.xz = (-u_xlat3.xz) + u_xlat5.xy;
            u_xlat2.x = u_xlat3.y * _RotationVariance;
            u_xlat2.x = u_xlat2.x * 0.0174532924;
            u_xlat5.x = cos(u_xlat2.x);
            u_xlat2.x = sin(u_xlat2.x);
            u_xlat6.x = (-u_xlat2.x);
            u_xlat6.y = u_xlat5.x;
            u_xlat5.x = dot(u_xlat6.yx, u_xlat3.xz);
            u_xlat6.z = u_xlat2.x;
            u_xlat5.y = dot(u_xlat6.zy, u_xlat3.xz);
            u_xlat3.xy = u_xlat5.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat2.x = dot(u_xlat3.xy, u_xlat3.xy);
            u_xlat2.x = sqrt(u_xlat2.x);
            u_xlatb33.x = u_xlat2.x<0.800000012;
            if(u_xlatb33.x){
                u_xlat3.xy = u_xlat3.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb5.xy = ((u_xlat3.xyxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                u_xlatb33.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb5.xy = ((float4(1.0, 1.0, 0.0, 0.0)) >= (u_xlat3.xyxx)).xy;
                u_xlatb5.x = u_xlatb5.y && u_xlatb5.x;
                u_xlatb33.x = u_xlatb33.x && u_xlatb5.x;
                if(u_xlatb33.x){
                    u_xlat16_4 = tex2Dlod(_MainTex, float4((u_xlat3.xy).xy, 0.0, 0.0));
                    u_xlat2.x = (-u_xlat2.x) * 0.714285731 + 1.0;
                    u_xlat2.x = max(u_xlat2.x, 0.0);
                    u_xlat3.x = u_xlat2.x * -2.0 + 3.0;
                    u_xlat2.x = u_xlat2.x * u_xlat2.x;
                    u_xlat2.x = u_xlat2.x * u_xlat3.x;
                    u_xlat5 = u_xlat16_4;
                } else {
                    u_xlat16_5.x = ((float)(0.0));
                    u_xlat16_5.y = ((float)(0.0));
                    u_xlat16_5.z = ((float)(0.0));
                    u_xlat16_5.w = ((float)(0.0));
                    u_xlat2.x = 0.0;
                    u_xlat5 = u_xlat16_5;
                }
                u_xlat4 = u_xlat5;
            } else {
                u_xlat16_4.x = ((float)(0.0));
                u_xlat16_4.y = ((float)(0.0));
                u_xlat16_4.z = ((float)(0.0));
                u_xlat16_4.w = ((float)(0.0));
                u_xlat2.x = 0.0;
                u_xlat4 = u_xlat16_4;
            }
            TempArray1[u_xlati17].x = u_xlat2.x;
            TempArray0[u_xlati17] = u_xlat4;
            u_xlati17 = u_xlati17 + 1;
        }
        u_xlat2.xz = u_xlat2.zw + u_xlat3.ww;
        u_xlat3.xy = u_xlat2.xz * float2(0.5, 0.5);
        u_xlat3.xy = floor(u_xlat3.xy);
        u_xlat2.xz = (-u_xlat3.xy) * float2(2.0, 2.0) + u_xlat2.xz;
        u_xlatb2.xz = ((float4(0.5, 0.0, 0.5, 0.0)) >= (u_xlat2.xxzx)).xz;
        if(u_xlatb2.x){
            TempArray0[u_xlati17] = float4(0.0, 0.0, 0.0, 0.0);
            TempArray1[u_xlati17].x = 0.0;
            u_xlati17 = u_xlati17 + 1;
        }
        if(!u_xlatb2.x){
            u_xlat2.xw = u_xlat1.zw + float2(10000.0, 10001.0);
            u_xlat3.x = dot(u_xlat2.xw, float2(127.099998, 311.700012));
            u_xlat2.x = dot(u_xlat2.xw, float2(269.5, 183.300003));
            u_xlat3.x = sin(u_xlat3.x);
            u_xlat3.y = sin(u_xlat2.x);
            u_xlat2.xw = u_xlat3.xy * float2(43758.5469, 43758.5469);
            u_xlat2.xw = frac(u_xlat2.xw);
            u_xlat3.x = _Spacing * 0.300000012 + 0.100000001;
            u_xlat18 = (-u_xlat3.x) * 2.0 + 1.0;
            u_xlat3.xy = u_xlat2.xw * ((float2)(u_xlat18)) + u_xlat3.xx;
            u_xlat33.xy = u_xlat1.xy + float2(-0.0, -1.0);
            u_xlat3.xy = (-u_xlat3.xy) + u_xlat33.xy;
            u_xlat2.x = u_xlat2.w * _RotationVariance;
            u_xlat2.x = u_xlat2.x * 0.0174532924;
            u_xlat5.x = cos(u_xlat2.x);
            u_xlat2.x = sin(u_xlat2.x);
            u_xlat6.x = (-u_xlat2.x);
            u_xlat6.y = u_xlat5.x;
            u_xlat5.x = dot(u_xlat6.yx, u_xlat3.xy);
            u_xlat6.z = u_xlat2.x;
            u_xlat5.y = dot(u_xlat6.zy, u_xlat3.xy);
            u_xlat2.xw = u_xlat5.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat3.x = dot(u_xlat2.xw, u_xlat2.xw);
            u_xlat3.x = sqrt(u_xlat3.x);
            u_xlatb18.x = u_xlat3.x<0.800000012;
            if(u_xlatb18.x){
                u_xlat2.xw = u_xlat2.xw * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb18.xy = ((u_xlat2.xwxx) >= (float4(0.0, 0.0, 0.0, 0.0))).xy;
                u_xlatb18.x = u_xlatb18.y && u_xlatb18.x;
                u_xlatb33.xy = ((float4(1.0, 1.0, 1.0, 1.0)) >= (u_xlat2.xwxw)).xy;
                u_xlatb33.x = u_xlatb33.y && u_xlatb33.x;
                u_xlatb18.x = u_xlatb33.x && u_xlatb18.x;
                if(u_xlatb18.x){
                    u_xlat16_4 = tex2Dlod(_MainTex, float4((u_xlat2.xw).xy, 0.0, 0.0));
                    u_xlat2.x = (-u_xlat3.x) * 0.714285731 + 1.0;
                    u_xlat2.x = max(u_xlat2.x, 0.0);
                    u_xlat47 = u_xlat2.x * -2.0 + 3.0;
                    u_xlat2.x = u_xlat2.x * u_xlat2.x;
                    u_xlat2.x = u_xlat2.x * u_xlat47;
                    u_xlat3 = u_xlat16_4;
                } else {
                    u_xlat16_3.x = ((float)(0.0));
                    u_xlat16_3.y = ((float)(0.0));
                    u_xlat16_3.z = ((float)(0.0));
                    u_xlat16_3.w = ((float)(0.0));
                    u_xlat2.x = 0.0;
                    u_xlat3 = u_xlat16_3;
                }
                u_xlat4 = u_xlat3;
            } else {
                u_xlat16_4.x = ((float)(0.0));
                u_xlat16_4.y = ((float)(0.0));
                u_xlat16_4.z = ((float)(0.0));
                u_xlat16_4.w = ((float)(0.0));
                u_xlat2.x = 0.0;
                u_xlat4 = u_xlat16_4;
            }
            TempArray1[u_xlati17].x = u_xlat2.x;
            TempArray0[u_xlati17] = u_xlat4;
            u_xlati17 = u_xlati17 + 1;
        }
        if(u_xlatb2.z){
            TempArray0[u_xlati17] = float4(0.0, 0.0, 0.0, 0.0);
            TempArray1[u_xlati17].x = 0.0;
            u_xlati17 = u_xlati17 + 1;
        }
        if(!u_xlatb2.z){
            u_xlat1 = u_xlat1 + float4(-1.0, -1.0, 10001.0, 10001.0);
            u_xlat2.x = dot(u_xlat1.zw, float2(127.099998, 311.700012));
            u_xlat31.x = dot(u_xlat1.zw, float2(269.5, 183.300003));
            u_xlat3.x = sin(u_xlat2.x);
            u_xlat3.y = sin(u_xlat31.x);
            u_xlat31.xy = u_xlat3.xy * float2(43758.5469, 43758.5469);
            u_xlat31.xy = frac(u_xlat31.xy);
            u_xlat2.x = _Spacing * 0.300000012 + 0.100000001;
            u_xlat32 = (-u_xlat2.x) * 2.0 + 1.0;
            u_xlat2.xz = u_xlat31.xy * ((float2)(u_xlat32)) + u_xlat2.xx;
            u_xlat1.xy = u_xlat1.xy + (-u_xlat2.xz);
            u_xlat31.x = u_xlat31.y * _RotationVariance;
            u_xlat31.x = u_xlat31.x * 0.0174532924;
            u_xlat2.x = sin(u_xlat31.x);
            u_xlat3.x = cos(u_xlat31.x);
            u_xlat5.x = (-u_xlat2.x);
            u_xlat5.y = u_xlat3.x;
            u_xlat3.x = dot(u_xlat5.yx, u_xlat1.xy);
            u_xlat5.z = u_xlat2.x;
            u_xlat3.y = dot(u_xlat5.zy, u_xlat1.xy);
            u_xlat1.xy = u_xlat3.xy / ((float2)(float2(_Scale, _Scale)));
            u_xlat31.x = dot(u_xlat1.xy, u_xlat1.xy);
            u_xlat31.x = sqrt(u_xlat31.x);
            u_xlatb46 = u_xlat31.x<0.800000012;
            if(u_xlatb46){
                u_xlat1.xy = u_xlat1.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                u_xlatb2.xz = ((u_xlat1.xxyx) >= (float4(0.0, 0.0, 0.0, 0.0))).xz;
                u_xlatb46 = u_xlatb2.z && u_xlatb2.x;
                u_xlatb2.xz = ((float4(1.0, 0.0, 1.0, 0.0)) >= (u_xlat1.xxyx)).xz;
                u_xlatb2.x = u_xlatb2.z && u_xlatb2.x;
                u_xlatb46 = u_xlatb46 && u_xlatb2.x;
                if(u_xlatb46){
                    u_xlat16_3 = tex2Dlod(_MainTex, float4((u_xlat1.xy).xy, 0.0, 0.0));
                    u_xlat1.x = (-u_xlat31.x) * 0.714285731 + 1.0;
                    u_xlat1.x = max(u_xlat1.x, 0.0);
                    u_xlat16 = u_xlat1.x * -2.0 + 3.0;
                    u_xlat1.x = u_xlat1.x * u_xlat1.x;
                    u_xlat1.x = u_xlat1.x * u_xlat16;
                    u_xlat4 = u_xlat16_3;
                } else {
                    u_xlat16_4.x = ((float)(0.0));
                    u_xlat16_4.y = ((float)(0.0));
                    u_xlat16_4.z = ((float)(0.0));
                    u_xlat16_4.w = ((float)(0.0));
                    u_xlat1.x = 0.0;
                    u_xlat4 = u_xlat16_4;
                }
                u_xlat3 = u_xlat4;
            } else {
                u_xlat16_3.x = ((float)(0.0));
                u_xlat16_3.y = ((float)(0.0));
                u_xlat16_3.z = ((float)(0.0));
                u_xlat16_3.w = ((float)(0.0));
                u_xlat1.x = 0.0;
                u_xlat3 = u_xlat16_3;
            }
            TempArray1[u_xlati17].x = u_xlat1.x;
            TempArray0[u_xlati17] = u_xlat3;
        }
        u_xlat16_1 = TempArray0[0];
        u_xlat2.x = TempArray1[0].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-_Color.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + _Color.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : _Color.xyz;
        u_xlat16_1 = TempArray0[1];
        u_xlat2.x = TempArray1[1].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-u_xlat16_8.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + u_xlat16_8.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : u_xlat16_8.xyz;
        u_xlat16_1 = TempArray0[2];
        u_xlat2.x = TempArray1[2].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-u_xlat16_8.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + u_xlat16_8.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : u_xlat16_8.xyz;
        u_xlat16_1 = TempArray0[3];
        u_xlat2.x = TempArray1[3].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-u_xlat16_8.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + u_xlat16_8.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : u_xlat16_8.xyz;
        u_xlat16_1 = TempArray0[4];
        u_xlat2.x = TempArray1[4].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-u_xlat16_8.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + u_xlat16_8.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : u_xlat16_8.xyz;
        u_xlat16_1 = TempArray0[5];
        u_xlat2.x = TempArray1[5].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-u_xlat16_8.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + u_xlat16_8.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : u_xlat16_8.xyz;
        u_xlat16_1 = TempArray0[6];
        u_xlat2.x = TempArray1[6].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-u_xlat16_8.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + u_xlat16_8.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : u_xlat16_8.xyz;
        u_xlat16_1 = TempArray0[7];
        u_xlat2.x = TempArray1[7].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-u_xlat16_8.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + u_xlat16_8.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : u_xlat16_8.xyz;
        u_xlat16_1 = TempArray0[8];
        u_xlat2.x = TempArray1[8].x;
        u_xlatb17 = 0.100000001<u_xlat16_1.w;
        u_xlatb32 = 0.0<u_xlat2.x;
        u_xlatb17 = u_xlatb32 && u_xlatb17;
        u_xlat2.x = u_xlat16_1.w * u_xlat2.x;
        u_xlat3.xyz = _QuestionColor.xyz * u_xlat16_1.xyz + (-u_xlat16_8.xyz);
        u_xlat2.xzw = u_xlat2.xxx * u_xlat3.xyz + u_xlat16_8.xyz;
        u_xlat16_8.xyz = (((bool)(u_xlatb17))) ? u_xlat2.xzw : u_xlat16_8.xyz;
        u_xlat2.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat2.x = rsqrt(u_xlat2.x);
        u_xlat2.xyz = u_xlat2.xxx * _WorldSpaceLightPos0.xyz;
        u_xlat2.x = dot(i.vs_TEXCOORD0.xyz, u_xlat2.xyz);
        u_xlat2.x = max(u_xlat2.x, 0.0);
        u_xlat2.x = (-u_xlat2.x) + 1.0;
        u_xlat2.x = u_xlat2.x * _ShadowColor.w;
        u_xlat17.xyz = (-u_xlat16_8.xyz) + _ShadowColor.xyz;
        u_xlat2.xyz = u_xlat2.xxx * u_xlat17.xyz + u_xlat16_8.xyz;
        u_xlatb47 = 0.0<_DissolveAmount;
        u_xlatb45 = u_xlat45<_DissolveWidth;
        u_xlat16_8.xyz = (((bool)(u_xlatb45))) ? _DissolveColor.xyz : float3(0.0, 0.0, 0.0);
        u_xlat16_8.xyz = (((bool)(u_xlatb47))) ? u_xlat16_8.xyz : float3(0.0, 0.0, 0.0);
        u_xlat3.z = (-_Glossiness) + 1.0;
        u_xlat16_53 = dot((-u_xlat0.xyz), i.vs_TEXCOORD0.xyz);
        u_xlat16_53 = u_xlat16_53 + u_xlat16_53;
        u_xlat16_9.xyz = i.vs_TEXCOORD0.xyz * (-((float3)(u_xlat16_53))) + (-u_xlat0.xyz);
        u_xlat1.xyz = i.vs_TEXCOORD0.xyz;
        u_xlat1.w = 1.0;
        u_xlat16_10.x = dot(unity_SHAr, u_xlat1);
        u_xlat16_10.y = dot(unity_SHAg, u_xlat1);
        u_xlat16_10.z = dot(unity_SHAb, u_xlat1);
        u_xlat16_10.xyz = u_xlat16_10.xyz + i.vs_TEXCOORD2.xyz;
        u_xlat16_10.xyz = max(u_xlat16_10.xyz, float3(0.0, 0.0, 0.0));
        u_xlat5.xyz = log2(u_xlat16_10.xyz);
        u_xlat5.xyz = u_xlat5.xyz * float3(0.416666657, 0.416666657, 0.416666657);
        u_xlat5.xyz = exp2(u_xlat5.xyz);
        u_xlat5.xyz = u_xlat5.xyz * float3(1.05499995, 1.05499995, 1.05499995) + float3(-0.0549999997, -0.0549999997, -0.0549999997);
        u_xlat5.xyz = max(u_xlat5.xyz, float3(0.0, 0.0, 0.0));
        u_xlat16_53 = (-u_xlat3.z) * 0.699999988 + 1.70000005;
        u_xlat16_53 = u_xlat3.z * u_xlat16_53;
        u_xlat16_53 = u_xlat16_53 * 6.0;
        u_xlat16_1 = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, u_xlat16_9.xyz, u_xlat16_53);
        u_xlat16_53 = u_xlat16_1.w + -1.0;
        u_xlat16_53 = unity_SpecCube0_HDR.w * u_xlat16_53 + 1.0;
        u_xlat16_53 = u_xlat16_53 * unity_SpecCube0_HDR.x;
        u_xlat16_9.xyz = u_xlat16_1.xyz * ((float3)(u_xlat16_53));
        u_xlat45 = dot(i.vs_TEXCOORD0.xyz, i.vs_TEXCOORD0.xyz);
        u_xlat45 = rsqrt(u_xlat45);
        u_xlat6.xyz = ((float3)(u_xlat45)) * i.vs_TEXCOORD0.xyz;
        u_xlat16_10.xyz = u_xlat2.xyz + float3(-0.220916301, -0.220916301, -0.220916301);
        u_xlat16_10.xyz = ((float3)(float3(_Metallic, _Metallic, _Metallic))) * u_xlat16_10.xyz + float3(0.220916301, 0.220916301, 0.220916301);
        u_xlat16_53 = (-_Metallic) * 0.779083729 + 0.779083729;
        u_xlat16_11.xyz = u_xlat2.xyz * ((float3)(u_xlat16_53));
        u_xlat45 = dot(u_xlat0.xyz, u_xlat6.xyz);
        u_xlat2.x = u_xlat45 + u_xlat45;
        u_xlat0.xyz = u_xlat6.xyz * (-u_xlat2.xxx) + u_xlat0.xyz;
        u_xlat2.x = dot(u_xlat6.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat2.x = clamp(u_xlat2.x, 0.0, 1.0);
        u_xlat45 = u_xlat45;
        u_xlat45 = clamp(u_xlat45, 0.0, 1.0);
        u_xlat0.x = dot(u_xlat0.xyz, _WorldSpaceLightPos0.xyz);
        u_xlat16_54 = (-u_xlat45) + 1.0;
        u_xlat0.x = u_xlat0.x * u_xlat0.x;
        u_xlat15 = u_xlat16_54 * u_xlat16_54;
        u_xlat15 = u_xlat16_54 * u_xlat15;
        u_xlat3.x = u_xlat0.x * u_xlat0.x;
        u_xlat0.x = u_xlat16_54 * u_xlat15;
        u_xlat16_53 = (-u_xlat16_53) + _Glossiness;
        u_xlat16_53 = u_xlat16_53 + 1.0;
        u_xlat16_53 = clamp(u_xlat16_53, 0.0, 1.0);
        u_xlat15 = tex2D(unity_NHxRoughness, (u_xlat3.xz).xy).x;
        u_xlat15 = u_xlat15 * 16.0;
        u_xlat16_12.xyz = ((float3)(u_xlat15)) * u_xlat16_10.xyz + u_xlat16_11.xyz;
        u_xlat16_13.xyz = u_xlat2.xxx * _LightColor0.xyz;
        u_xlat16_14.xyz = (-u_xlat16_10.xyz) + ((float3)(u_xlat16_53));
        u_xlat16_10.xyz = u_xlat0.xxx * u_xlat16_14.xyz + u_xlat16_10.xyz;
        u_xlat16_9.xyz = u_xlat16_9.xyz * u_xlat16_10.xyz;
        u_xlat16_9.xyz = u_xlat5.xyz * u_xlat16_11.xyz + u_xlat16_9.xyz;
        u_xlat16_9.xyz = u_xlat16_12.xyz * u_xlat16_13.xyz + u_xlat16_9.xyz;
        SV_Target0.xyz = u_xlat16_8.xyz + u_xlat16_9.xyz;
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
    Fallback "Standard"
}