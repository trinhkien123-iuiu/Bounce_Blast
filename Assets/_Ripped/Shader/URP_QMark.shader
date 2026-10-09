Shader "URP/QMark" {
	Properties {
		_MainTex ("Question Mark Texture", 2D) = "white" {}
		_Color ("Background Color", Vector) = (0.5,0.5,0.5,1)
		_QuestionColor ("Question Mark Color", Vector) = (1,1,1,1)
		_ShadowColor ("Shadow Color", Vector) = (0.3,0.3,0.3,1)
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Speed ("Animation Speed", Range(0.1, 5)) = 1
		_Density ("Question Mark Density", Range(1, 20)) = 8
		_RotationVariance ("Rotation Variance", Range(0, 360)) = 180
		_Scale ("Question Mark Scale", Range(0.1, 1)) = 0.6
		_Direction ("Movement Direction", Vector) = (1,0,0,0)
		_Spacing ("Question Mark Spacing", Range(0.1, 0.9)) = 0.5
		_VerticalDissolve ("Vertical Dissolve", Range(0, 1)) = 0
		_DissolveYMin ("Dissolve Min Y (Object Space)", Float) = 0
		_DissolveYMax ("Dissolve Max Y (Object Space)", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
}