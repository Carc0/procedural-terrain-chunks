using Assets.Scripts.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.MyEditor
{
    // Por si se quisiera usar el MeshCombiner con un GameObject seleccionado. Para pruebas.
    [CustomEditor(typeof(MeshCombiner))]
    public class MeshCombinerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            if (GUILayout.Button("Combine selected meshes"))
            {
                MeshCombiner.CombineMeshes(Selection.activeTransform.gameObject);
            }
        }
    }
}