#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;

//A class that will bake the Octree
[CustomEditor(typeof(Voxelizer))]
public class Octree_Baker : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Voxelizer octreeBuilder = (Voxelizer)target;
        if(GUILayout.Button("Bake Octree!"))
        {
            octreeBuilder.BAKE_OCTREE();
        }

        if(GUILayout.Button("Delete Octree!"))
        {
            octreeBuilder.DeleteOctree();
        }
    }
}
#endif