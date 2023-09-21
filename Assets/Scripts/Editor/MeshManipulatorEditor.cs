using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshManipulator))]
public class MeshManipulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (Application.isPlaying)
        {
            if (target is not MeshManipulator manipulator)
            {
                return;
            }
            if (GUILayout.Button("Make Cube into Sphere"))
            {
                manipulator.MakeCubeIntoShere(1);
            }
            if (GUILayout.Button("Reset Mesh"))
            {
                manipulator.ResetMesh();
            }
        }
        base.OnInspectorGUI();
    }
}
