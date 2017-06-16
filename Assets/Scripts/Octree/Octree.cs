using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//========================================================================================================================
//Following this guy: https://www.youtube.com/watch?v=m0guE7804to
//Thank you!!!!
//========================================================================================================================


//========================================================================================================================
//Structure to used for Signed Distance Fields
//========================================================================================================================
public class SDF_Struct
{
    public Vector3 mPosition; //position of the CubeNode(Octet)
    public float mSize; //size of the Cube (X,Y,Z)
    public Vector3 mPoint; //Point on the nearest geometry
    public float mDistance; //distance to nearest Geometry surface...this isn't really necessary
}

public enum OctreeIndex //An OctreeNode is sub-divided into 8 smaller Cubes
{
    /*
     *       0---2
     *      /   /|
     *     1---3 |
     *     | 4 : 6
     *     5---7/
     *     
     *     Y  Z
     *     | /
     *     |/___X
    */

    //Use Bit flags to do this...faster look ups for sure
    //Left Bit : Top or Bottom
    //Mid Bit  : Left or Right
    //Low Bit  : Front or Back

    TopLeftFront = 5, //(101)
    TopLeftBack = 4, //(100)
    TopRightFront = 7,//(111)
    TopRightBack = 6, //(110)

    BottomLeftFront = 1, //(001)
    BottomLeftBack = 0, //(000)
    BottomRightFront = 3, //(011)
    BottomRightBack = 2  //(010)
}

//========================================================================================================================
//octree class
//========================================================================================================================

public class Octree
{
    //A node(Cube) used in the tree 
    public class OctreeNode
    {
        //Members
        int mNodeIndex; //creation order of this node
        int mParentIndex;

        SDF_Struct mSDF;
        int mNodeDepth; //how deep is this node from Root 
        OctreeNode mParent;
        OctreeNode[] mSubNodes;
        public bool mHasGeo; //if this node has geometry
        AABB mAABB; //this nodes AABB
        bool mInsideMesh; //if this node is inside a collider

        Octree mOctree; //which Octree this node belongs too

        //Methods
        public OctreeNode(Octree owner, Vector3 position, float size, int depth, int ParentIndex, OctreeNode parent)
        {
            mOctree = owner;
            //Increase the number of nodes now
            mOctree.mNumberNodes += 1;
            
            //Debug.Log("Current Index = " + mOctree.mNumberNodes + " ,parent = " + ParentIndex);

            mSDF = new SDF_Struct();

            mSDF.mPosition = position;
            mSDF.mSize = size;

            mNodeDepth = depth;
            mParentIndex = ParentIndex;
            mParent = parent;
            mNodeIndex = mOctree.mNumberNodes; //this node was just created so it's the newest one

            //for now arrbitrary values here
            mSDF.mPoint = new Vector3(999999.9f, 999999.9f, 999999.9f);
            mSDF.mDistance = 999999.9f;//Mathf.Infinity;
            mInsideMesh = false;

            mAABB = new AABB();
            mAABB.Max.x = position.x + size;
            mAABB.Max.y = position.y + size;
            mAABB.Max.z = position.z + size;
            mAABB.Min.x = position.x - size;
            mAABB.Min.y = position.y - size;
            mAABB.Min.z = position.z - size;

            //check if Node intersects with scene geometry AABBs
            mHasGeo = HasGeo();           

            //Lowest level dept = -1
            if (mHasGeo && mNodeDepth >= 0)
            {
              SubDivide();
            }
        }

        //SubDivide the Node into 8 smaller nodes
        public void SubDivide()
        {
            mSubNodes = new OctreeNode[8];
            float halfSize = mSDF.mSize * 0.5f;

            for (int i = 0; i < 8; ++i)
            {
                Vector3 newPos = mSDF.mPosition;

                //Check which Cube this should be
                /*
                 *       0---2
                 *      /   /|
                 *     1---3 |
                 *     | 4 : 6
                 *     5---7/
                 *     
                 *     Y  Z
                 *     | /
                 *     |/___X
                */

                switch (i)
                {
                    case (int)OctreeIndex.TopLeftFront:
                        newPos.x -= halfSize; newPos.y += halfSize; newPos.z += halfSize;
                        break;
                    case (int)OctreeIndex.TopLeftBack:
                        newPos.x -= halfSize; newPos.y += halfSize; newPos.z -= halfSize;
                        break;
                    case (int)OctreeIndex.TopRightFront:
                        newPos.x += halfSize; newPos.y += halfSize; newPos.z += halfSize;
                        break;
                    case (int)OctreeIndex.TopRightBack:
                        newPos.x += halfSize; newPos.y += halfSize; newPos.z -= halfSize;
                        break;
                    case (int)OctreeIndex.BottomLeftFront:
                        newPos.x -= halfSize; newPos.y -= halfSize; newPos.z += halfSize;
                        break;
                    case (int)OctreeIndex.BottomLeftBack:
                        newPos.x -= halfSize; newPos.y -= halfSize; newPos.z -= halfSize;
                        break;
                    case (int)OctreeIndex.BottomRightFront:
                        newPos.x += halfSize; newPos.y -= halfSize; newPos.z += halfSize;
                        break;
                    case (int)OctreeIndex.BottomRightBack:
                        newPos.x += halfSize; newPos.y -= halfSize; newPos.z -= halfSize;
                        break;
                    default:
                        break;
                }
                mSubNodes[i] = new OctreeNode(mOctree, newPos, halfSize, mNodeDepth-1, mNodeIndex, this);
            }//End of For loop
        }

        //This node has no subNode children and it can be ignored
        public bool IsLeaf()
        {
            return mSubNodes == null; 
        }

        //Check if Node AABB intersects with any mesh OBBs
        bool HasGeo()
        {
            bool hasGeo = false;

            for(int i = 0; i < All_OBBs.Count; ++i)
            {
                if (hasGeo)
                {
                    break;
                }
                else
                {
                    hasGeo = Intersect(mAABB, All_OBBs[i]);
                }
            }

            return hasGeo;
        }

        //public Getters for private stuff
        public IEnumerable<OctreeNode> Nodes
        {
            get
            {
                return mSubNodes;
            }
        }

        public Vector3 Position
        {
            get { return mSDF.mPosition; }
        }

        public SDF_Struct SDF
        {
            get { return mSDF; }
        }

        public int Depth
        {
            get { return mNodeDepth; }
        }

        public int NodeIndex
        {
            get { return mNodeIndex; }
            set { mNodeIndex = value; }
        }
        public int ParentIndex
        {
            get { return mParentIndex; }
        }

        public bool InsideMesh
        {
            get
            {
                return mInsideMesh;
            }
            set
            {
                mInsideMesh = value;
            }
        }

        public OctreeNode Parent
        {
            get { return mParent; }
        }

        public float size
        {
            get { return mSDF.mSize; }
        }

        public static bool Intersect(AABB A, OBB B)
        {
            //Function found at https://github.com/gszauer/GamePhysicsCookbook/blob/master/Code/Geometry3D.cpp
            //nice clean way of using Separating axis theorum

            Vector3[] axis = new Vector3[15];
            axis[0] = new Vector3(1.0f, 0.0f, 0.0f); //AABB x axis
            axis[1] = new Vector3(0.0f, 1.0f, 0.0f); //AABB y axis
            axis[2] = new Vector3(0.0f, 0.0f, 1.0f); //AABB z axis
            axis[3] = B.x;
            axis[4] = B.y;
            axis[5] = B.z;

            //Fill in the remaining axis
            for (int i = 0; i < 3; ++i)
            {
                axis[6 + i * 3 + 0] = Vector3.Cross(axis[i], axis[0]);
                axis[6 + i * 3 + 1] = Vector3.Cross(axis[i], axis[1]);
                axis[6 + i * 3 + 2] = Vector3.Cross(axis[i], axis[2]);
            }
  
            for(int i = 0; i < 15; ++i)
            {
                if(OverlapOnAxis(A, B, axis[i]) == false)
                {
                    return false; //Separating axis found
                }
            }

            return true;
        }
        static bool OverlapOnAxis(AABB A, OBB B, Vector3 axis)
        {
            //Found at: https://github.com/gszauer/GamePhysicsCookbook/blob/master/Code/Geometry3D.cpp
            
            //using Vec2.x = min, Vec2.y = max
            Vector2 a = GetInterval(A, axis);
            Vector2 b = GetInterval(B, axis);

            return (b.x <= a.y) && (a.x <= b.y);
        }

        static Vector2 GetInterval(AABB aabb, Vector3 axis)
        {
            //Found at: https://github.com/gszauer/GamePhysicsCookbook/blob/master/Code/Geometry3D.cpp
            Vector3 m = aabb.Min;
            Vector3 M = aabb.Max;

            Vector3[] vertex = new Vector3[8];
            vertex[0] = new Vector3(m.x, M.y, M.z);
            vertex[1] = new Vector3(m.x, M.y, m.z);
            vertex[2] = new Vector3(m.x, m.y, M.z);
            vertex[3] = new Vector3(m.x, m.y, m.z);
            vertex[4] = new Vector3(M.x, M.y, M.z);
            vertex[5] = new Vector3(M.x, M.y, m.z);
            vertex[6] = new Vector3(M.x, m.y, M.z);
            vertex[7] = new Vector3(M.x, m.y, m.z);

            Vector2 result;
            result.x = result.y = Vector3.Dot(axis, vertex[0]);

            for(int i = 1; i < 8; ++i)
            {
                float projection = Vector3.Dot(axis, vertex[i]);
                result.x = (projection < result.x) ? projection : result.x;
                result.y = (projection > result.y) ? projection : result.y;
            }

            return result;
        }
        static Vector2 GetInterval(OBB obb, Vector3 axis)
        {
            //Found at: https://github.com/gszauer/GamePhysicsCookbook/blob/master/Code/Geometry3D.cpp
            Vector3[] vertex = new Vector3[8];

            //OBB extents taking scaling into account
            Vector3 e = new Vector3(obb.e.x * obb.s.x, obb.e.y * obb.s.y, obb.e.z * obb.s.z);

            vertex[0] = obb.c + (((obb.x - obb.c) * e.x) + ((obb.y - obb.c) * e.y) + ((obb.z - obb.c) * e.z));
            vertex[1] = obb.c + (((obb.c - obb.x) * e.x) + ((obb.y - obb.c) * e.y) + ((obb.z - obb.c) * e.z));
            vertex[2] = obb.c + (((obb.x - obb.c) * e.x) + ((obb.c - obb.y) * e.y) + ((obb.z - obb.c) * e.z));
            vertex[3] = obb.c + (((obb.c - obb.x) * e.x) + ((obb.c - obb.y) * e.y) + ((obb.z - obb.c) * e.z));
            vertex[4] = obb.c + (((obb.x - obb.c) * e.x) + ((obb.y - obb.c) * e.y) + ((obb.c - obb.z) * e.z));
            vertex[5] = obb.c + (((obb.c - obb.x) * e.x) + ((obb.y - obb.c) * e.y) + ((obb.c - obb.z) * e.z));
            vertex[6] = obb.c + (((obb.x - obb.c) * e.x) + ((obb.c - obb.y) * e.y) + ((obb.c - obb.z) * e.z));
            vertex[7] = obb.c + (((obb.c - obb.x) * e.x) + ((obb.c - obb.y) * e.y) + ((obb.c - obb.z) * e.z));

            Vector2 result;
            result.x = result.y = Vector3.Dot(axis, vertex[0]);

            for(int i = 1; i < 8; ++i)
            {
                float projection = Vector3.Dot(axis, vertex[i]);
                result.x = (projection < result.x) ? projection : result.x;
                result.y = (projection > result.y) ? projection : result.y;
            }


            return result;
        }

        static bool Intersect(AABB A, AABB B)
        {
            //http://www.miguelcasillas.com/?p=30
            return (A.Max.x > B.Min.x &&
                A.Min.x < B.Max.x &&
                A.Max.y > B.Min.y &&
                A.Min.y < B.Max.y &&
                A.Max.z > B.Min.z &&
                A.Min.z < B.Max.z);
        }
        static bool Intersect(AABB A, Vector3 P)
        {
            //http://www.miguelcasillas.com/?p=24
            if(P.x > A.Min.x && P.x < A.Max.x &&
                P.y > A.Min.y && P.y < A.Max.y &&
                P.z > A.Min.z && P.z < A.Max.z)
            {
                return true;
            }

            return false;
        }

        public bool IsLowestNode()
        {
            return mNodeDepth < 0;
        }
    }

    //Subdividing the Octree based on AABBs is pretty bad beacuse you can't get good depth and accuracy
    public class AABB
    {
        public Vector3 Min,Max; //Min/Max corners for this AABB
    }

    public class OBB
    {
        //Center, Basis Vecs, Extents
        public Vector3 c, x , y , z, e;
        //scale of OBB
        public Vector3 s;

        public float[] OrientationArray
        {
            get
            {
                float[] o = new float[9];
                o[0] = x.x;
                o[1] = x.y;
                o[2] = x.z;
                o[3] = y.x;
                o[4] = y.y;
                o[5] = y.z;
                o[6] = z.x;
                o[7] = z.y;
                o[8] = z.z;

                return o;
            }
        }

        public Vector3[] Axis
        {
            get
            {
                Vector3[] axis = new Vector3[3];
                axis[0] = x;
                axis[1] = y;
                axis[2] = z;
                return axis;
            }
        }
    }

    //========================================================================================================================
    //Members
    //========================================================================================================================
    private OctreeNode mRoot;
    private int mNumberNodes;

    //All the Scene Geometry in the Node
    public static List<OBB> All_OBBs = null;

    //========================================================================================================================
    //Methods
    //========================================================================================================================
    public Octree (Vector3 position, float size, int depth)
    {
        mNumberNodes = -1;
        mRoot = new OctreeNode(this, position, size, depth, -1, null);
    }

    //using bit flags to find which index this Node should be
    private int GetIndexOfPosition(Vector3 lookUpPosition, Vector3 nodePosition)
    {
        int index = 0;

        /*
         *       0---2
         *      /   /|
         *     1---3 |
         *     | 4 : 6
         *     5---7/
         *     
         *     Y  Z
         *     | /
         *     |/___X
        */

        //ternary operations huzza!
        index |= lookUpPosition.y > nodePosition.y ? 4 : 0; //either top(100) or bottom(000)
        index |= lookUpPosition.x > nodePosition.x ? 2 : 0; //then right(010) or left(000)
        index |= lookUpPosition.z > nodePosition.z ? 1 : 0;// then front(001) or bottom(000)

        return index;
    }

    public OctreeNode GetRootNode()
    {
        return mRoot;
    }

    public int Size
    {
        get
        {
            return mNumberNodes;
        }
    }
}

