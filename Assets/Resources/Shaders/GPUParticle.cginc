#ifndef _PARTICLE_INCLUDED_
#define _PARTICLE_INCLUDED_

struct Particle //Stride = 48
{
	float lifeTime;
	float invMass;
	float3 position;
	float3 velocity;
	float3 acceleration;
	bool active;
};

struct SDF //Stride = 72 Bytes
{
	int index;
	int children[8];
	float3 Min; //AABB of this node
	float3 Max; 
	float3 Point; //closest surface point
};

struct AABB 
{
	float3 Min;
	float3 Max; //Min/Max corners for this AABB
};

#endif // _PARTICLE_INCLUDED_
