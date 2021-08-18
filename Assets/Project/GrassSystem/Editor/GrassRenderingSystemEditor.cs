using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.TerrainAPI;
using UnityEngine;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(GrassRenderingSystem))]
public class GrassRenderingSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Reload (데이터 전체 재설정)"))
        {
            (target as GrassRenderingSystem).ReLoad();
        }

        if (GUILayout.Button("Refresh (다시 그리기)"))
        {
            (target as GrassRenderingSystem).Refresh();
        }
    }
}
