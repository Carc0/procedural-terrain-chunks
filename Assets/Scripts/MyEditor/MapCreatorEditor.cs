using Assets.Scripts.Map;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.MyEditor
{
    // Editor principal de la clase MapCreator
    [CustomEditor(typeof(MapCreator))]
    public class MapCreatorEditor : Editor
    {
        private MapCreator mapCreator;


        private void OnEnable()
        {
            mapCreator = target as MapCreator;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            if (GUILayout.Button("Create map"))
            {
                mapCreator.CreateMap();
            }

            if (GUILayout.Button("Do next step"))
            {
                mapCreator.DoNextStep();
            }

            if (GUILayout.Button("Delete map"))
            {
                mapCreator.DeleteAllChunks();
            }
        }
    }
}