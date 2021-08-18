using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GeometryGrassDrawing))]
public class GeometryGrassDrawingTool : Editor
{
    private GeometryGrassDrawing m_GeometryGrass;

    SerializedObject m_SerializedSourceTexture;

    //The center of the circle
    private Vector3 m_BrushCenter;

    public bool m_IsDrawing;
    public bool m_IsEraser;
    bool m_MouseDown;

    private void OnEnable()
    {
        m_GeometryGrass = target as GeometryGrassDrawing;
        m_IsDrawing = false;

        Tools.hidden = true;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        //Unhide the handles of the GO
        Tools.hidden = false;

        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnSceneGUI(SceneView _View)
    {
        if (!m_IsDrawing) return;

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            m_MouseDown = true;
        else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            if (m_MouseDown)
            {
                if (m_GeometryGrass.m_PixelUpdate)
                {
                    m_GeometryGrass.m_PixelUpdate = false;
                    m_GeometryGrass.SaveMask();
                    Repaint();
                }
            }
            m_MouseDown = false;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, m_GeometryGrass.m_BrushDistance, m_GeometryGrass.m_LayerMask))
        {
            m_BrushCenter = hit.point;
            bool active = false;
            if (m_IsEraser)
                active = true;
            else if (Vector3.Dot(Vector3.up, hit.normal) > 0.5f)
                active = true;

            if (active)
            {
                SceneView.RepaintAll();
                if (m_MouseDown && m_GeometryGrass.m_Density > 0)
                {
                    RaycastHit hit2;
                    UnityEngine.Random.InitState(128);

                    Vector3[] tmpPoints = new Vector3[m_GeometryGrass.m_Density];
                    int pointcount = 0;
                    for (int i = 0; i < m_GeometryGrass.m_Density; ++i)
                    {
                        Ray ray2 = new Ray();

                        float x = UnityEngine.Random.Range(-m_GeometryGrass.m_CircleRadius, m_GeometryGrass.m_CircleRadius);
                        float z = UnityEngine.Random.Range(-m_GeometryGrass.m_CircleRadius, m_GeometryGrass.m_CircleRadius);

                        ray2.origin = hit.point + new Vector3(x, 5, z);
                        ray2.direction = Vector3.down;
                        if (Physics.Raycast(ray2, out hit2, 5.2f, m_GeometryGrass.m_LayerMask))
                        {
                            if (m_IsEraser)
                            {
                                tmpPoints[i] = new Vector3(hit2.point.x, hit2.point.y + m_GeometryGrass.m_HeightOffset, hit2.point.z);
                                pointcount++;
                            }
                            else if (Vector3.Dot(Vector3.up, hit2.normal) > 0.5f)
                            {
                                tmpPoints[i] = new Vector3(hit2.point.x, hit2.point.y + m_GeometryGrass.m_HeightOffset, hit2.point.z);
                                pointcount++;
                            }
                        }
                    }

                    Vector3[] points = new Vector3[pointcount];
                    Array.Copy(tmpPoints, points, pointcount);

                    Color color = m_GeometryGrass.m_DrawColor;
                    if (m_IsEraser)
                    {
                        color = Color.black;
                    }
                    else
                    {
                        color.g = color.g == 0 ? 0.02f : color.g;
                    }
                    m_GeometryGrass.SetMaskPixel(points, color);
                }

                Handles.DrawWireDisc(m_BrushCenter, hit.normal, m_GeometryGrass.m_CircleRadius);
            }
        }

        HandleUtility.AddDefaultControl(0);
    }

    //Add buttons this scripts inspector
    public override void OnInspectorGUI()
    {
        //Add the default stuff
        DrawDefaultInspector();

        if (m_GeometryGrass == null) return;

        EditorGUILayout.HelpBox("브러싱을 할때 G채널은 강제적으로 5 이상이 됩니다.", MessageType.Info);

        m_GeometryGrass.m_ColorSourceTexture = EditorGUILayout.ObjectField("ColorSourceTexture", m_GeometryGrass.m_ColorSourceTexture, typeof(Texture2D), false) as Texture2D;
        m_GeometryGrass.m_HeightMapSourceTexture = EditorGUILayout.ObjectField("HeightSourceTexture", m_GeometryGrass.m_HeightMapSourceTexture, typeof(Texture2D), false) as Texture2D;

        if (GUILayout.Button("Source Load"))
        {
            m_GeometryGrass.SourceLoad();
        }

        if (!m_IsDrawing)
        {
            EditorGUILayout.HelpBox("높이(Y축) 0 ~ 100 사이만 적용 가능", MessageType.Info);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Drawing"))
            {
                m_IsDrawing = true;
            }
        }
        else
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Stop"))
            {
                m_IsDrawing = false;
            }
        }

        if (!m_IsEraser)
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Brush Mode"))
            {
                m_IsEraser = true;
            }
        }
        else
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Eraser Mode"))
            {
                m_IsEraser = false;
            }
        }
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("ResetTexture"))
        {
            m_GeometryGrass.ResetTexture();
        }

        if (GUILayout.Button("SaveTexture"))
        {
            m_GeometryGrass.SaveMask();
        }

        if (m_GeometryGrass.m_ColorTexture)
        {
            EditorGUI.DrawPreviewTexture(new Rect(40,600,300,300), m_GeometryGrass.m_ColorTexture);
        }

        if (m_GeometryGrass.m_ColorTexture)
        {
            EditorGUI.DrawPreviewTexture(new Rect(360, 600, 300, 300), m_GeometryGrass.m_HeightMapTexture);
        }

        EditorGUILayout.Space(400);
    }
}
