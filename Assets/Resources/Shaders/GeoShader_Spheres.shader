Shader "Custom/GeoShader_Spheres"
{
	Properties
	{
		_Color("Main Color", COLOR) = (1,1,1,1)
		_Size("Size", Range(0.1, 1)) = 0.5
	}	

	SubShader
	{
		Pass
		{
			//Tags{ "RenderType" = "Opaque" }
			Tags{ "LightMode" = "ForwardBase" }
			LOD 200

			ZWrite On 

			//Blend SrcAlpha OneMinusSrcAlpha // use alpha blending

			Cull Off

			CGPROGRAM
				#pragma target 5.0	
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				#pragma geometry GS_Main

				//Unity FOG
				#pragma multi_compile_fog
				//#pragma enable_d3d11_debug_symbols //For debugging only
				#include "UnityCG.cginc" 

				//==========================================================================================
				// Data structures
				//==========================================================================================
				struct GS_INPUT
				{
					float4	pos		: POSITION;
					bool active : TEXCOORD0;
				};

				struct FS_INPUT
				{
					float4	pos	: POSITION;
					float2 uv	: TEXCOORD0;
					UNITY_FOG_COORDS(1)
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
				// Geometry Shader: Make Icosahedron : https://schneide.wordpress.com/2016/07/15/generating-an-icosphere-in-c/
				//==========================================================================================
				[maxvertexcount(60)]
				void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					const float X = _Size * .525731112119133606f;
					const float Z = _Size * .850650808352039932f;
					const float N = 0.f;

					float4 v[12];
					//{-X, N, Z}, { X,N,Z }, { -X,N,-Z }, { X,N,-Z },
					v[0] = float4(float3(p[0].pos.x - X, p[0].pos.y + N, p[0].pos.z + Z), 1.0f);
					v[1] = float4(float3(p[0].pos.x + X, p[0].pos.y + N, p[0].pos.z + Z), 1.0f);
					v[2] = float4(float3(p[0].pos.x - X, p[0].pos.y + N, p[0].pos.z - Z), 1.0f);
					v[3] = float4(float3(p[0].pos.x + X, p[0].pos.y + N, p[0].pos.z - Z), 1.0f);

					//{ N,Z,X }, { N,Z,-X }, { N,-Z,X }, { N,-Z,-X },
					v[4] = float4(float3(p[0].pos.x + N, p[0].pos.y + Z, p[0].pos.z + X), 1.0f);
					v[5] = float4(float3(p[0].pos.x + N, p[0].pos.y + Z, p[0].pos.z - X), 1.0f);
					v[6] = float4(float3(p[0].pos.x + N, p[0].pos.y - Z, p[0].pos.z + X), 1.0f);
					v[7] = float4(float3(p[0].pos.x + N, p[0].pos.y - Z, p[0].pos.z - X), 1.0f);
					
					//{ Z,X,N }, { -Z,X, N }, { Z,-X,N }, { -Z,-X, N }
					v[8] = float4(float3(p[0].pos.x + Z, p[0].pos.y + X, p[0].pos.z + N), 1.0f);
					v[9] = float4(float3(p[0].pos.x - Z, p[0].pos.y + X, p[0].pos.z + N), 1.0f);
					v[10] = float4(float3(p[0].pos.x + Z, p[0].pos.y - X, p[0].pos.z + N), 1.0f);
					v[11] = float4(float3(p[0].pos.x - Z, p[0].pos.y - X, p[0].pos.z + N), 1.0f);

					//Create Quad only particle is ACTIVE
					if (p[0].active)
					{
						float4x4 vp = mul(UNITY_MATRIX_MVP, _World2Object); //put the Quad into View Space

						//Simple UVs for each triangle
						//   1
						//  / \
						// 0---2

						FS_INPUT pIn;
						//{0, 4, 1}, { 0,9,4 }, { 9,5,4 }, { 4,5,8 }, { 4,8,1 },
						
						pIn.pos = mul(vp, v[0]);//Triangle 1
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[4]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[1]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[0]);//Triangle 2
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[9]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[4]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[9]);//Triangle 3
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[5]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[4]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[4]);//Triangle 4
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[5]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[8]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[4]);//Triangle 5
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[8]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[1]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);

						//{ 8,10,1 }, { 8,3,10 }, { 5,3,8 }, { 5,2,3 }, { 2,7,3 },
						pIn.pos = mul(vp, v[8]);//Triangle 6
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[10]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[1]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[8]);//Triangle 7
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[3]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[10]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[5]);//Triangle 8
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[3]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[8]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[5]);//Triangle 9
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[2]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[3]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[2]);//Triangle 10
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[7]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[3]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);

						//{ 7,10,3 }, { 7,6,10 }, { 7,11,6 }, { 11,0,6 }, { 0,1,6 },
						pIn.pos = mul(vp, v[7]);//Triangle 11
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[10]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[3]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[7]);//Triangle 12
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[6]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[10]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[7]);//Triangle 13
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[11]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[6]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[11]);//Triangle 14
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[0]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[6]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[0]);//Triangle 15
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[1]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[6]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);

						//{ 6,1,10 }, { 9,0,11 }, { 9,11,2 }, { 9,2,5 }, { 7,2,11 }
						pIn.pos = mul(vp, v[6]);//Triangle 16
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[1]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[10]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[9]);//Triangle 17
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[0]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[11]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[9]);//Triangle 18
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[11]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[2]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[9]);//Triangle 19
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[2]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[5]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[7]);//Triangle 20
						pIn.uv = float2(0.1f, 0.1f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[2]);
						pIn.uv = float2(0.5f, 0.9f);
						triStream.Append(pIn);
						pIn.pos = mul(vp, v[11]);
						pIn.uv = float2(0.9f, 0.9f);
						triStream.Append(pIn);

						//used for applying FOG color
						UNITY_TRANSFER_FOG(pIn, pIn.pos);
					}
				}

				//==========================================================================================
				// Fragment Shader
				//==========================================================================================
				float4 FS_Main(FS_INPUT input) : SV_Target//: COLOR
				{
					float4 colour = float4(_Color.rgb, 1.0f);
					float4 second = colour * 0.05f;

					UNITY_APPLY_FOG(input.fogCoord, colour);
					
					//because this is Unlit add some "3D" to it by lerping colours
					float t = (input.uv.x + input.uv.y) * 0.5f;
					colour = lerp(colour, second, t);

					return colour;
				}

			ENDCG
		}
	}
}
