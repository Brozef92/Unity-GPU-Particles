Shader "Custom/GeoShader_Quads" 
{
	Properties 
	{
		_Color("Main Color", COLOR) = (1,1,1,1)
		_SpriteTex ("Base (RGB)", 2D) = "white" {}
		_Size ("Size", Range(0.01, 3)) = 0.5
	}

	SubShader 
	{
		Pass
		{
			Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
			LOD 200

			ZWrite Off // don't write to depth buffer in order to not to occlude other particles

			Blend SrcAlpha OneMinusSrcAlpha // use alpha blending

			Cull Back//Off
		
			CGPROGRAM
				#pragma target 5.0
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				#pragma geometry GS_Main
				//#pragma enable_d3d11_debug_symbols //For debugging only
				#include "UnityCG.cginc" 

				//==========================================================================================
				// Data structures
				//==========================================================================================
				struct GS_INPUT
				{
					float4	pos		: POSITION;
					bool active		: TEXCOORD0;
				};

				struct FS_INPUT
				{
					float4	pos		: POSITION;
					float2  tex0	: TEXCOORD0;
				};

				// Particle's data
				#include "./GPUParticle.cginc"

				//==========================================================================================
				// Varriables
				//==========================================================================================

				float _Size;
				float4x4 _VP;
				Texture2D _SpriteTex;
				SamplerState sampler_SpriteTex;

				StructuredBuffer<Particle> _ParticleBuffer;

				fixed4 _Color;

				//==========================================================================================
				// Shader Programs
				//==========================================================================================

				//==========================================================================================
				// Vertex Shader
				//==========================================================================================
				GS_INPUT VS_Main(uint id : SV_VertexID) //appdata_base v
				{
					//https://gamedev.stackexchange.com/questions/139378/unity-simple-pass-through-geometry-shader
					GS_INPUT output = (GS_INPUT)0;

					//Put this vertex into World space from it's local space
					float4 vPos = float4(_ParticleBuffer[id].position, 1.0f);
					output.pos = mul(_Object2World, vPos);

					//Check if particle is active or not
					if (_ParticleBuffer[id].active)
					{
						output.active = true;
					}
					else
					{
						output.active = false;
					}

					return output;
				}

				//==========================================================================================
				// Geometry Shader
				//==========================================================================================
				[maxvertexcount(4)]
				void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					float3 look = _WorldSpaceCameraPos - p[0].pos;
					look = normalize(look);

					//use camera's matrix
					float3 up = UNITY_MATRIX_IT_MV[1].xyz;
					
					float3 right = cross(up, look);
					
					float halfS = 0.5f * _Size;
							
					float4 v[4];
					v[0] = float4(p[0].pos + halfS * right - halfS * up, 1.0f);
					v[1] = float4(p[0].pos + halfS * right + halfS * up, 1.0f);
					v[2] = float4(p[0].pos - halfS * right - halfS * up, 1.0f);
					v[3] = float4(p[0].pos - halfS * right + halfS * up, 1.0f);

					//Create Quad only particle is ACTIVE
					if (p[0].active)
					{
						float4x4 vp = mul(UNITY_MATRIX_MVP, _World2Object); //put the Quad into View Space
						FS_INPUT pIn;
						pIn.pos = mul(vp, v[0]);
						pIn.tex0 = float2(1.0f, 0.0f);
						triStream.Append(pIn);

						pIn.pos = mul(vp, v[1]);
						pIn.tex0 = float2(1.0f, 1.0f);
						triStream.Append(pIn);

						pIn.pos = mul(vp, v[2]);
						pIn.tex0 = float2(0.0f, 0.0f);
						triStream.Append(pIn);

						pIn.pos = mul(vp, v[3]);
						pIn.tex0 = float2(0.0f, 1.0f);
						triStream.Append(pIn);
					}	
				}

				//==========================================================================================
				// Fragment Shader
				//==========================================================================================
				float4 FS_Main(FS_INPUT input) : COLOR
				{
					float4 mainTexture = _SpriteTex.Sample(sampler_SpriteTex, input.tex0);
					float4 colour = float4(_Color.rgb, 1.0f);
					return colour * mainTexture;
				}

			ENDCG
		}
	} 
}
