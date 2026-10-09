Shader "ELCAPITAN/IceBlockerWorld" {
	Properties {
		[Header(Base Ice)] _IceBaseColor ("Color", Vector) = (0.04,0.72,1,0.99)
		[Header(Global Stone Texture)] _GlobalStoneTex ("Global Stone Texture", 2D) = "white" {}
		_GlobalStoneStrength ("Stone Texture Strength", Range(0, 1)) = 1
		[Header(Rim Light)] _RimStrength ("Rim Strength", Range(0, 2)) = 1
		_RimPower ("Rim Power", Range(0.25, 8)) = 2
		[Header(Rendering)] [MaterialEnum(Off,0,On,1)] _ZWrite ("Depth Writing", Float) = 1
		[Enum(Front,2,Back,1,Both,0)] _Cull ("Render Face", Float) = 2
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
}