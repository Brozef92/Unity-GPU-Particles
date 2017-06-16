using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices; //used for sizeof fucntion because C# can't safely use sizeof()

[RequireComponent(typeof(Baked_Octree))]
public class Particle_Flowing : MonoBehaviour
{
    //Solution to reading ComputeBuffer in a shader found at:http://answers.unity3d.com/questions/1235815/unity-shader-writing-and-reading-computebuffer-in.html

    //Integration http://gafferongames.com/game-physics/integration-basics/

    //Also using code from: https://github.com/hecomi/UnityPseudoInstancedGPUParticles/blob/master/Assets/Screen%20Space%20Collision%20GPU%20Particles/Scripts/PseudoInstancedGPUParticleManager.cs
    //                    : https://github.com/keijiro/KvantSpray


    const int JOB_SIZE = 256; // Number of particle to update concurrently...this MUST match compute kernel(s)!!!!

    //=============================================================================================================================================
    //Structs
    //=============================================================================================================================================
    public struct Particle
    {
        public float invMass;
        public float lifeTime;
        public Vector3 pos;
        public Vector3 vel;
        public Vector3 acc;
        public bool active;
    };

    //=============================================================================================================================================
    //Privates
    //=============================================================================================================================================

    //Kernel IDs of the Compute shader compiled functions, These are used to Dispatch the correct function(kernel)
    int mKernelID_Init, mKernelID_Emit, mKernelID_Update;

    ComputeBuffer CB_particleBuffer;
    ComputeBuffer CB_particlePool; //All the "dead" particles
    ComputeBuffer CB_particleCounterCopy; //used to Copy particlePool counter, it is on GPU memory so I think this is how you find out how many elements are in it
    ComputeBuffer CB_Octree; //A spare octree that approximates scene geometry
    int[] particleCounter; //used to get how many particles are inside the Append/Consume Buffers

    int mJobSlice; // How many particles can be worked on at a time
    
    Vector3 EmitterPoint = Vector3.zero;
    Vector4 EmitterDir = new Vector4(0.0f, 1.0f, 0.0f, 0.5f); //Normalized vector.xyz, spread.w

    bool isInitialized = false;
    bool isReleased = false;

    public Baked_Octree octree; //a pre-baked octree as a prefab
    public string Octree_File; //name of the file 

    //=============================================================================================================================================
    //Publics
    //=============================================================================================================================================
    public int particleCount = 100000;
    public float ParticleMass = 0.1f;
    public float particleLifeTime = 10.0f;
    [Range(5.0f, 50.0f)]
    public float particleFlowRate = 10.0f; //How many particles are emitted at once
    public float particleInitialSpeed = 4.0f; //initial velocity scalar
    [Range(0.0f, 1.0f)]
    public float particleBounciness = 0.95f;
    [Range(0.0f, 1.0f)]
    public float SpeedRandomness = 0.5f; //Viscous material maybe
    public Material material; // Material used to draw the Particle on screen. using special Geometry shader
    public ComputeShader computeShader;// Compute shader used to update the Particles.
    public GameObject Emitter; //where the particle are spawned
    public Vector3 EmitterSize = Vector3.one;
    [Range(0.05f, 0.3f)]
    public float EmitterSpread = 0.2f;
    public Vector3 Gravity = new Vector3(0.0f, -9.81f, 0.0f);

    public float CollisionThreshold = 1.5f;   

    //=============================================================================================================================================
    //Helper function to determine how many particles are in the ParticlePoolBuffer on the GPU
    //=============================================================================================================================================
    int GetParticlePoolSize()
    {
        CB_particleCounterCopy.SetData(particleCounter);
        ComputeBuffer.CopyCount(CB_particlePool, CB_particleCounterCopy, 0);
        CB_particleCounterCopy.GetData(particleCounter);
        return particleCounter[0];
    }

    //=============================================================================================================================================
    //Helper function used to clean up Compute buffers
    //=============================================================================================================================================
    void ReleaseBuffers()
    {
        //I think these are COM objects so you have to release them when finished
        if(CB_Octree != null)
        {
            CB_Octree.Release();
        }
        if (CB_particleBuffer != null)
        {
            CB_particleBuffer.Release();
        }
        if (CB_particlePool != null)
        {
            CB_particlePool.Release();
        }
        if (CB_particleCounterCopy != null)
        {
            CB_particleCounterCopy.Release();
        }

        isReleased = true;
    }

    //=============================================================================================================================================
    //Helper function used to initialize the Compute Shader
    //=============================================================================================================================================
    void Initialize_ComputeShader ()
    {
        // Calculate the number of warp needed to handle all the particles
        if (particleCount <= 0)
        {
            particleCount = 10000;
        }

        mJobSlice = Mathf.CeilToInt((float)particleCount / JOB_SIZE);

        if (Emitter)
        {
            EmitterPoint = Emitter.transform.position;
            EmitterPoint.y -= Emitter.transform.localScale.y;
            EmitterDir = new Vector4(Emitter.transform.up.x, Emitter.transform.up.y, Emitter.transform.up.z, EmitterSpread);
        }
        else
        {
            EmitterDir.w = EmitterSpread;
        }

        //Now load a pre-baked Octree from a file
        octree.FileName = Octree_File;
        octree.Load_Tree();

        if(octree.Initialized == false)
        {
            Debug.Log("Failed to Load " + "Assets/Resources/Baked_Octrees/" + Octree_File + ".txt");
            return;
        }

        CB_Octree = new ComputeBuffer(octree.nodeArray.Length, Voxelizer.SDF_Node.Stride, ComputeBufferType.Default);
        CB_Octree.SetData(octree.nodeArray);

        // Create the ComputeBuffers holding the Particles on the GPU
        CB_particleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
        CB_particlePool = new ComputeBuffer(particleCount, sizeof(int), ComputeBufferType.Append); //Buffer is like a Stack for Appending/Consuming things on the GPU
        //CB_particlePool.SetCounterValue(0); //This shouldn't even be necessary???
        CB_particleCounterCopy = new ComputeBuffer(4, sizeof(int), ComputeBufferType.DrawIndirect); //.IndirectArguments for Unity 5.4 and up
        particleCounter = new int[] { 0, 1, 0, 0 };

        //Set Compute shader varriables..if you want to dynamically change these then you have to set these in FixedUpdate every frame
        computeShader.SetVector("_EmitterPos", EmitterPoint); //after particle is "dead" the computeShader will re-emit it
        computeShader.SetVector("_EmitterSize", EmitterSize); 
        computeShader.SetVector("_EmitterDirection", EmitterDir);
        computeShader.SetFloat("ParticleLifeTime", particleLifeTime);
        computeShader.SetFloat("InvParticleMass", ParticleMass);
        computeShader.SetFloat("_ParticleSpeed", particleInitialSpeed);
        computeShader.SetFloat("_SpeedRandomness", SpeedRandomness);
        computeShader.SetFloat("_ParticleBounce", particleBounciness);
        computeShader.SetVector("_Gravity", Gravity);
        computeShader.SetFloat("CollisionThreshold", CollisionThreshold);

        float particleRadius = material.GetFloat("_Size");
        computeShader.SetFloat("_ParticleRadius", particleRadius);

        //Set the SDF octree that was made earlier
        if (CB_Octree != null)
        {
            int updateKernel = computeShader.FindKernel("CS_Update");
            
            computeShader.SetInt("Octree_Size", CB_Octree.count);
            computeShader.SetInt("Octree_Depth", octree.TreeDepth);
            computeShader.SetBuffer(updateKernel, "SDF_Buffer", CB_Octree);

            Debug.Log("OCTREE SIZE = " + Voxelizer.SDF_Node.Stride * CB_Octree.count + " BYTES");
            Debug.Log("OCTREE DEPTH = " + octree.TreeDepth);
        }
        else
        {
            computeShader.SetInt("Octree_Size", 0);
            computeShader.SetInt("Octree_Depth", 0);
        }

        isInitialized = true;

        //Dispatch the Initialize kernel on the GPU this will initialize all our particles 
        Dispatch_Init();
    }

    //=============================================================================================================================================
    //Called when this script is Enabled/Disabled or Object is destroyed
    //=============================================================================================================================================
    void OnEnabled()
    {
        if(!isInitialized)
        {
            Debug.Log("OnEnabled.Initialzing");
            Initialize_ComputeShader();
        }
    }

    void OnDisable()
    {
        if(!isReleased)
        {
            Debug.Log("OnDisable.Releasing");
            ReleaseBuffers();
            isInitialized = false;
        }   
    }

    void OnDestroy()
    {
        if(!isReleased)
        {
            Debug.Log("OnDestroy.Releasing");
            ReleaseBuffers();
            isInitialized = false;
        }
    }

    //=============================================================================================================================================
    //All Dispatch functions to run our Compute Shader functions(kernels)
    //=============================================================================================================================================
    void Dispatch_Init()
    {
        mKernelID_Init = computeShader.FindKernel("CS_Initialize");
        //Debug.Log("Kernel_Init: " + mKernelID_Init.ToString());

        computeShader.SetBuffer(mKernelID_Init, "particleBuffer", CB_particleBuffer);
        computeShader.SetBuffer(mKernelID_Init, "deadParticles", CB_particlePool);
        computeShader.Dispatch(mKernelID_Init, mJobSlice, 1, 1); //Launch!!
    }

    void Dispatch_Emit(int ParticleGroup)
    {
        mKernelID_Emit = computeShader.FindKernel("CS_Emit");
        //Debug.Log("Kernel_Emit: " + mKernelID_Emit.ToString());

        computeShader.SetBuffer(mKernelID_Emit, "particleBuffer", CB_particleBuffer);
        computeShader.SetBuffer(mKernelID_Emit, "particlePool", CB_particlePool);
        computeShader.SetFloat("deltaTime", Time.deltaTime);

        //Find out how many particles to emit
        int numParticles = GetParticlePoolSize();
        int jobSlice = Mathf.Min(ParticleGroup, numParticles / JOB_SIZE);

        if(jobSlice > 0)
        {
            computeShader.Dispatch(mKernelID_Emit, jobSlice, 1, 1); //Launch!!
        }
    }

    void Dispatch_Update()
    {
        mKernelID_Update = computeShader.FindKernel("CS_Update");
        //Debug.Log("Kernel_Update: " + mKernelID_Update.ToString());

        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetBuffer(mKernelID_Update, "particleBuffer", CB_particleBuffer);
        computeShader.SetBuffer(mKernelID_Update, "deadParticles", CB_particlePool);
        computeShader.Dispatch(mKernelID_Update, mJobSlice, 1, 1); //Launch!! mJobSlice
    }

    //=============================================================================================================================================
    //Mono Functions
    //=============================================================================================================================================
    void Start()
    {
        if(!isInitialized)
        {
            Debug.Log("Start.initializing");
            Initialize_ComputeShader();
        }
        else
        {
            Debug.Log("Start.Reinitializing");
            ReleaseBuffers();
            Initialize_ComputeShader();
        }
    }

    void FixedUpdate()
    {
        //Dispatch both kernels in the compute shader

        if(isInitialized)
        {
            if (Emitter.activeSelf)
            {
                int particleCount = Mathf.CeilToInt(particleFlowRate);
                Dispatch_Emit(particleCount);
            }

            Dispatch_Update();
        }
    }

    //=============================================================================================================================================
    //OnRenderObject is called AFTER camera has rendered the scene.
    //=============================================================================================================================================
    void OnRenderObject() 
    {
        if (Camera.current == Camera.main)
        {
            material.SetBuffer("_ParticleBuffer", CB_particleBuffer);

            //Debug.Log("Rendering Geometry!!!");
            material.SetPass(0);

            //Draws mesh on the GPU in a geometry shader
            //Draw Points(In the Geo Shader it will turn them to Quads), vertex Count = 1, and Instance count = NumParticles
            Graphics.DrawProcedural(MeshTopology.Points, particleCount);
        }
    }

    //Optionally Draw Tree in the Editor
    void OnDrawGizmos()
    {
        if(octree)
        {
            octree.DrawVoxelGrid(Color.black);
        }
    }
}
