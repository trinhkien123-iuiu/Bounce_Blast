// Nền tối phủ màn có LỖ SÁNG hình chữ nhật bo góc (hướng dẫn onboarding). Toạ độ theo pixel màn hình.
// _Center: tâm lỗ (pixel), _Half: nửa rộng/cao (pixel), _Round: bán kính bo (pixel; = nửa cạnh ngắn thì thành hình tròn), _Soft: độ mờ viền.
Shader "BounceBlast/UI/Spotlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,0.7)
        _ScreenSize ("Screen Size", Vector) = (1080,1920,0,0)
        _Center ("Center", Vector) = (540,960,0,0)
        _Half ("Half Size", Vector) = (100,100,0,0)
        _Round ("Round", Float) = 100
        _Soft ("Soft", Float) = 24
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            float4 _ScreenSize, _Center, _Half;
            float _Round, _Soft;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv * _ScreenSize.xy - _Center.xy;
                float r = min(_Round, min(_Half.x, _Half.y));
                float2 q = abs(p) - _Half.xy + r;
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;   // khoảng cách có dấu tới hộp bo góc
                float a = smoothstep(0.0, max(_Soft, 1.0), d);
                return fixed4(_Color.rgb, _Color.a * a);
            }
            ENDCG
        }
    }
}
