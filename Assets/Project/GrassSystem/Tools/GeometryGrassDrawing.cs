using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;

[ExecuteInEditMode]
public class GeometryGrassDrawing : MonoBehaviour
{
    public static GeometryGrassDrawing Instance;

    public LayerMask m_LayerMask = -1;
    [Range(0.1f, 50.0f)]
    public float m_CircleRadius = 3.0f;
    public float m_BrushDistance = 1000.0f;
    [Range(3.0f, 100.0f)]
    public int m_Density = 1;

    public RenderTexture m_ColorTexture;
    public RenderTexture m_HeightMapTexture;
    [HideInInspector] public Texture2D m_Texture;

    [SerializeField] ComputeShader m_GrassMaskCS;

    ComputeBuffer m_PositionBuffer;

    [HideInInspector] public bool m_PixelUpdate;

    public Texture2D m_ColorSourceTexture;
    public Texture2D m_HeightMapSourceTexture;
    public Color m_DrawColor;
    [Range(-1.0f, 0.0f)]
    public float m_HeightOffset;

    private void Awake()
    {
        Instance = this;
#if UNITY_EDITOR
        SourceLoad();
#else
        CreateMaskTexture();
#endif
    }

    private void OnEnable()
    {
        Instance = this;
        //CreateMaskTexture();
        m_PixelUpdate = false;
    }

    void CreateMaskTexture()
    {
        if (m_ColorTexture && !m_ColorTexture.enableRandomWrite) return;
        if (m_HeightMapTexture && !m_HeightMapTexture.enableRandomWrite) return;

        m_ColorTexture = new RenderTexture(512,512,1, RenderTextureFormat.ARGB32);
        m_ColorTexture.filterMode = FilterMode.Point;
        m_ColorTexture.enableRandomWrite = true;
        m_ColorTexture.Create();

        m_HeightMapTexture = new RenderTexture(512, 512, 1, RenderTextureFormat.ARGB32);
        m_HeightMapTexture.filterMode = FilterMode.Point;
        m_HeightMapTexture.enableRandomWrite = true;
        m_HeightMapTexture.Create();

        Debug.Log("Create Texture");
    }

    public void SourceLoad()
    {
        if (m_ColorTexture && !m_ColorTexture.enableRandomWrite) return;
        if (m_HeightMapTexture && !m_HeightMapTexture.enableRandomWrite) return;

        m_ColorTexture = new RenderTexture(512, 512, 1, RenderTextureFormat.ARGB32);
        m_ColorTexture.filterMode = FilterMode.Point;
        m_ColorTexture.enableRandomWrite = true;
        m_ColorTexture.Create();

        m_HeightMapTexture = new RenderTexture(512, 512, 1, RenderTextureFormat.ARGB32);
        m_HeightMapTexture.filterMode = FilterMode.Point;
        m_HeightMapTexture.enableRandomWrite = true;
        m_HeightMapTexture.Create();

        if (m_ColorSourceTexture && m_ColorTexture && m_HeightMapSourceTexture && m_HeightMapTexture)
        {
            RenderTexture beforeRT = RenderTexture.active;

            RenderTexture.active = m_HeightMapTexture;
            Graphics.Blit(m_HeightMapSourceTexture, m_HeightMapTexture);

            RenderTexture.active = m_ColorTexture;
            Graphics.Blit(m_ColorSourceTexture, m_ColorTexture);

            RenderTexture.active = beforeRT;
        }
    }

    public void ResetTexture()
    {
        if (m_ColorTexture)
        {
            m_ColorTexture.Release();
            m_ColorTexture = null;
        }

        if (m_HeightMapTexture)
        {
            m_HeightMapTexture.Release();
            m_HeightMapTexture = null;
        }

        CreateMaskTexture();
    }

    public void SetMaskPixel(Vector3[] _PixelDatas, Color _Color)
    {
        if (m_GrassMaskCS == null) return;

        if (m_PositionBuffer != null) m_PositionBuffer.Release();
        m_PositionBuffer = new ComputeBuffer(_PixelDatas.Length, sizeof(float) * 3);
        m_PositionBuffer.SetData(_PixelDatas);

        int kernal = m_GrassMaskCS.FindKernel("CSGGMask");
        m_GrassMaskCS.SetTexture(kernal, "_ResultColorTex", m_ColorTexture);
        m_GrassMaskCS.SetTexture(kernal, "_ResultHeightMap", m_HeightMapTexture);
        m_GrassMaskCS.SetBuffer(kernal, "_PositionBuffers", m_PositionBuffer);
        m_GrassMaskCS.SetFloats("_Color", _Color.r, _Color.g, _Color.b );

        m_GrassMaskCS.Dispatch(kernal, Mathf.Max(_PixelDatas.Length / 8, 8), 1, 1);


        //for (int i = 0; i < _PixelDatas.Length; ++i)
        //{
        //    BitConverter. _PixelDatas[i].
        //}

        m_PixelUpdate = true;
    }

    public void SaveMask()
    {
#if UNITY_EDITOR
        if (m_ColorTexture)
        {
            Texture2D texture2D = ConvertTexture2D(m_ColorTexture);
            byte[] bytes = texture2D.EncodeToPNG();
            m_Texture = texture2D;
            DestroyImmediate(texture2D);

            string parentPath = Directory.GetCurrentDirectory() + "/Assets/Project/GrassSystem";

            if (!Directory.Exists(parentPath))
                Directory.CreateDirectory(parentPath);

            string filePath = parentPath + "/GrassColorTexture.png";
            if (File.Exists(filePath))
                File.Delete(filePath);

            File.WriteAllBytes(filePath, bytes);
        }

        if (m_HeightMapTexture)
        {
            Texture2D texture2D = ConvertTexture2D(m_HeightMapTexture);
            byte[] bytes = texture2D.EncodeToPNG();
            m_Texture = texture2D;
            DestroyImmediate(texture2D);

            string parentPath = Directory.GetCurrentDirectory() + "/Assets/Project/GrassSystem";

            if (!Directory.Exists(parentPath))
                Directory.CreateDirectory(parentPath);

            string filePath = parentPath + "/GrassHeightMapTexture.png";
            if (File.Exists(filePath))
                File.Delete(filePath);

            File.WriteAllBytes(filePath, bytes);
        }

        AssetDatabase.Refresh();
        GrassRenderingSystem.Instance.Refresh();
#endif
    }

    Texture2D ConvertTexture2D(RenderTexture _RenderTexture)
    {
        RenderTexture beforeActive = RenderTexture.active;
        RenderTexture.active = _RenderTexture;
        Texture2D tex = new Texture2D(_RenderTexture.width, _RenderTexture.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, _RenderTexture.width, _RenderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = beforeActive;
        return tex;
    }
}
