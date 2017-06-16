using UnityEngine;
using System.Collections;
using System.Collections.Generic; //used for List<T>
using System.IO; //for reading/writing files

//This class reads a file and Builds a Sparse Octree array based on that file

public class Baked_Octree : MonoBehaviour
{
    [HideInInspector]
    public string FileName;

    [HideInInspector]
    public Vector3 Position; //the center of this Octree

    [HideInInspector]
    public Voxelizer.SDF_Node[] nodeArray;

    [HideInInspector]
    public float Root_Size;
    float Smallest_Size;

    [HideInInspector]
    public int TreeDepth;

    [HideInInspector]
    public bool Initialized = false;

    [HideInInspector]
    public bool DrawTree = false;

    [HideInInspector]
    public bool DrawNormals = true;

    [HideInInspector]
    public bool DrawNodes = true;

    //Material for drawing the grid
    public Material LineMaterial;
    //use this to draw all the lines in one single draw call
    List<LINE> All_Lines = null;

    //=============================================================================================================================================
    //simple structs used for Debugging here
    //=============================================================================================================================================
    //used to make a cube into Lines
    public struct CUBE
    {
        public Vector3 v0, v1, v2, v3, v4, v5, v6, v7;
    }
    //used to draw Lines using GL lines
    private struct LINE
    {
        public LINE(Vector3 a, Vector3 b, bool n)
        {
            p1 = a;
            p2 = b;
            isNormal = n;
        }
        public Vector3 p1, p2;
        public bool isNormal;
    }

    //=============================================================================================================================================
    //called from Particles Start() function
    //=============================================================================================================================================
    public void Load_Tree()
    {
        Initialized = false;
        //read from file
        string path = "Assets/Resources/Baked_Octrees/" + FileName + ".txt";
        string SavedTree = System.IO.File.ReadAllText(path);// PlayerPrefs.GetString(FileName);

        if (SavedTree != "")
        {
            //read the tree
            // Depth,Count;RootSize/Node[0]/Node[1]/..../Node[N]

            string word = "";
            char c = ' ';
            int j = 0;
            int NodeCount = 0;

            //First read all the header stuff before reading each node
            for(int i = 0; i < SavedTree.Length; ++i)
            {
                c = SavedTree[i];

                //check for delimeters
                if (c == '/')
                {
                    Root_Size = float.Parse(word);
                    word = "";
                    j = i + 1; //ignore starting '/'
                    break;
                }  
                else if (c == ',')
                {
                    TreeDepth = int.Parse(word);
                    word = "";
                }
                else if (c == ';')
                {
                    NodeCount = int.Parse(word);
                    word = "";
                }
                else
                    word += c;
            }

            //Debug.Log(TreeDepth);
            //Debug.Log(NodeCount);
            //Debug.Log(Root_Size);

            nodeArray = new Voxelizer.SDF_Node[NodeCount]; 
            int nI = 0;

            //Now read all the Nodes
            for (int k = j; k < SavedTree.Length; ++k)
            {
                c = SavedTree[k];

                if (c == '/')
                {
                    nodeArray[nI] = ParseNode(word);
                    //Debug.Log(nodeArray[nI].ToString());
                    word = "";
                    nI++;
                }
                else if(c == '*') //end of nodes
                {
                    nodeArray[nI] = ParseNode(word);
                    //Debug.Log(nodeArray[nI].ToString());
                    j = k + 1;
                    word = "";
                    break;
                }
                else
                    word += c;
            }

            //finally read it's position
            Position = new Vector3();
            int axis = 0;
            for(int k = j; k < SavedTree.Length; ++k)
            {
                c = SavedTree[k];
                if (c == ',')
                {
                    switch (axis)
                    {
                        case 0:
                            Position.x = float.Parse(word);
                            break;
                        case 1:
                            Position.y = float.Parse(word);
                            break;
                        case 2:
                            Position.z = float.Parse(word);
                            break;
                        default:
                            break;
                    }
                    word = "";
                    axis++;
                }
                else
                    word += c;
            }

        } //End of Reading nodeArray from string/file

        //Now build the Octree from lines
        if (All_Lines == null)
        {
            All_Lines = new List<LINE>();
        }

        Smallest_Size = Root_Size * 2.0f;
        for(int i = TreeDepth; i >= 0; --i)
        {
            Smallest_Size *= 0.5f;
        }
        
        //Build the Nodes into an List of Lines so we can Draw them all at once
        BuildNodeLines();

        //Log_NodeArray();

        DrawTree = false; //Drawing a massive Tree is Costly as Fuck even if it's a single draw call
        Initialized = true;
    }

    //=============================================================================================================================================
    //helper function to read a string and format it into a Node
    //=============================================================================================================================================
    Voxelizer.SDF_Node ParseNode(string word)
    {
        Voxelizer.SDF_Node node = new Voxelizer.SDF_Node();

        int item = 0;
        string str = "";
        word += ','; //required to get through all items
        char c = word[0];

        //Node Format : int,int[8],Vector3,Vector3,Vector3
        for (int i = 0; i < word.Length; ++i)
        {
            c = word[i];

            if(c == ',')
            {
                switch (item)
                {
                    case 0:
                        node.index = int.Parse(str);
                        break;
                    case 1: //Node Index
                        node.c0 = int.Parse(str);
                        break;
                    case 2: //Node Children
                        node.c1 = int.Parse(str);
                        break;
                    case 3:
                        node.c2 = int.Parse(str);
                        break;
                    case 4:
                        node.c3 = int.Parse(str);
                        break;
                    case 5:
                        node.c4 = int.Parse(str);
                        break;
                    case 6:
                        node.c5 = int.Parse(str);
                        break;
                    case 7:
                        node.c6 = int.Parse(str);
                        break;
                    case 8:
                        node.c7 = int.Parse(str);
                        break;
                    case 9: //Node Min
                        node.Min.x = float.Parse(str);
                        break;
                    case 10: 
                        node.Min.y = float.Parse(str);
                        break;
                    case 11: 
                        node.Min.z = float.Parse(str);
                        break;
                    case 12: //Node Max
                        node.Max.x = float.Parse(str);
                        break;
                    case 13:
                        node.Max.y = float.Parse(str);
                        break;
                    case 14:
                        node.Max.z = float.Parse(str);
                        break;
                    case 15: //Node SurfacePoint
                        node.SurfacePoint.x = float.Parse(str);
                        break;
                    case 16:
                        node.SurfacePoint.y = float.Parse(str);
                        break;
                    case 17: //never gets called....
                        node.SurfacePoint.z = float.Parse(str);
                        break;
                    default:
                        break;
                }
                str = "";
                item++;
            }
            else
            {
                str += c;
            }
        }
        return node;
    }

    //=============================================================================================================================================
    //Helper function to debug the nodeArray..To see if the Indexing is correct
    //=============================================================================================================================================
    void Log_NodeArray()
    {
        Debug.Log("Tree Size = " + nodeArray.Length);
        //for safety... Using Debug.Log that much is slow as hell
        if (nodeArray.Length > 500)
            return;

        for (int i = 0; i < nodeArray.Length; ++i)
        {
            Debug.Log("Node[" + i + "] index: " + nodeArray[i].index + " AABB: " + nodeArray[i].Min + ", " + nodeArray[i].Max);

            if (nodeArray[i].c0 == -1)
            {
                Debug.Log("\tNode [" + i + "] child[0]: " + nodeArray[i].c0);
            }
            else
            {
                if (nodeArray[nodeArray[i].c0].c0 != -1)
                {
                    Debug.Log("\tNode [" + i + "] child[0]: " + nodeArray[i].c0 + "_[8]");
                }
                else
                {
                    Debug.Log("\tNode [" + i + "] child[0]: " + nodeArray[i].c0 + "_[0]");
                }

            }
            if (nodeArray[i].c1 == -1)
            {
                Debug.Log("\tNode [" + i + "] child[1]: " + nodeArray[i].c1);
            }
            else
            {
                if (nodeArray[nodeArray[i].c1].c0 != -1)
                {
                    Debug.Log("\tNode [" + i + "] child[1]: " + nodeArray[i].c1 + "_[8]");
                }
                else
                {
                    Debug.Log("\tNode [" + i + "] child[1]: " + nodeArray[i].c1 + "_[0]");
                }
            }
            if (nodeArray[i].c2 == -1)
            {
                Debug.Log("\tNode [" + i + "] child[2]: " + nodeArray[i].c2);
            }
            else
            {
                if (nodeArray[nodeArray[i].c2].c0 != -1)
                {
                    Debug.Log("\tNode [" + i + "] child[2]: " + nodeArray[i].c2 + "_[8]");
                }
                else
                {
                    Debug.Log("\tNode [" + i + "] child[2]: " + nodeArray[i].c2 + "_[0]");
                }
            }
            if (nodeArray[i].c3 == -1)
            {
                Debug.Log("\tNode [" + i + "] child[3]: " + nodeArray[i].c3);
            }
            else
            {
                if (nodeArray[nodeArray[i].c3].c0 != -1)
                {
                    Debug.Log("\tNode [" + i + "] child[3]: " + nodeArray[i].c3 + "_[8]");
                }
                else
                {
                    Debug.Log("\tNode [" + i + "] child[3]: " + nodeArray[i].c3 + "_[0]");
                }
            }
            if (nodeArray[i].c4 == -1)
            {
                Debug.Log("\tNode [" + i + "] child[4]: " + nodeArray[i].c4);
            }
            else
            {
                if (nodeArray[nodeArray[i].c4].c0 != -1)
                {
                    Debug.Log("\tNode [" + i + "] child[4]: " + nodeArray[i].c4 + "_[8]");
                }
                else
                {
                    Debug.Log("\tNode [" + i + "] child[4]: " + nodeArray[i].c4 + "_[0]");
                }
            }
            if (nodeArray[i].c5 == -1)
            {
                Debug.Log("\tNode [" + i + "] child[5]: " + nodeArray[i].c5);
            }
            else
            {
                if (nodeArray[nodeArray[i].c5].c0 != -1)
                {
                    Debug.Log("\tNode [" + i + "] child[5]: " + nodeArray[i].c5 + "_[8]");
                }
                else
                {
                    Debug.Log("\tNode [" + i + "] child[5]: " + nodeArray[i].c5 + "_[0]");
                }
            }
            if (nodeArray[i].c6 == -1)
            {
                Debug.Log("\tNode [" + i + "] child[6]: " + nodeArray[i].c6);
            }
            else
            {
                if (nodeArray[nodeArray[i].c6].c0 != -1)
                {
                    Debug.Log("\tNode [" + i + "] child[6]: " + nodeArray[i].c6 + "_[8]");
                }
                else
                {
                    Debug.Log("\tNode [" + i + "] child[6]: " + nodeArray[i].c6 + "_[0]");
                }
            }
            if (nodeArray[i].c7 == -1)
            {
                Debug.Log("\tNode [" + i + "] child[7]: " + nodeArray[i].c7);
            }
            else
            {
                if (nodeArray[nodeArray[i].c7].c0 != -1)
                {
                    Debug.Log("\tNode [" + i + "] child[7]: " + nodeArray[i].c7 + "_[8]");
                }
                else
                {
                    Debug.Log("\tNode [" + i + "] child[7]: " + nodeArray[i].c7 + "_[0]");
                }
            }
        }
    }

    //=====================================================================================================================
    //Debug drawing functions
    //=====================================================================================================================

    public void DrawVoxelGrid(Color colour)
    {
        if (LineMaterial)
        {
            if (DrawTree && Initialized)
            {
                DrawAll_Lines(colour);
            }
        }
    }

    void BuildNodeLines()
    {
        for(int i = 0; i < nodeArray.Length; ++i)
        {
            
            Voxelizer.SDF_Node n = nodeArray[i];

            // the Cube
            float s = (n.Max.x - n.Min.x) * 0.5f;
            Vector3 p = new Vector3(n.Min.x + s, n.Min.y + s, n.Min.z + s);

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

            CUBE c;
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

            AddCube(c);

            //at lowest nodes draw the Nearest surface Vector
            if (n.c0 == -1 && s*2.0f <= Smallest_Size)
            {
                AddLine(p, n.SurfacePoint, true);
            }
        }
    }

    //using GL Lines : https://gamedev.stackexchange.com/questions/96964/how-to-correctly-draw-a-line-in-unity
    void AddCube(CUBE c)
    {

        //Top square
        All_Lines.Add(new LINE(c.v0, c.v1, false));
        All_Lines.Add(new LINE(c.v1, c.v3, false));
        All_Lines.Add(new LINE(c.v3, c.v2, false));
        All_Lines.Add(new LINE(c.v2, c.v0, false));

        //Bottom square
        All_Lines.Add(new LINE(c.v4, c.v5, false));
        All_Lines.Add(new LINE(c.v5, c.v7, false));
        All_Lines.Add(new LINE(c.v7, c.v6, false));
        All_Lines.Add(new LINE(c.v6, c.v4, false));

        //Connecting Top and Bottom
        All_Lines.Add(new LINE(c.v0, c.v4, false));
        All_Lines.Add(new LINE(c.v1, c.v5, false));
        All_Lines.Add(new LINE(c.v3, c.v7, false));
        All_Lines.Add(new LINE(c.v2, c.v6, false));
    }

    //Add a line to our List
    void AddLine(Vector3 p, Vector3 p2, bool normal)
    {
        if (p2 == Vector3.zero)
            return; //un-initialized Bottom node
        else
            All_Lines.Add(new LINE(p, p2, normal));
    }

    void DrawAll_Lines(Color colour)
    {
        if (All_Lines == null)
            return;

        GL.PushMatrix();
        LineMaterial.SetPass(0);
        LineMaterial.SetColor("_Color", colour);
        GL.Color(colour);
        GL.Begin(GL.LINES);
        for (int i = 0; i < All_Lines.Count; ++i)
        {
            if(All_Lines[i].isNormal && DrawNormals)
            {
                GL.Vertex(All_Lines[i].p1);
                GL.Vertex(All_Lines[i].p2);
            }
            else if(!All_Lines[i].isNormal && DrawNodes)
            {
                GL.Vertex(All_Lines[i].p1);
                GL.Vertex(All_Lines[i].p2);
            }
        }

        GL.End();
        GL.PopMatrix();
    }

    //Helper function to draw OBB using Gizmos class
    static public void Draw_OBB(Octree.OBB obb)
    {
        Vector3[] Positions = new Vector3[8];
        Vector3 v = new Vector3(obb.e.x * obb.s.x, obb.e.y * obb.s.y, obb.e.z * obb.s.z);

        Positions[0] = obb.c + (((obb.x - obb.c) * v.x) + ((obb.y - obb.c) * v.y) + ((obb.z - obb.c) * v.z));
        Positions[1] = obb.c + (((obb.c - obb.x) * v.x) + ((obb.y - obb.c) * v.y) + ((obb.z - obb.c) * v.z));
        Positions[2] = obb.c + (((obb.x - obb.c) * v.x) + ((obb.c - obb.y) * v.y) + ((obb.z - obb.c) * v.z));
        Positions[3] = obb.c + (((obb.c - obb.x) * v.x) + ((obb.c - obb.y) * v.y) + ((obb.z - obb.c) * v.z));

        Positions[4] = obb.c + (((obb.x - obb.c) * v.x) + ((obb.y - obb.c) * v.y) + ((obb.c - obb.z) * v.z));
        Positions[5] = obb.c + (((obb.c - obb.x) * v.x) + ((obb.y - obb.c) * v.y) + ((obb.c - obb.z) * v.z));
        Positions[6] = obb.c + (((obb.x - obb.c) * v.x) + ((obb.c - obb.y) * v.y) + ((obb.c - obb.z) * v.z));
        Positions[7] = obb.c + (((obb.c - obb.x) * v.x) + ((obb.c - obb.y) * v.y) + ((obb.c - obb.z) * v.z));

        Gizmos.color = Color.green;
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
}
