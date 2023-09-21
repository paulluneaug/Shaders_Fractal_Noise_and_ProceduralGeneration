using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PerlinNoise3DController))]
public class PerlinNoise3DControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Update Noise Properties"))
            {
                if (target is PerlinNoise3DController controller) 
                {
                    controller.UpdateShaderProperty();
                }
            }
        }
        base.OnInspectorGUI();
    }
}
