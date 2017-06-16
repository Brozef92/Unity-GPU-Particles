using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(Particle_Flowing))]
public class Voxel_Debugger : MonoBehaviour
{
    Baked_Octree VoxelGrid = null;
    public KeyCode DebugDrawKey = KeyCode.Space; //Toggle debug drawing entire tree
    public KeyCode DebugNormals = KeyCode.N; //Toggel drawing Surface Normals
    public KeyCode DebugNodes = KeyCode.M; //Toggel drawing Nodes

	// Use this for initialization
	void Start ()
    {
        VoxelGrid = GetComponent<Particle_Flowing>().octree;
	}

    void Update()
    {
        if(Input.GetKeyDown(DebugDrawKey))
        {
            VoxelGrid.DrawTree = !VoxelGrid.DrawTree;
        }

        if(Input.GetKeyDown(DebugNormals))
        {
            VoxelGrid.DrawNormals = !VoxelGrid.DrawNormals;
        }

        if (Input.GetKeyDown(DebugNodes))
        {
            VoxelGrid.DrawNodes = !VoxelGrid.DrawNodes;
        }
    }
	
    //Draw the Grid after this camera has drawn everything else
    void OnPostRender()
    {
        VoxelGrid.DrawVoxelGrid(Color.black);
    }
}
