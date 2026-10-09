Shader "Toony Colors Pro 2/Hybrid Shader 2 Directional Cube (Outline)"
{
    Properties
    {
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull ("Render Face", Float) = 2.0
        [ToggleUI] _ZWrite ("Depth Write", Float) = 1.0
        [Toggle(_ALPHATEST_ON)] _UseAlphaTest ("Alpha Clipping", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        
        [PerRendererData] _BaseColor ("Color", Color) = (1,1,1,1)
        _Color ("Color (Fallback)", Color) = (1,1,1,1)
        _BaseMap ("Albedo", 2D) = "white" {}
        _MainTex ("MainTex", 2D) = "white" {}

        // Toon Shading (Vibrant & Rich)
        [Header(Toon Shading)]
        _HColor ("Highlight Tint", Color) = (1,1,1,1)
        _SColor ("Shadow Tint", Color) = (0.82,0.80,0.86,1)
        _RampThreshold ("Ramp Threshold", Range(0.01, 1)) = 0.4
        _RampSmoothing ("Ramp Smoothing", Range(0.001, 1)) = 0.3

        // Specular
        [Header(Specular)]
        [Toggle(TCP2_SPECULAR)] _UseSpecular ("Enable Specular", Float) = 1.0
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularToonSize ("Specular Size", Range(0.001, 1)) = 0.18
        _SpecularToonSmoothness ("Specular Smoothness", Range(0.001, 1)) = 0.06

        // Outline
        [Header(Outline)]
        [ToggleUI] _UseOutline ("Enable Outline", Float) = 0.0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 5)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "PerformanceChecks"="False" }
        LOD 200
        Cull [_Cull]
        ZWrite [_ZWrite]

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local TCP2_SPECULAR

            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            sampler2D _MainTex;
            float4 _BaseMap_ST;
            float4 _MainTex_ST;

            half4 _Color;
            half4 _HColor;
            half4 _SColor;
            half _RampThreshold;
            half _RampSmoothing;

            half _UseSpecular;
            half4 _SpecularColor;
            half _SpecularToonSize;
            half _SpecularToonSmoothness;
            half _Cutoff;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                fixed4 tex = tex2D(_BaseMap, i.uv) * tex2D(_MainTex, i.uv);
                half4 instColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                half4 baseColor = (instColor.a > 0.001) ? instColor : _Color;
                fixed4 albedo = tex * baseColor;

                #ifdef _ALPHATEST_ON
                clip(albedo.a - _Cutoff);
                #endif

                float3 worldNormal = normalize(i.worldNormal);
                // Front-top camera-biased light (ensures even, bright, juicy illumination on both top and front faces)
                float3 lightDir = normalize(float3(0.12, 0.38, -0.92));
                float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float3 halfDir = normalize(lightDir + viewDir);

                // Smooth 2-tone Toon ramp
                half ndl = dot(worldNormal, lightDir) * 0.5 + 0.5;
                half ramp = smoothstep(_RampThreshold - _RampSmoothing * 0.5, _RampThreshold + _RampSmoothing * 0.5, ndl);
                
                // Tone multiplier: 0.82 on shadow underside, 1.05 on front and top faces (vibrant & pop!)
                half3 tone = lerp(_SColor.rgb, _HColor.rgb * 1.05, ramp);
                half3 finalColor = albedo.rgb * tone;

                // Subtle glossy shine at corner curves
                #ifdef TCP2_SPECULAR
                if (_UseSpecular > 0.5)
                {
                    half ndh = max(0.0, dot(worldNormal, halfDir));
                    half specSize = 1.0 - _SpecularToonSize;
                    half spec = smoothstep(specSize - _SpecularToonSmoothness, specSize + _SpecularToonSmoothness, ndh);
                    finalColor += _SpecularColor.rgb * spec * 0.2 * ramp;
                }
                #endif

                fixed4 col;
                col.rgb = finalColor;
                col.a = albedo.a;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}