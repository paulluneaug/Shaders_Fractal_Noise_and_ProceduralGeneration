using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Chunkifier : MonoBehaviour
{
    // Singleton stuff
    public static Chunkifier Instance 
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindObjectOfType<Chunkifier>();
            }
            return s_instance;
        }
    }
    [NonSerialized] private static Chunkifier s_instance = null;

    [SerializeField] private Vector3Int m_chunkSpan = new Vector3Int(2, 2, 2);
    [SerializeField] private int m_chunkSize = 4;

    [SerializeField] private Renderer m_cubePrefab;
    [SerializeField] private Color[] m_colors;

    [NonSerialized] private Color[,,] m_datas;
    [NonSerialized] private Vector3Int m_datasSize;
    [NonSerialized] private Color[] m_flatDatas;
    [NonSerialized] private Color[] m_chunkifiedFlatDatas;
    [NonSerialized] private int m_flatDatasLen;
    [NonSerialized] private Transform m_transform;

    [NonSerialized] private List<GameObject> m_dataVisusParents = new List<GameObject>();

    [NonSerialized] private MaterialPropertyBlock m_propertyblock;

    [NonSerialized] private ChunkifierUtils m_utils;


    // Start is called before the first frame update
    void Start()
    {
        PlayWithDatas();
    }

    public void PlayWithDatas()
    {
        m_transform = transform;
        m_propertyblock = new MaterialPropertyBlock();
        m_utils = new ChunkifierUtils(m_chunkSize, (m_chunkSpan.x, m_chunkSpan.y, m_chunkSpan.z));

        CreateDatas();
        DisplayCubeDatas();

        FlattenDatas();
        DisplayFlatDatas(m_flatDatas, -2, "FlatDatas");

        FlattenAndChunkifyDatas();
        DisplayFlatDatas(m_chunkifiedFlatDatas, -4, "ChunkifiedFlatDatas");

        //Chunkify();
    }


    private void CreateDatas()
    {
        m_datas = new Color[m_chunkSize * m_chunkSpan.x, m_chunkSize * m_chunkSpan.y, m_chunkSize * m_chunkSpan.z];
        m_datasSize = new Vector3Int(m_chunkSize * m_chunkSpan.x, m_chunkSize * m_chunkSpan.y, m_chunkSize * m_chunkSpan.z);

        for (int x = 0; x < m_chunkSpan.x; x++)
        {
            for (int y = 0; y < m_chunkSpan.y; y++)
            {
                for (int z = 0; z < m_chunkSpan.z; z++)
                {
                    for (int ix = 0;  ix < m_chunkSize; ix++)
                    {
                        for (int iy = 0; iy < m_chunkSize; iy++)
                        {
                            for (int iz = 0; iz < m_chunkSize; iz++)
                            {
                                (int x, int y, int z) coords = (x * m_chunkSize + ix, y * m_chunkSize + iy, z * m_chunkSize + iz);
                                float colorFactor = Mathf.Lerp(1.0f, 0.4f, m_utils.LocalCoordinatesToChunkifiedLocalOffset(m_utils.CoordinatesToLocalCoordinates(coords)) / (float)m_utils.ChunkVolume);
                                m_datas[coords.x, coords.y, coords.z] = m_colors[x + y * m_chunkSpan.x + z * m_chunkSpan.x * m_chunkSpan.y] * colorFactor;
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
        m_dataVisusParents.Add(cubeDatasParent);

        for (int x = 0; x < m_datasSize.x; x++)
        {
            for (int y = 0; y < m_datasSize.y; y++)
            {
                for (int z = 0; z < m_datasSize.z; z++)
                {
                    CreateCube(x, y, z, m_datas[x, y, z], cubesParent, $"Cube_{m_utils.CoordinatesToChunkifiedIndex((x, y, z))}");
                }
            }
        }
    }

    private void FlattenDatas()
    {
        m_flatDatasLen = m_chunkSize * m_chunkSpan.x * m_chunkSize * m_chunkSpan.y * m_chunkSize * m_chunkSpan.z;
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
    private void FlattenAndChunkifyDatas()
    {
        m_chunkifiedFlatDatas = new Color[m_flatDatasLen];
        for (int x = 0; x < m_datasSize.x; x++)
        {
            for (int y = 0; y < m_datasSize.y; y++)
            {
                for (int z = 0; z < m_datasSize.z; z++)
                {
                    m_chunkifiedFlatDatas[m_utils.CoordinatesToChunkifiedIndex((x, y, z))] = m_datas[x, y, z];
                }
            }
        }
    }

    private void DisplayFlatDatas(Color[] flatDatas, int zOffset, string name)
    {
        GameObject flatDatasParent = new GameObject(name);
        Transform cubesParent = flatDatasParent.transform;
        cubesParent.parent = m_transform;
        cubesParent.localPosition = Vector3.forward * zOffset;
        m_dataVisusParents.Add(flatDatasParent);

        for (int i = 0; i < m_flatDatasLen; i++)
        {
            CreateCube(i, 0, 0, flatDatas[i], cubesParent, $"FlatCube_{i}");
        }
    }


    private void Chunkify()
    {
        
    }

    private void CreateCube(int x, int y, int z, Color color, Transform parent, string name)
    {
        Renderer newCube = Instantiate(m_cubePrefab);
        newCube.name = name;
        Transform t = newCube.transform;
        t.parent = parent;
        t.localPosition = new Vector3(x, y, z);

        newCube.GetPropertyBlock(m_propertyblock);
        m_propertyblock.SetColor("_BaseColor", color);
        newCube.SetPropertyBlock(m_propertyblock);
    }

    public void ClearCubes()
    {
        Action<GameObject> destroyAction = Application.isPlaying ? Destroy : DestroyImmediate;
        foreach (GameObject parent in m_dataVisusParents)
        {
            destroyAction(parent);
        }
        m_dataVisusParents.Clear();
    }

    [MenuItem("CONTEXT/Chunkifier/PlayWithDatas")]
    public static void PlayWithDatasEditor()
    {
        Instance.PlayWithDatas();
    }

    [MenuItem("CONTEXT/Chunkifier/ClearCubes")]
    public static void ClearCubesEditor()
    {
        Instance.ClearCubes();
    }
}
