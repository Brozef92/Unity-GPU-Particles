using UnityEngine;
using System.Collections;

//For some reason all Bounds in Unity are done using AABB which is stupid as fuck
//But luckily Box Colliders are OBBs

    [ExecuteInEditMode]
public class OBB_Bounds : MonoBehaviour
{
    MeshRenderer meshRenderer = null;
    MeshFilter meshFilter = null;
    Octree.OBB obb;

    static Octree.AABB aabb = null;
    static float size = 5.0f;

    // Use this for initialization
    void Awake ()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        obb = new Octree.OBB();

        if(aabb == null)
        {
            aabb = new Octree.AABB();
            aabb.Min = new Vector3(-size, -size, -size);
            aabb.Max = new Vector3(size, size, size);
        }
    }

    void Update()
    {
        Vector3 c = meshRenderer.bounds.center;
        Mesh m = meshFilter.sharedMesh;

        if(obb != null)
        {
            obb.c = c;
            obb.x = c + transform.right;
            obb.y = c + transform.up;
            obb.z = c + transform.forward;
            obb.e = m.bounds.extents;
            obb.s = transform.localScale;
        }
    }

    //Helper function to draw OBB using Gizmos class
    void Draw_OBB(Color colour)
    {
        Vector3[] Positions = new Vector3[8];
        Vector3 e = new Vector3(obb.e.x * obb.s.x, obb.e.y * obb.s.y, obb.e.z * obb.s.z);

        Positions[0] = obb.c + (((obb.x - obb.c) * e.x) + ((obb.y - obb.c) * e.y) + ((obb.z - obb.c) * e.z));
        Positions[1] = obb.c + (((obb.c - obb.x) * e.x) + ((obb.y - obb.c) * e.y) + ((obb.z - obb.c) * e.z));
        Positions[2] = obb.c + (((obb.x - obb.c) * e.x) + ((obb.c - obb.y) * e.y) + ((obb.z - obb.c) * e.z));
        Positions[3] = obb.c + (((obb.c - obb.x) * e.x) + ((obb.c - obb.y) * e.y) + ((obb.z - obb.c) * e.z));
        Positions[4] = obb.c + (((obb.x - obb.c) * e.x) + ((obb.y - obb.c) * e.y) + ((obb.c - obb.z) * e.z));
        Positions[5] = obb.c + (((obb.c - obb.x) * e.x) + ((obb.y - obb.c) * e.y) + ((obb.c - obb.z) * e.z));
        Positions[6] = obb.c + (((obb.x - obb.c) * e.x) + ((obb.c - obb.y) * e.y) + ((obb.c - obb.z) * e.z));
        Positions[7] = obb.c + (((obb.c - obb.x) * e.x) + ((obb.c - obb.y) * e.y) + ((obb.c - obb.z) * e.z));

        Gizmos.color = colour;
        Gizmos.DrawLine(Positions[0], Positions[1]);
        Gizmos.DrawLine(Positions[1], Positions[3]);
        Gizmos.DrawLine(Positions[3], Positions[2]);
        Gizmos.DrawLine(Positions[2], Positions[0]);
        
        Gizmos.DrawLine(Positions[4], Positions[5]);
        Gizmos.DrawLine(Positions[5], Positions[7]);
        Gizmos.DrawLine(Positions[7], Positions[6]);
        Gizmos.DrawLine(Positions[6], Positions[4]);
        //Connectors
        Gizmos.DrawLine(Positions[0], Positions[4]);
        Gizmos.DrawLine(Positions[1], Positions[5]);
        Gizmos.DrawLine(Positions[3], Positions[7]);
        Gizmos.DrawLine(Positions[2], Positions[6]);
    }

    void Draw_AABB()
    {
        Baked_Octree.CUBE c = new Baked_Octree.CUBE();
        Vector3 p = Vector3.zero;
        float s = (aabb.Max.x - aabb.Min.x) * 0.5f;

        //top
        c.v0 = new Vector3(p.x - s, p.y + s, p.z + s);
        c.v1 = new Vector3(p.x - s, p.y + s, p.z - s);
        c.v2 = new Vector3(p.x + s, p.y + s, p.z + s);
        c.v3 = new Vector3(p.x + s, p.y + s, p.z - s);
        //bottom
        c.v4 = new Vector3(p.x - s, p.y - s, p.z + s);
        c.v5 = new Vector3(p.x - s, p.y - s, p.z - s);
        c.v6 = new Vector3(p.x + s, p.y - s, p.z + s);
        c.v7 = new Vector3(p.x + s, p.y - s, p.z - s);

        Gizmos.color = Color.black;
        //Top square
        Gizmos.DrawLine(c.v0, c.v1);
        Gizmos.DrawLine(c.v1, c.v3);
        Gizmos.DrawLine(c.v3, c.v2);
        Gizmos.DrawLine(c.v2, c.v0);

        //Bottom square
        Gizmos.DrawLine(c.v4, c.v5);
        Gizmos.DrawLine(c.v5, c.v7);
        Gizmos.DrawLine(c.v7, c.v6);
        Gizmos.DrawLine(c.v6, c.v4);

        //Connecting Top and Bottom
        Gizmos.DrawLine(c.v0, c.v4);
        Gizmos.DrawLine(c.v1, c.v5);
        Gizmos.DrawLine(c.v3, c.v7);
        Gizmos.DrawLine(c.v2, c.v6);
    }

    void OnDrawGizmos()
    {
        if(obb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(obb.c, obb.x);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(obb.c, obb.y);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(obb.c, obb.z);

            if(Octree.OctreeNode.Intersect(aabb, obb))
            {
                Draw_OBB(Color.red);
            }
            else
            {
                Draw_OBB(Color.green);
            }
        }

        if (aabb != null)
        {
            Draw_AABB();
        }
    }
}
