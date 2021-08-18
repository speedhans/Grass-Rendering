using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GrassData 
{
    public Vector3[] m_GrassPositions;
}

public class QuadTree
{
    public class QuadUnit
    {
        public GrassData m_GrassData;
        public int m_Depth;
        public Rect m_Rect;
        public List<QuadUnit> m_ListQuadUnit;

        public void Initialize(Rect _Rect, int _CurrentDepth, int _TargetDepth)
        {
            m_Rect = _Rect;
            m_Depth = _CurrentDepth;
            if (_CurrentDepth >= _TargetDepth)
            {
                // Final Depth Event
                m_GrassData = new GrassData();
                m_GrassData.m_GrassPositions = new Vector3[15];
                for (int i = 0; i < m_GrassData.m_GrassPositions.Length; ++i)
                {
                    m_GrassData.m_GrassPositions[i] = new Vector3(m_Rect.position.x + Mathf.Cos(i + m_Rect.position.x * 0.42f) * (m_Rect.width * 0.45f), 0, m_Rect.position.y + Mathf.Sin(i + m_Rect.position.y * 4.15f) * (m_Rect.height * 0.45f));
                }
                return;
            }

            m_ListQuadUnit = new List<QuadUnit>();

            float sizeX = m_Rect.width * 0.5f;
            float sizeY = m_Rect.height * 0.5f;

            float x = m_Rect.x - (sizeX * 0.5f);
            float y = m_Rect.y + (sizeY * 0.5f);

            QuadUnit unit = new QuadUnit();
            unit.Initialize(new Rect(x, y, sizeX, sizeY), _CurrentDepth + 1, _TargetDepth);
            m_ListQuadUnit.Add(unit);

            x = m_Rect.x + (sizeX * 0.5f);
            y = m_Rect.y + (sizeY * 0.5f);
            unit = new QuadUnit();
            unit.Initialize(new Rect(x, y, sizeX, sizeY), _CurrentDepth + 1, _TargetDepth);
            m_ListQuadUnit.Add(unit);

            x = m_Rect.x + (sizeX * 0.5f);
            y = m_Rect.y - (sizeY * 0.5f);
            unit = new QuadUnit();
            unit.Initialize(new Rect(x, y, sizeX, sizeY), _CurrentDepth + 1, _TargetDepth);
            m_ListQuadUnit.Add(unit);

            x = m_Rect.x - (sizeX * 0.5f);
            y = m_Rect.y - (sizeY * 0.5f);
            unit = new QuadUnit();
            unit.Initialize(new Rect(x, y, sizeX, sizeY), _CurrentDepth + 1, _TargetDepth);
            m_ListQuadUnit.Add(unit);
        }

        public List<QuadUnit> GetUnitList() { return m_ListQuadUnit; }
        public bool IsFinal() { return m_ListQuadUnit == null; }
        public int GetGrassCount() { return m_GrassData == null ? 0 : m_GrassData.m_GrassPositions.Length; }

        public void SearchUnits(float _Distance, Vector2 _Location, ref List<QuadUnit> _Return)
        {
            if (Vector2.Distance(m_Rect.position, _Location) - (m_Rect.width + m_Rect.height) * 0.5f <= _Distance)
            {
                if (IsFinal())
                {
                    _Return.Add(this);
                }
                else
                {
                    for (int i = 0; i < m_ListQuadUnit.Count; ++i)
                    {
                        m_ListQuadUnit[i].SearchUnits(_Distance, _Location, ref _Return);
                    }
                }
            }
            else
            {
                return;
            }
        }

        public void GetAllFinalUnits(ref List<QuadUnit> _Return)
        {
            if (IsFinal())
            {
                _Return.Add(this);
                return;
            }
            else
            {
                for (int i = 0; i < m_ListQuadUnit.Count; ++i)
                {
                    m_ListQuadUnit[i].GetAllFinalUnits(ref _Return);
                }
            }
        }
    }

    Rect m_Rect;
    List<QuadUnit> m_ListQuadUnit = new List<QuadUnit>();

    public void Initialize(Rect _Rect, int _Depth)
    {
        if (_Depth < 1)
        {
            return;
        }
        if (_Rect.width < 1 | _Rect.height < 1)
        {
            return;
        }

        m_Rect = _Rect;
        QuadUnit unit = new QuadUnit();
        unit.Initialize(m_Rect, 1, _Depth);
        m_ListQuadUnit.Add(unit);
    }

    public List<QuadUnit> GetUnitList() { return m_ListQuadUnit; }

    public void SearchUnits(float _Distance, Vector2 _Location, ref List<QuadUnit> _Return)
    {
        for (int i = 0; i < m_ListQuadUnit.Count; ++i)
        {
            m_ListQuadUnit[i].SearchUnits(_Distance, _Location, ref _Return);
        }
    }

    public void GetAllFinalUnits(ref List<QuadUnit> _Return)
    {
        for (int i = 0; i < m_ListQuadUnit.Count; ++i)
        {
            m_ListQuadUnit[i].GetAllFinalUnits(ref _Return);
        }
    }
}
