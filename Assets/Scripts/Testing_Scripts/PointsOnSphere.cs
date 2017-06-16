using UnityEngine;
using System.Collections;
using System.Collections.Generic; //for List<T>

//a Class to test how to Points/Vectors on a sphere.. used later on when Raycasting spherically from a point
//Code found at : http://answers.unity3d.com/questions/410992/how-do-i-get-raycasts-to-cast-symmetrically.html
[ExecuteInEditMode]
public class PointsOnSphere : MonoBehaviour
{
    [Range(1.0f, 10.0f)]
    public float Radius = 5.0f;

    public float NumberOfPoints = 10;
    Vector3[] points;

    public static Vector3[] GetPoints(float numPoints)
    {
        List<Vector3> points = new List<Vector3>();
        float i = Mathf.PI * (3 - Mathf.Sqrt(5));
        float offset = 2 / numPoints;
        float halfOffset = 0.5f * offset;
        float y = 0;
        float r = 0;
        float phi = 0;
        int currPoint = 0;
        for (; currPoint < numPoints; currPoint++)
        {
            y = currPoint * offset - 1 + halfOffset;
            r = Mathf.Sqrt(1 - y * y);
            phi = currPoint * i;
            Vector3 point = new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
            if (!points.Contains(point)) points.Add(point);
        }
        return points.ToArray();
    }
	
	// Update is called once per frame
	void Update ()
    {
        points = GetPoints(NumberOfPoints);
	}

    //Draws them in editor
    void OnDrawGizmos()
    {
        //Draws all the points
        Gizmos.color = Color.black;
        Vector3 p = transform.position;
        for(int i = 0; i < points.Length; ++i)
        {
            Gizmos.DrawLine(p, points[i] * Radius);
        }
    }
}
