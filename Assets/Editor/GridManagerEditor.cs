using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Rebuild Grid"))
        {
            GridManager gridManager = (GridManager)target;
            gridManager.RebuildGrid();
            EditorUtility.SetDirty(gridManager);
        }
    }
}
