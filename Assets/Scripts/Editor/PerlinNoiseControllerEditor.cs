using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PerlinNoiseController))]
public class PerlinNoiseControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Update Noise Properties"))
            {
                if (target is PerlinNoiseController controller) 
                {
                    controller.UpdateShaderProperty();
                }
            }
        }
        base.OnInspectorGUI();
    }
}
