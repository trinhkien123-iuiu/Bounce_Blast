Shader "FishJam/Mosaic Tile" {
	Properties {
		_Color ("Palette colour", Vector) = (1,1,1,1)
		_SheenStrength ("Soft light sheen", Range(0, 0.3)) = 0.2
		_FacetContrast ("Facet contrast", Range(0, 1)) = 0.78
		[HideInInspector] _SrcBlend ("Source blend", Float) = 1
		[HideInInspector] _DstBlend ("Destination blend", Float) = 0
		[HideInInspector] _ZWrite ("Depth write", Float) = 1
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

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
}