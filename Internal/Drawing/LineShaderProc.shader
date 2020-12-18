Shader "LineShaderProc" {
	Properties
	{
	}
	SubShader
	{
		Pass
		{
			// Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

			//ZWrite off
			//ZTest Always
			//Cull off
			//Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 4.5

			#include "UnityCG.cginc"

			StructuredBuffer<float4> positionBuffer;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 color : TEXCOORD0;
			};

            float4 unpack(float i)
            {
                return float4(i / 262144.0, i / 4096.0, i / 64.0, i) % 64.0 / 63;
            }

			v2f vert(uint vid : SV_VertexID)
			{
				float4 pos = positionBuffer[vid];

				float col = pos.w;
				float4 worldPos = float4(pos.xyz, 1);
				float4 projectionPos = mul(UNITY_MATRIX_VP, worldPos);

				v2f o;
				o.pos = projectionPos;
				o.color = unpack(pos.w);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return i.color;
			}

			ENDCG
		}
	}
}
