using UnityEngine;
using System.Collections;

public class Make_Icosahedron : MonoBehaviour
{
    //Found at: https://schneide.wordpress.com/2016/07/15/generating-an-icosphere-in-c/

    float X = 0.525731112119133606f;
    float Z = 0.850650808352039932f;
    float N = 0.0f;

    public Material material;
    public float size = 4.0f;
    public Mesh mesh;

    GameObject[] Verticies;

    // Use this for initialization
    void Start ()
    {
        X *= size;
        Z *= size;

        Verticies = new GameObject[12];
        for(int i = 0; i < 12; ++i)
        {
            Verticies[i] = new GameObject();
            Verticies[i].name = i.ToString();
            Verticies[i].AddComponent(typeof(MeshFilter));
            Verticies[i].AddComponent(typeof(MeshRenderer));
            Verticies[i].GetComponent<MeshFilter>().mesh = mesh;
            Verticies[i].transform.parent = gameObject.transform;
            Verticies[i].GetComponent<MeshRenderer>().material = material;
        }

        Vector3[] positions = new Vector3[12];
        //{-X,N,Z}, {X,N,Z}, {-X,N,-Z}, {X,N,-Z},
        positions[0] = new Vector3(-X, N, Z);
        positions[1] = new Vector3( X, N, Z);
        positions[2] = new Vector3(-X, N, -Z);
        positions[3] = new Vector3( X, N, -Z);
        //{N,Z,X}, {N,Z,-X}, {N,-Z,X}, {N,-Z,-X},
        positions[4] = new Vector3( N, Z, X);
        positions[5] = new Vector3(N, Z, -X);
        positions[6] = new Vector3(N, -Z, X);
        positions[7] = new Vector3(N, -Z, -X);
        //{Z,X,N}, {-Z,X, N}, {Z,-X,N}, {-Z,-X, N}
        positions[8] = new Vector3(Z, X, N);
        positions[9] = new Vector3(-Z, X, N);
        positions[10] = new Vector3(Z, -X, N);
        positions[11] = new Vector3(-Z, -X, N);

        for(int i = 0; i < 12; ++i)
        {
            Verticies[i].transform.position = positions[i];
        }
    }

    void Update()
    {

    }
}
