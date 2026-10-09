// Port Built-in RP cua "Toony Colors Pro 2 / Hybrid Shader 2" theo GLSL rip tu ban build Bounce Blast 3.7.6.
// Chi cai dat cac tinh nang ma material cua game dang bat:
//   ramp mac dinh, TCP2_SHADOW_LIGHT_COLOR, TCP2_SPECULAR (GGX), TCP2_RIM_LIGHTING (+ LIGHTMASK),
//   _NORMALMAP, _ALPHATEST_ON, _ALPHAPREMULTIPLY_ON, _RECEIVE_SHADOWS_OFF, TCP2_MOBILE,
//   cube shade offset (TCP2_CUBE_SHADE) va pass outline (TCP2_OUTLINE_PASS).
#ifndef TCP2_HYBRID_PORT_INCLUDED
#define TCP2_HYBRID_PORT_INCLUDED

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

sampler2D _BaseMap;
float4 _BaseMap_ST;
half4 _BaseColor;
half _Cutoff;
half4 _HColor;
half4 _SColor;
half _RampThreshold;
half _RampSmoothing;
half _IndirectIntensity;
half _SingleIndirectColor;
half4 _SpecularColor;
half _SpecularRoughness;
half _SpecularMapType;
sampler2D _SpecGlossMap;
half4 _RimColor;
half _RimMin;
half _RimMax;
sampler2D _BumpMap;
float4 _BumpMap_ST;
half _BumpScale;
half _CubeShadeOffset;
half _UseBakedCubeShadeOffset;
half4 _OutlineColor;
half _OutlineWidth;

struct TCP2_Attributes
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    half4 color : COLOR;
    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct TCP2_Varyings
{
    float4 pos : SV_POSITION;
    float3 normal : NORMAL;
    half4 color : COLOR;
    float3 worldPos : TEXCOORD0;
    float4 uv : TEXCOORD1;          // xy = _BaseMap uv, zw = uv goc
#if defined(_NORMALMAP)
    float3 tangent : TEXCOORD2;
    float3 bitangent : TEXCOORD3;
#endif
    half rim : TEXCOORD4;           // TCP2_MOBILE: rim tinh o vertex
    UNITY_LIGHTING_COORDS(5, 6)
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// SH bac 0-1 (giong code goc: chi dung unity_SHAr/g/b), tu doi sang gamma khi project dung Gamma
half3 TCP2_ShadeSHL1(half3 n)
{
    half3 sh = max(SHEvalLinearL0L1(half4(n, 1.0)), 0.0);
#ifdef UNITY_COLORSPACE_GAMMA
    sh = LinearToGammaSpace(sh);
#endif
    return sh;
}

TCP2_Varyings TCP2_Vert(TCP2_Attributes v)
{
    TCP2_Varyings o;
    UNITY_INITIALIZE_OUTPUT(TCP2_Varyings, o);
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.pos = UnityWorldToClipPos(worldPos);
    o.worldPos = worldPos;
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.color = v.color;
    o.uv.xy = TRANSFORM_TEX(v.texcoord.xy, _BaseMap);
    o.uv.zw = v.texcoord.xy;
#if defined(_NORMALMAP)
    o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
    o.bitangent = cross(o.normal, o.tangent) * (v.tangent.w * unity_WorldTransformParams.w);
#endif
#if defined(TCP2_RIM_LIGHTING) && defined(TCP2_MOBILE)
    float3 viewDir = _WorldSpaceCameraPos - worldPos;
    viewDir *= rsqrt(max(dot(viewDir, viewDir), 0.001));
    o.rim = 1.0 - max(dot(viewDir, o.normal), 0.0);
#endif
    UNITY_TRANSFER_LIGHTING(o, v.texcoord1.xy);
    return o;
}

half TCP2_Ramp(half ndl)
{
    half lo = _RampThreshold - _RampSmoothing * 0.5;
    half hi = _RampThreshold + _RampSmoothing * 0.5;
    return smoothstep(lo, hi, ndl * 0.5 + 0.5);
}

half TCP2_SpecularMask(half4 albedoTex, float2 uv)
{
#if defined(TCP2_MOBILE)
    return tex2D(_SpecGlossMap, uv).a;
#else
    half4 specTex = tex2D(_SpecGlossMap, uv);
    half mask = 1.0;
    if (_SpecularMapType >= 5.0) mask = specTex.a;
    else if (_SpecularMapType >= 4.0) mask = specTex.b;
    else if (_SpecularMapType >= 3.0) mask = specTex.g;
    else if (_SpecularMapType >= 2.0) mask = specTex.r;
    else if (_SpecularMapType >= 1.0) mask = albedoTex.a;
    return mask;
#endif
}

// GGX "TCP2" nhu ban goc: sqrt(D * PI/20) * (1 - 0.28 * a * r)
half TCP2_SpecularGGX(half3 normal, half3 halfDir, half roughness)
{
    half r = max(_SpecularRoughness, 1e-5) * roughness;
    half a = r * r;
    half a2 = min(a, 1.0);
    a2 *= a2;
    half nh = saturate(dot(normal, halfDir));
    half d = nh * (nh * a2 - nh) + 1.0;
    half D = (a2 * UNITY_INV_PI) / (d * d + 1e-4);
    half spec = sqrt(max(D * (UNITY_PI / 20.0), 1e-4)) * (1.0 - 0.28 * a * r);
    return max(spec, 0.0);
}

half3 TCP2_CubeShade(half3 color, half vertexAlpha)
{
    float c = floor(vertexAlpha * 255.0 + 0.5);
    float baked = (c <= 0.0 || c >= 255.0) ? 0.0 : (c - 1.0) * 0.00197628466 - 0.25;
    float offset = (_UseBakedCubeShadeOffset > 0.5 ? baked : 0.0) + _CubeShadeOffset;
    offset = clamp(offset, -0.25, 0.25);
    return lerp(color, (offset < 0.0 ? 1.0 : 0.0).xxx, abs(offset));
}

half4 TCP2_Frag(TCP2_Varyings i, half facing, bool addPass)
{
    UNITY_SETUP_INSTANCE_ID(i);

    float3 viewDir = _WorldSpaceCameraPos - i.worldPos;
    viewDir *= rsqrt(max(dot(viewDir, viewDir), 0.001));

    half3 normal = normalize(i.normal) * (facing > 0 ? 1.0 : -1.0);
#if defined(_NORMALMAP)
    half3 nTS = UnpackScaleNormal(tex2D(_BumpMap, i.uv.zw * _BumpMap_ST.xy + _BumpMap_ST.zw), _BumpScale);
    normal = normalize(nTS.x * i.tangent + nTS.y * i.bitangent + nTS.z * normal);
#endif

    half4 albedoTex = tex2D(_BaseMap, i.uv.xy);
    half4 albedo = albedoTex * _BaseColor;
#if defined(_ALPHATEST_ON)
    clip(albedo.a - _Cutoff);
#endif

    float3 lightDir = _WorldSpaceLightPos0.xyz - i.worldPos * _WorldSpaceLightPos0.w;
    lightDir *= rsqrt(dot(lightDir, lightDir));

#if !defined(TCP2_FORWARD_ADD) && defined(_RECEIVE_SHADOWS_OFF)
    half atten = 1.0;
#else
    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
#endif

    half ndl = dot(normal, lightDir);
    half ramp = TCP2_Ramp(ndl) * atten;

    half3 color;
#if defined(TCP2_FORWARD_ADD)
    color = albedo.rgb * ramp * _HColor.rgb * _LightColor0.rgb;
#else
  #if defined(TCP2_SHADOW_LIGHT_COLOR)
    half3 rampColor = lerp(_SColor.rgb, _HColor.rgb, ramp) * _LightColor0.rgb;
  #else
    half3 rampColor = lerp(_SColor.rgb, _HColor.rgb * _LightColor0.rgb, ramp);
  #endif
    color = albedo.rgb * rampColor;

    if (_IndirectIntensity > 0.0)
    {
        half3 shNormal = _SingleIndirectColor > 0.0 ? (half3)viewDir : normal;
        color += albedo.rgb * TCP2_ShadeSHL1(shNormal) * _IndirectIntensity;
    }
#endif

#if defined(TCP2_SPECULAR)
    float3 halfDir = lightDir + viewDir;
    halfDir *= rsqrt(max(dot(halfDir, halfDir), 0.001));
    half spec = TCP2_SpecularGGX(normal, halfDir, TCP2_SpecularMask(albedoTex, i.uv.xy));
    color += spec * saturate(ndl) * atten * _LightColor0.rgb * _SpecularColor.rgb;
#endif

#if defined(TCP2_RIM_LIGHTING) && !defined(TCP2_FORWARD_ADD)
  #if defined(TCP2_MOBILE)
    half rimTerm = i.rim;
  #else
    half rimTerm = 1.0 - max(dot((half3)viewDir, normal), 0.0);
  #endif
    half rim = smoothstep(_RimMin, _RimMax, rimTerm);
  #if defined(TCP2_RIM_LIGHTING_LIGHTMASK)
    color += rim * ramp * _LightColor0.rgb * _RimColor.rgb;
  #else
    color += rim * _RimColor.rgb;
  #endif
#endif

#if defined(TCP2_CUBE_SHADE) && !defined(TCP2_FORWARD_ADD)
    color = TCP2_CubeShade(color, i.color.a);
#endif

    half alpha = 1.0;
#if defined(_ALPHAPREMULTIPLY_ON)
    color *= albedo.a;
    alpha = albedo.a;
#elif defined(_ALPHABLEND_ON)
    alpha = albedo.a;
#endif
    return half4(color, alpha);
}

half4 TCP2_FragBase(TCP2_Varyings i, half facing : VFACE) : SV_Target
{
    return TCP2_Frag(i, facing, false);
}

// ---------------- Outline (Directional Cube (Outline)) ----------------
struct TCP2_OutlineVaryings
{
    float4 pos : SV_POSITION;
    half4 color : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

// Day dinh theo huong tam-object -> dinh tren man hinh (fallback: normal chieu len man hinh)
TCP2_OutlineVaryings TCP2_OutlineVert(TCP2_Attributes v)
{
    TCP2_OutlineVaryings o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);
    float2 dir = mul((float3x3)UNITY_MATRIX_VP, worldNormal).xy;
    float len2 = dot(dir, dir);
    dir = len2 > 1e-8 ? dir * rsqrt(len2) : float2(0.0, 1.0);

    float4 centerClip = mul(UNITY_MATRIX_VP, float4(unity_ObjectToWorld._m03_m13_m23, 1.0));
    float2 centerNdc = centerClip.xy / max(centerClip.w, 1e-5);

    float4 clipPos = UnityObjectToClipPos(v.vertex);
    float2 posNdc = clipPos.xy / max(clipPos.w, 1e-5);
    float2 fromCenter = posNdc - centerNdc;
    float lenC = dot(fromCenter, fromCenter);
    dir = lenC > 1e-8 ? fromCenter * rsqrt(lenC) : dir;

    clipPos.xy += dir * (clipPos.w * _OutlineWidth) / (_ScreenParams.xy * 0.5);
    o.pos = clipPos;
    o.color = _OutlineColor;
    return o;
}

half4 TCP2_OutlineFrag(TCP2_OutlineVaryings i) : SV_Target
{
    return i.color;
}

#endif
