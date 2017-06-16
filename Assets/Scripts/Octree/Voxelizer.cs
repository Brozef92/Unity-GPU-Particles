#if UNITY_EDITOR

using UnityEngine;
using UnityEditor; //for making handling assets/folders

using System.Collections;
using System.Collections.Generic; //used for List<T>
using System.Linq; //used for sorting lists
using System.IO; //for Read/Write of files

[ExecuteInEditMode]
public class Voxelizer : MonoBehaviour
{
    [Range(10, 50)]
    public int VoxelSize = 20; //How big the initial Root Cube is
    [Range(1, 5)]
    public int TreeDepth = 2;

    [HideInInspector]
    public Octree OCTREE = null;

    //read through the Tree and make a list out of them and then convert that list to an continuous ARRAY
    List<TreeNode> Unpacked_Tree = null;
    [HideInInspector]
    public SDF_Node[] nodeArray; 

    //This is used to compare each mesh because for Spheres it is better to use AABB rather than OBB
    public Mesh SphereExample;

    [HideInInspector]
    public bool Initialized = false;

    //used to find closes point to colliders
    Vector3[] RayDirs;

    public string Octree_Name;
    public int RayDirections = 100; //How many Rays to shoot outward from each node

    //=====================================================================================================================
    // This function creates the Octree and then saves it out as a Prefab
    //=====================================================================================================================
    public void BAKE_OCTREE()
    {
        Debug.Log("Baking Octree: " + Octree_Name + ".....");
        Initialized = false;

        Build_Octree(); //the Meat and the potatoes

        Debug.Log("Finished Baking Octree!!");

        //Now save the Octree 
        bool folderExists = AssetDatabase.IsValidFolder("Assets/Resources/Baked_Octrees/");
        if(!folderExists)
        {
            AssetDatabase.CreateFolder("Assets/Resources/", "Baked_Octrees");
        }

        string path = "Assets/Resources/Baked_Octrees/" + Octree_Name + ".txt";

        SaveTree(path);
    }

    //=====================================================================================================================
    //Deletes the currently named File
    //=====================================================================================================================
    public void DeleteOctree()
    {
        string path = "Assets/Resources/Baked_Octrees/" + Octree_Name + ".txt";

        bool removed = AssetDatabase.DeleteAsset(path);
        if (removed)
        {
            Debug.Log("Removed: " + path);
        }
        else
        {
            Debug.Log("Cannot Remove: " + path);
        }

    }
    //=====================================================================================================================
    //this Function saves the linear Node array as a text file
    //=====================================================================================================================
    void SaveTree(string path)
    {
        string file = "";

        //Start with the Tree Depth ,number of nodes; and Root Size
        file += TreeDepth.ToString() + "," + nodeArray.Length.ToString() + ";" + VoxelSize.ToString();

        for(int i = 0; i < nodeArray.Length; ++i)
        {
            SDF_Node n = nodeArray[i];
            file += "/" + n.index.ToString() + "," + n.c0.ToString() + "," + n.c1.ToString()
                 + "," + n.c2.ToString() + "," + n.c3.ToString() + "," + n.c4.ToString()
                 + "," + n.c5.ToString() + "," + n.c6.ToString() + "," + n.c7.ToString()
                 + "," + n.Min.x.ToString() + "," + n.Min.y.ToString() + "," + n.Min.z.ToString()
                 + "," + n.Max.x.ToString() + "," + n.Max.y.ToString() + "," + n.Max.z.ToString()
                 + "," + n.SurfacePoint.x.ToString() + "," + n.SurfacePoint.y.ToString() + "," + n.SurfacePoint.z.ToString();
        }
        //finally add the center of the Octree 
        Vector3 p = transform.position;
        file += '*' + p.x.ToString() + "," + p.y.ToString() + "," + p.z.ToString() + ",";

        //PlayerPrefs.SetString(Octree_Name, file);
        System.IO.File.WriteAllText(path, file);
    }

    //=====================================================================================================================
    // structs used in this class and maybe elsewhere
    //=====================================================================================================================

    //This is the format that the compute buffer will use
    public struct SDF_Node
    {
        public int index; //can't use pointers so this should work too
        public int c0, c1, c2, c3, c4, c5, c6, c7;

        public Vector3 Min, Max; //AABB of this node
        public Vector3 SurfacePoint; //closest surface point in this node

        public static int Stride
        {
            get
            {
                return 72;
            }
        }

        public override string ToString()
        {
            return index.ToString() + ", " + Min.ToString() + ", " + Max.ToString() + ", " + SurfacePoint.ToString();
        }
    }

    //this will be used to unpack the Tree into a reable List 
    public class TreeNode
    {
        public Octree.OctreeNode mParent;
        public Octree.OctreeNode mNode;
        public Octree.OctreeNode[] mChildren = null;
    }

    //simple struct used when looking for the closest collider to a point
    class ColliderDistance
    {
        public float distance;
        public int index;
    }

    //=====================================================================================================================
    //This initializes this Octree 
    //=====================================================================================================================
    void Build_Octree ()
    {
        RayDirs = PointsOnSphere.GetPoints((float)RayDirections);

        //First step all Scene geometry send their Meshes to the Octree class
        if(Octree.All_OBBs == null)
        {
            Octree.All_OBBs = new List<Octree.OBB>();
        }

        if(Unpacked_Tree == null)
        {
            Unpacked_Tree = new List<TreeNode>();
        }

        //send all scene Geometry(AABBs) to the Octree class
        StreamGeometry();

        //Create a new Octree only ONCE this is not a dynamic scene!!!!
        OCTREE = new Octree(transform.position, VoxelSize, TreeDepth);

        Octree.OctreeNode root = OCTREE.GetRootNode();
        
        //Now for each Leaf node at the bottom of the Tree populate it
        //ie. Find the closest Surface geometry to it and populate its SDF structure
        PopulateTree(root);

        //Structure the Octree into a single list of tree nodes
        UnpackTree(root);

        //Finaly Step: Store the UnpackedOctree into a linear Array(ComputeBuffer)
        PackTree();

        Initialized = true;
    }

    //=====================================================================================================================
    // Helper functions to Generate the Octree and then Pack it
    //=====================================================================================================================

    //send all scene Geometry(OBBs) to the Octree class
    void StreamGeometry()
    {
        MeshRenderer[] allMesh = FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];

        Octree.AABB volume = new Octree.AABB(); //this is the volume of the Octree Root node
        Vector3 p = transform.position;
        volume.Max.x = p.x + VoxelSize;
        volume.Max.y = p.y + VoxelSize;
        volume.Max.z = p.z + VoxelSize;
        volume.Min.x = p.x - VoxelSize;
        volume.Min.y = p.y - VoxelSize;
        volume.Min.z = p.z - VoxelSize;

        //All the AABBs
        for(int i = 0; i < allMesh.Length; ++i)
        {
            if (allMesh[i].enabled == false)
                continue;

            //world position bounds
            Octree.AABB meshAABB = new Octree.AABB();
            meshAABB.Max = allMesh[i].bounds.max;
            meshAABB.Min = allMesh[i].bounds.min;

            if(IsInOctree(meshAABB, volume))
            {
                //Now Because Unity uses AABBs for all its bounds we first have to construct our own OBBs
                Octree.OBB obb = new Octree.OBB();

                Vector3 c = allMesh[i].bounds.center;
                Mesh m = allMesh[i].gameObject.GetComponent<MeshFilter>().sharedMesh;

                obb.c = c;
                obb.e = m.bounds.extents;
                obb.s = allMesh[i].transform.localScale;

                //if it's a sphere use AABB instead
                if (m == SphereExample)
                {
                    
                    obb.x = c + Vector3.right;
                    obb.y = c + Vector3.up;
                    obb.z = c + Vector3.forward;
                    
                }
                else
                {
                    obb.x = c + allMesh[i].transform.right;
                    obb.y = c + allMesh[i].transform.up;
                    obb.z = c + allMesh[i].transform.forward;
                }

                Octree.All_OBBs.Add(obb);
            }
        }
    }

    bool IsInOctree(Octree.AABB A, Octree.AABB B)
    {
        //http://www.miguelcasillas.com/?p=30
        return (A.Max.x > B.Min.x &&
            A.Min.x < B.Max.x &&
            A.Max.y > B.Min.y &&
            A.Min.y < B.Max.y &&
            A.Max.z > B.Min.z &&
            A.Min.z < B.Max.z);
    }

    //Recursively search the tree and for each leaf node at the bottom depth populate it's
    //SDF structure by raycasting to all Scene Geometry and find the closest Surface Point
    void PopulateTree(Octree.OctreeNode node)
    {
        if (!node.IsLeaf())
        {
            var subNodes = node.Nodes;
            foreach (var subNode in subNodes)
            {
                PopulateTree(subNode);
            }
        }
        else
        {
            if(node.IsLowestNode())
            {
                //find the closest Surface Point to this Mesh
                //Unity 5.7 has Physics.ClosestPoint(Vec3 p) 

                //This is my version of it
                Vector3 surfacePoint = ClosesPointOnCollider(node.Position);
                if(surfacePoint == node.Position)
                {
                    node.InsideMesh = true;
                }
                else
                {
                    node.InsideMesh = false;
                    node.SDF.mPoint = surfacePoint;
                    node.SDF.mDistance = (surfacePoint - node.Position).magnitude;
                }
            }
        }
    }

    //recursively go through the Octree and convert them into a simple structure
    void UnpackTree(Octree.OctreeNode node)
    {
        TreeNode current = new TreeNode();

        var subNodes = node.Nodes;

        //set this up
        current.mParent = node.Parent;
        current.mNode = node;
        int i = 0;
        if (subNodes != null)
        {
            current.mChildren = new Octree.OctreeNode[8];
            foreach (var subNode in subNodes)
            {
                current.mChildren[i] = subNode;
                ++i;
            }
        }
        else
        {
            current.mChildren = null;
        }

        //assign this node to our array only if It's not inside a mesh
        if(!current.mNode.InsideMesh)
        {
            Unpacked_Tree.Add(current);
        }

        //if this Node has children recurse
        if (!node.IsLeaf())
        {
            foreach (var subNode in subNodes)
            {
                UnpackTree(subNode);
            }
        }
    }
    //Linearly search the Unpacked Tree to find this nodes index
    int FindIndex(Octree.OctreeNode node, ref List<TreeNode> tree)
    {
        int index = -1;

        for(index = 0; index < tree.Count; ++index)
        {
            if (tree[index].mNode == node)
                return index;
        }

        return -1;
    }

    //Final step Get the Unpacked tree and then pack it into a linear Buffer
    void PackTree()
    {
        //the unpacked Tree is recursively built so we have to turn it into a linear list here before we copy it with proper
        //indexing into the final LinearList below
        List<TreeNode> LinearUnpackedTree = new List<TreeNode>();
        for(int i = 0; i < Unpacked_Tree.Count-1; ++i) //ignore last node some reason the recursion fucks up when adding an extra garbage node
        {
            LinearUnpackedTree.Add(Unpacked_Tree[i]);
        }
        Unpacked_Tree.Clear();

        //Go through the unpacked tree and build it linearly
        List<SDF_Node> LinearList = new List<SDF_Node>();
        
        for(int i = 0; i < LinearUnpackedTree.Count; ++i)
        {
            SDF_Node current = new SDF_Node();

            Octree.OctreeNode node = LinearUnpackedTree[i].mNode;
            Vector3 p = node.Position;
            float s = node.SDF.mSize;

            current.Min.x = p.x - s;
            current.Min.y = p.y - s;
            current.Min.z = p.z - s;

            current.Max.x = p.x + s;
            current.Max.y = p.y + s;
            current.Max.z = p.z + s;

            current.index = i;
            current.SurfacePoint = node.SDF.mPoint;
            if (LinearUnpackedTree[i].mChildren != null)
            {
                current.c0 = FindIndex(LinearUnpackedTree[i].mChildren[0], ref LinearUnpackedTree);
                current.c1 = FindIndex(LinearUnpackedTree[i].mChildren[1], ref LinearUnpackedTree);
                current.c2 = FindIndex(LinearUnpackedTree[i].mChildren[2], ref LinearUnpackedTree);
                current.c3 = FindIndex(LinearUnpackedTree[i].mChildren[3], ref LinearUnpackedTree);
                current.c4 = FindIndex(LinearUnpackedTree[i].mChildren[4], ref LinearUnpackedTree);
                current.c5 = FindIndex(LinearUnpackedTree[i].mChildren[5], ref LinearUnpackedTree);
                current.c6 = FindIndex(LinearUnpackedTree[i].mChildren[6], ref LinearUnpackedTree);
                current.c7 = FindIndex(LinearUnpackedTree[i].mChildren[7], ref LinearUnpackedTree);
            }
            else
            {
                current.c0 = -1;
                current.c1 = -1;
                current.c2 = -1;
                current.c3 = -1;
                current.c4 = -1;
                current.c5 = -1;
                current.c6 = -1;
                current.c7 = -1;
            }

            LinearList.Add(current);
        }

        //used to set a ComputeBuffer later on
        nodeArray = LinearList.ToArray();
    }

    //=============================================================================================================================================
    //Helper Functions to Find the closest approximate point on a collider from a Vector3.. Note Unity 5.7 and up has this as Physics.ClosestPoint
    //=============================================================================================================================================
    Vector3 ClosesPointOnCollider(Vector3 p)
    {
        Vector3 closestPoint = p;

        Collider[] allColliders = GameObject.FindObjectsOfType<Collider>();

        //first find the closest collider to the Point

        List<ColliderDistance> distances = new List<ColliderDistance>();

        for(int c = 0; c < allColliders.Length; ++c)
        {
            Collider currentCollider = allColliders[c];
            ColliderDistance curr = new ColliderDistance();

            curr.distance = (p - currentCollider.transform.position).sqrMagnitude;
            curr.index = c;

            distances.Add(curr);
        }

        distances = distances.OrderBy(c => c.distance).ToList();

        //The closest Collider should be first in the list
        Collider closestCollider = allColliders[distances[0].index];
        if(closestCollider.GetType() == typeof(SphereCollider)) //this is much nicer for a sphere
        {
            RaycastHit hit;
            Ray r = new Ray();
            r.origin = p;
            r.direction = closestCollider.transform.position - p;
            if (closestCollider.Raycast(r, out hit, VoxelSize))
            {
                closestPoint = hit.point;
            }
        }
        else //Ray cast in out-ward directions
        {
            List<RaycastHit> hits = new List<RaycastHit>();
            //now from the point P raycast in a whole bunch of directions
            for (int r = 0; r < RayDirs.Length; ++r)
            {
                Ray ray = new Ray();
                ray.origin = p;
                ray.direction = RayDirs[r];
        
                RaycastHit hit;
                if (closestCollider.Raycast(ray, out hit, VoxelSize))
                {
                    hits.Add(hit);
                }
            }
        
            if (hits.Count > 1)
            {
                //find the closest hit
                hits = hits.OrderBy(h => (h.point - p).sqrMagnitude).ToList();
                closestPoint = hits[0].point;
            }
            else if (hits.Count == 1)
            {
                closestPoint = hits[0].point;
            }
        }

        return closestPoint;
    }

    //=============================================================================================================================================
    //Draw all the OBBs and the max extents of this Octree
    //=============================================================================================================================================
    void OnDrawGizmos()
    {
        if (Octree.All_OBBs != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < Octree.All_OBBs.Count; ++i)
            {
                Baked_Octree.Draw_OBB(Octree.All_OBBs[i]);
            }
        }

        //Draw the maximum extent of the Octree even if there is no octree
        Gizmos.color = Color.grey;
        Gizmos.DrawWireCube(transform.position, new Vector3(VoxelSize * 2, VoxelSize * 2, VoxelSize * 2));
    }
}

#endif