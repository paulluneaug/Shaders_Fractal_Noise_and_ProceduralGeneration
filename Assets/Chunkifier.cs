using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Chunkifier : MonoBehaviour
{
    static readonly Vector3Int CHUNK_SPAN = new Vector3Int(2, 2, 2);
    const int CHUNK_SIZE = 4;

    [SerializeField] private Renderer m_cubePrefab;
    [SerializeField] private Color[] m_colors;

    [NonSerialized] private Color[,,] m_datas;
    [NonSerialized] private Vector3Int m_datasSize;
    [NonSerialized] private Color[] m_flatDatas; 
    [NonSerialized] private int m_flatDatasLen;
    [NonSerialized] private Transform m_transform;
    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
        CreateDatas();
        DisplayCubeDatas();


        FlattenDatas();
        DisplayFlatDatas();

        //Chunkify();
    }

    private void CreateDatas()
    {
        m_datas = new Color[CHUNK_SIZE * CHUNK_SPAN.x, CHUNK_SIZE * CHUNK_SPAN.y, CHUNK_SIZE * CHUNK_SPAN.z];
        m_datasSize = new Vector3Int(CHUNK_SIZE * CHUNK_SPAN.x, CHUNK_SIZE * CHUNK_SPAN.y, CHUNK_SIZE * CHUNK_SPAN.z);

        for (int x = 0; x < CHUNK_SPAN.x; x++)
        {
            for (int y = 0; y < CHUNK_SPAN.y; y++)
            {
                for (int z = 0; z < CHUNK_SPAN.z; z++)
                {
                    for (int ix = 0;  ix < CHUNK_SIZE; ix++)
                    {
                        for (int iy = 0; iy < CHUNK_SIZE; iy++)
                        {
                            for (int iz = 0; iz < CHUNK_SIZE; iz++)
                            {
                                m_datas[x * CHUNK_SIZE + ix, y * CHUNK_SIZE + iy, z * CHUNK_SIZE + iz] = m_colors[x + y * CHUNK_SPAN.x + z * CHUNK_SPAN.x * CHUNK_SPAN.y];
                            }
                        }
                    }
                }
            }
        }
    }

    private void DisplayCubeDatas()
    {
        GameObject cubeDatasParent = new GameObject("CubeDatas");
        Transform cubesParent = cubeDatasParent.transform;
        cubesParent.parent = m_transform;
        cubesParent.localPosition = Vector3.zero;

        for (int x = 0; x < m_datasSize.x; x++)
        {
            for (int y = 0; y < m_datasSize.y; y++)
            {
                for (int z = 0; z < m_datasSize.z; z++)
                {
                    CreateCube(x, y, z, m_datas[x, y, z], cubesParent);
                }
            }
        }
    }

    private void FlattenDatas()
    {
        m_flatDatasLen = CHUNK_SIZE * CHUNK_SPAN.x * CHUNK_SIZE * CHUNK_SPAN.y * CHUNK_SIZE * CHUNK_SPAN.z;
        m_flatDatas = new Color[m_flatDatasLen];
        for (int x = 0; x < m_datasSize.x; x++)
        {
            for (int y = 0; y < m_datasSize.y; y++)
            {
                for (int z = 0; z < m_datasSize.z; z++)
                {
                    m_flatDatas[x + y * m_datasSize.x + z * m_datasSize.x * m_datasSize.y] = m_datas[x, y, z];
                }
            }
        }
    }

    private void DisplayFlatDatas()
    {
        GameObject flatDatasParent = new GameObject("FlatDatas");
        Transform cubesParent = flatDatasParent.transform;
        cubesParent.parent = m_transform;
        cubesParent.localPosition = Vector3.forward * -2;

        for (int i = 0; i < m_flatDatasLen; i++)
        {
            CreateCube(i, 0, 0, m_flatDatas[i], cubesParent);
        }
    }


    private void Chunkify()
    {
        
    }

    private void CreateCube(int x, int y, int z, Color color, Transform parent)
    {
        Renderer newCube = Instantiate(m_cubePrefab);
        Transform t = newCube.transform;
        t.parent = parent;
        t.localPosition = new Vector3(x, y, z);
        newCube.material.color = color;
    }

    private Vector3Int GetCoordinatesFromIndex(int index)
    {
        int z = index / (m_datasSize.x * m_datasSize.y);
        int indexY = index % (m_datasSize.x * m_datasSize.y);
        int y = indexY / m_datasSize.x;
        int x = indexY % m_datasSize.x;
        return new Vector3Int(x, y, z);
    }
}
