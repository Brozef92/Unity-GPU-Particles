# Unity-GPU-Particles
Using Direct X compute shaders to emit and update up to 5,000,000 particles simultaneously. Particle-Mesh collision detection is done using a pre-baked octree. 


To use simply arrange the mesh as you wish and click Bake Octree.
For maximum tree depth it may take between 1-2 minutes to complete.

Collision accuracy really depends on number of nodes and tree depth, but the octree creation can take extremely
long beyond a depth of 5.
