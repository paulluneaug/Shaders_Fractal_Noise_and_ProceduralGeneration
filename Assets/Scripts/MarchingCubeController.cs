using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;

using static Constants;
using static MeshStructs;

public class MarchingCubeController : MonoBehaviour
{
    #region Structs
    [Serializable]
    private struct NoiseLayer3D
    {
        public bool Enabled;

        public int NoiseScale;
        public bool UseSmootherStep;
        public int GradientOffset;
        public float LayerWeight;

        public GPUNoiseLayer3D ToGPUNoiseLayer()
        {
            return new GPUNoiseLayer3D(
                LayerWeight,
                GradientOffset,
                NoiseScale,
                UseSmootherStep ? 1 : 0);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GPUNoiseLayer3D
    {
        public float LayerWeigth;

        public int GradientOffset;
        public int NoiseScale;

        public int UseSmootherStep;

        public GPUNoiseLayer3D(float layerWeigth, int gradientOffset, int noiseScale, int useSmootherStep)
        {
            LayerWeigth = layerWeigth;
            GradientOffset = gradientOffset;
            NoiseScale = noiseScale;
            UseSmootherStep = useSmootherStep;
        }
    }

    #endregion

    #region Shader Properties Names
    // MarchingCube CS
    private const string NOISE_KERNEL_NAME = "ComputeNoise";
    private const string MARCHING_CUBE_KERNEL_NAME = "MarchingCubes";
    private const string CHUNKIFY_CELLS_KERNEL_NAME = "ChunkifyCells";

    private const string NOISE_TEXTURE = "_NoiseTexture";
    private const string GENERATED_CELLS = "_GeneratedCells";
    private const string GENERATED_CHUNKS = "_GeneratedChunks";

    private const string CHUNK_ZONE_TO_GENERATE_SIZE = "_ChunkZoneSizeToGenerate";
    private const string CHUNK_OFFSET = "_ChunkOffset";

    private const string THRESHOLD = "_Threshold";

    private const string NOISE_LAYERS_COUNT = "_NoiseLayersCount";
    private const string NOISE_LAYERS = "_NoiseLayers";
    private const string NOISE_WEIGHTS_MULTIPLIER = "_NoiseWeigthsMultiplier";
    #endregion

    #region Properties
    private Vector3Int CellOffset => m_chunkOffset * CHUNK_SIZE;

    private Vector3Int CellsToGenerateSize => m_chunkZoneSizeToGenerate * CHUNK_SIZE;
    #endregion

    #region Serialized Fields
    [SerializeField] private ComputeShader m_marchingCubeCS = null;
    [SerializeField] private ComputeShader m_meshSimplifierCS = null;

    [SerializeField] private Vector3Int m_chunkOffset = Vector3Int.zero;
    [SerializeField] private Vector3Int m_chunkZoneSizeToGenerate = Vector3Int.one * 4;

    [SerializeField, Range(0.0f, 1.0f)] private float m_threshold = 0.5f;

    [SerializeField] private NoiseLayer3D[] m_noiseLayers = null;

    [SerializeField] private Material m_material = null;

    [Header("Gizmos")]
    [SerializeField] private Axis m_axisToDrawChunkBorders = Axis.X | Axis.Y | Axis.Z;
    [SerializeField] private Color m_chunksBorderColor = Color.red;
    [SerializeField] private Axis m_axisToDrawCellBorders = Axis.X | Axis.Y | Axis.Z;
    [SerializeField] private Color m_cellsBorderColor = Color.yellow;
    #endregion

    // Cache
    #region Shader Properties IDs
    // MarchingCube CS
    [NonSerialized] private int m_noiseKernelID = 0;
    [NonSerialized] private int m_marchingCubeKernelID = 0;
    [NonSerialized] private int m_chunkifyCellsKernelID = 0;

    [NonSerialized] private int m_noiseTexturePropertyID = 0;
    [NonSerialized] private int m_generatedCellsPropertyID = 0;
    [NonSerialized] private int m_generatedChunksPropertyID = 0;

    [NonSerialized] private int m_chunkZoneSizeToGeneratePropertyID = 0;
    [NonSerialized] private int m_chunkOffsetPropertyID = 0;

    [NonSerialized] private int m_thresholdID = 0;

    [NonSerialized] private int m_noiseLayerCountPropertyID = 0;
    [NonSerialized] private int m_noiseLayersPropertyID = 0;
    [NonSerialized] private int m_noiseWeightsMultiplierPropertyID = 0;
    #endregion

    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;

    [NonSerialized] private RenderTexture m_noiseTexture = null;
    [NonSerialized] private ComputeBuffer m_noiseLayersBuffer = null;
    [NonSerialized] private ComputeBuffer m_generatedCellsBuffer = null;
    [NonSerialized] private ComputeBuffer m_generatedChunksBuffer = null;

    [NonSerialized] private Transform m_transform = null;

    [NonSerialized] private ChunkifierUtils m_utils;

    [NonSerialized] private CellMesh[] m_generatedCells = null;
    [NonSerialized] private ChunkMesh[] m_generatedChunks = null;
    [NonSerialized] private CPUChunkMesh[] m_generatedCPUChunks = null;

    private void Awake()
    {
        GetPropertiesIDs();

        m_recorder = new ScriptExecutionTimeRecorder();

        m_transform = transform;

        UpdateShaderProperty();
    }

    private void GetPropertiesIDs()
    {
        // MarchingCube CS
        m_noiseKernelID = m_marchingCubeCS.FindKernel(NOISE_KERNEL_NAME);
        m_marchingCubeKernelID = m_marchingCubeCS.FindKernel(MARCHING_CUBE_KERNEL_NAME);
        m_chunkifyCellsKernelID = m_marchingCubeCS.FindKernel(CHUNKIFY_CELLS_KERNEL_NAME);

        m_noiseTexturePropertyID = Shader.PropertyToID(NOISE_TEXTURE);
        m_generatedCellsPropertyID = Shader.PropertyToID(GENERATED_CELLS);
        m_generatedChunksPropertyID = Shader.PropertyToID(GENERATED_CHUNKS);

        m_chunkZoneSizeToGeneratePropertyID = Shader.PropertyToID(CHUNK_ZONE_TO_GENERATE_SIZE);
        m_chunkOffsetPropertyID = Shader.PropertyToID(CHUNK_OFFSET);

        m_thresholdID = Shader.PropertyToID(THRESHOLD);

        m_noiseLayersPropertyID = Shader.PropertyToID(NOISE_LAYERS);
        m_noiseLayerCountPropertyID = Shader.PropertyToID(NOISE_LAYERS_COUNT);
        m_noiseWeightsMultiplierPropertyID = Shader.PropertyToID(NOISE_WEIGHTS_MULTIPLIER);

    }

    private void UpdateShaderProperty()
    {
        m_recorder.Reset();

        SetMarchingCubeShaderProperties();

        m_recorder.AddEvent("Shader properties assignation");

        int groupX = Mathf.CeilToInt(CellsToGenerateSize.x + 1 / 8.0f);
        int groupY = Mathf.CeilToInt(CellsToGenerateSize.y + 1 / 8.0f);
        int groupZ = Mathf.CeilToInt(CellsToGenerateSize.z + 1 / 8.0f);
        m_marchingCubeCS.Dispatch(m_noiseKernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Noise Shader Dispatch");

        groupX = Mathf.CeilToInt(CellsToGenerateSize.x / 8.0f);
        groupY = Mathf.CeilToInt(CellsToGenerateSize.y / 8.0f);
        groupZ = Mathf.CeilToInt(CellsToGenerateSize.z / 8.0f);
        m_marchingCubeCS.Dispatch(m_marchingCubeKernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Marching cubes Shader Dispatch");

        groupX = m_chunkZoneSizeToGenerate.x;
        groupY = m_chunkZoneSizeToGenerate.y;
        groupZ = m_chunkZoneSizeToGenerate.z;
        m_marchingCubeCS.Dispatch(m_chunkifyCellsKernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Chunkify Mesh Shader Dispatch");

        m_generatedChunks = new ChunkMesh[m_chunkZoneSizeToGenerate.x * m_chunkZoneSizeToGenerate.y * m_chunkZoneSizeToGenerate.z];
        m_generatedChunksBuffer.GetData(m_generatedChunks);

        m_recorder.AddEvent("Chunk Meshes acquisition from shader");

        //m_generatedCells = new CellMesh[CellsToGenerateSize.x * CellsToGenerateSize.y * CellsToGenerateSize.z];
        //m_generatedCellsBuffer.GetData(m_generatedCells);
        //m_recorder.AddEvent("Mesh acquisition from shader");

        //ChunkifyMeshes();

        //m_recorder.AddEvent("Chunkify Operation");

        for (int i = 0; i < m_generatedChunks.Length; i++)
        {
            CreateMesh(m_generatedChunks[i], i);
        }

        m_recorder.AddEvent("Meshes Generation");

        m_recorder.LogAllEventsTimeSpan();
    }

    private void SetMarchingCubeShaderProperties()
    {
        GPUNoiseLayer3D[] gpuNoiseLayers = m_noiseLayers
            .Where(layer => layer.Enabled)
            .Select(layer => layer.ToGPUNoiseLayer())
            .ToArray();
        int layerCount = gpuNoiseLayers.Length;
        float weightMultiplier = 1.0f / gpuNoiseLayers.Select(layer => layer.LayerWeigth).Sum();

        // Cells Meshes Buffer
        m_generatedCellsBuffer?.Release();
        m_generatedCellsBuffer = new ComputeBuffer(CellsToGenerateSize.x * CellsToGenerateSize.y * CellsToGenerateSize.z, Marshal.SizeOf(typeof(CellMesh)));

        m_marchingCubeCS.SetBuffer(m_marchingCubeKernelID, m_generatedCellsPropertyID, m_generatedCellsBuffer);
        m_marchingCubeCS.SetBuffer(m_chunkifyCellsKernelID, m_generatedCellsPropertyID, m_generatedCellsBuffer);

        // Cells Meshes Buffer
        m_generatedChunksBuffer?.Release();
        m_generatedChunksBuffer = new ComputeBuffer(m_chunkZoneSizeToGenerate.x * m_chunkZoneSizeToGenerate.y * m_chunkZoneSizeToGenerate.z, Marshal.SizeOf(typeof(ChunkMesh)));

        m_marchingCubeCS.SetBuffer(m_chunkifyCellsKernelID, m_generatedChunksPropertyID, m_generatedChunksBuffer);

        // Other variables
        m_marchingCubeCS.SetFloat(m_thresholdID, m_threshold);
        m_marchingCubeCS.SetInts(m_chunkZoneSizeToGeneratePropertyID, m_chunkZoneSizeToGenerate.x, m_chunkZoneSizeToGenerate.y, m_chunkZoneSizeToGenerate.z);
        m_marchingCubeCS.SetInts(m_chunkOffsetPropertyID, m_chunkOffset.x, m_chunkOffset.y, m_chunkOffset.z);

        // Noise Texture
        m_noiseTexture = new RenderTexture(CellsToGenerateSize.x + 1, CellsToGenerateSize.y + 1, 0, RenderTextureFormat.RFloat);
        m_noiseTexture.dimension = TextureDimension.Tex3D;
        m_noiseTexture.volumeDepth = CellsToGenerateSize.z + 1;
        m_noiseTexture.enableRandomWrite = true;

        m_marchingCubeCS.SetTexture(m_noiseKernelID, m_noiseTexturePropertyID, m_noiseTexture);
        m_marchingCubeCS.SetTexture(m_marchingCubeKernelID, m_noiseTexturePropertyID, m_noiseTexture);

        // Layers
        m_marchingCubeCS.SetInt(m_noiseLayerCountPropertyID, layerCount);

        m_noiseLayersBuffer?.Release();
        m_noiseLayersBuffer = new ComputeBuffer(layerCount, Marshal.SizeOf(typeof(GPUNoiseLayer3D)));
        m_noiseLayersBuffer.SetData(gpuNoiseLayers);
        m_marchingCubeCS.SetBuffer(m_noiseKernelID, m_noiseLayersPropertyID, m_noiseLayersBuffer);

        m_marchingCubeCS.SetFloat(m_noiseWeightsMultiplierPropertyID, weightMultiplier);
    }

    #region Mesh Creation
    private void ChunkifyMeshes()
    {
        int chunkCount = m_chunkZoneSizeToGenerate.x * m_chunkZoneSizeToGenerate.y * m_chunkZoneSizeToGenerate.z;
        m_generatedCPUChunks = new CPUChunkMesh[chunkCount];
        m_utils = new ChunkifierUtils(CHUNK_SIZE, (m_chunkZoneSizeToGenerate.x, m_chunkZoneSizeToGenerate.y, m_chunkZoneSizeToGenerate.z));
        Parallel.For(0, chunkCount, (int chunkIndex) => ChunkifyCellsForChunk(chunkIndex));

    }

    private void CreateMesh(IMesh meshStruct, int index)
    {
        Vector3Int coordinates = GetCoordinatesFromIndex(index, m_chunkZoneSizeToGenerate) * CHUNK_SIZE;
        Vector3 meshPos = CellOffset + coordinates;
        GameObject go = new GameObject($"Mesh_{index}{meshPos}");
        Transform t = go.transform;
        t.position = meshPos;

        t.parent = m_transform;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.material = m_material;

        MeshFilter filter = go.AddComponent<MeshFilter>();

        filter.mesh = meshStruct.GetMesh();
    }

    private Vector3Int GetCoordinatesFromIndex(int index, Vector3Int zone)
    {
#if true

        int z = index / (zone.x * zone.y);
        int indexY = index % (zone.x * zone.y);
        int y = indexY / zone.x;
        int x = indexY % zone.x;
#else
        (int x, int y, int z) = m_utils.ChunkifiedOffsetToCoordinates(index);
#endif
        return new Vector3Int(x, y, z);

    }
    #endregion

    private void ChunkifyCellsForChunk(int chunkIndex)
    {
        int chunkOffset = chunkIndex * CHUNK_VOLUME;

        Vector3[] chunkVertices = new Vector3[12 * CHUNK_VOLUME];
        int[] chunkTriangles = new int[12 * CHUNK_VOLUME];

        int[] vertexMap = new int[12 * CHUNK_VOLUME];
        int nextVertexIndex = 0;
        int nextTriangleIndex = 0;

        for (int i = 0; i < CHUNK_VOLUME; ++i)
        {
            CellMesh currentMesh = m_generatedCells[chunkOffset + i];
            Vector3[] currentVertices = currentMesh.GetVertices();
            int[] currentTriangles = currentMesh.GetTriangles();

            int currentMeshMinVerticeIndex = i * 12;
            int currentMeshMaxVerticeIndex = (i + 1) * 12 - 1;

            for (uint j = 0; j < 12; j++)
            {
                int rawVertexIndex = currentTriangles[j];
                if (rawVertexIndex == -1)
                {
                    break;
                }

                if (rawVertexIndex >= 12 * CHUNK_VOLUME)
                {
                    Debug.LogError($"{rawVertexIndex}");
                }


                if (rawVertexIndex.Between(currentMeshMinVerticeIndex, currentMeshMaxVerticeIndex))
                {
                    if (vertexMap[rawVertexIndex] == 0)
                    {
                        chunkVertices[nextVertexIndex++] = currentVertices[rawVertexIndex % 12] + GetCoordinatesFromIndex(rawVertexIndex / 12, Vector3Int.one * CHUNK_SIZE);

                        vertexMap[rawVertexIndex] = nextVertexIndex;
                    }
                }
            }
        }

        for (int i = 0; i < CHUNK_VOLUME; ++i)
        {
            CellMesh currentMesh = m_generatedCells[chunkOffset + i];
            int[] currentTriangles = currentMesh.GetTriangles();
            for (uint j = 0; j < 12; j++)
            {
                int rawVertexIndex = currentTriangles[j];
                if (rawVertexIndex == -1)
                {
                    break;
                }
                if (vertexMap[rawVertexIndex] - 1 == -1)
                {
                    Debug.Log(rawVertexIndex);
                }
                chunkTriangles[nextTriangleIndex++] = vertexMap[rawVertexIndex] - 1;
            }
        }

        CPUChunkMesh chunk = new CPUChunkMesh(chunkVertices.Resize(nextVertexIndex), chunkTriangles.Resize(nextTriangleIndex));

        m_generatedCPUChunks[chunkIndex] = chunk;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = m_cellsBorderColor;
        if ((m_axisToDrawCellBorders & Axis.X) == Axis.X)
        {
            for (int xy_x = 1; xy_x < CellsToGenerateSize.x; ++xy_x)
            {
                for (int xy_y = 1; xy_y < CellsToGenerateSize.y; ++xy_y)
                {
                    DrawGizmoLineInLocalSpace(m_chunkOffset + new Vector3(xy_x, xy_y, 0), m_chunkOffset + new Vector3(xy_x, xy_y, CellsToGenerateSize.z));
                }
            }

        }

        if ((m_axisToDrawCellBorders & Axis.Y) == Axis.Y)
        {
            for (int xz_x = 1; xz_x < CellsToGenerateSize.x; ++xz_x)
            {
                for (int xz_z = 1; xz_z < CellsToGenerateSize.z; ++xz_z)
                {
                    DrawGizmoLineInLocalSpace(m_chunkOffset + new Vector3(xz_x, 0, xz_z), m_chunkOffset + new Vector3(xz_x, CellsToGenerateSize.y, xz_z));
                }
            }

        }

        if ((m_axisToDrawCellBorders & Axis.Z) == Axis.Z)
        {
            for (int yz_y = 1; yz_y < CellsToGenerateSize.y; ++yz_y)
            {
                for (int yz_z = 1; yz_z < CellsToGenerateSize.z; ++yz_z)
                {
                    DrawGizmoLineInLocalSpace(m_chunkOffset + new Vector3(0, yz_y, yz_z), m_chunkOffset + new Vector3(CellsToGenerateSize.x, yz_y, yz_z));
                }
            }
        }

        Gizmos.color = m_chunksBorderColor;
        if ((m_axisToDrawChunkBorders & Axis.X) == Axis.X)
        {
            for (int xy_x = 0; xy_x < m_chunkZoneSizeToGenerate.x + 1; ++xy_x)
            {
                for (int xy_y = 0; xy_y < m_chunkZoneSizeToGenerate.y + 1; ++xy_y)
                {
                    DrawGizmoLineInLocalSpace(m_chunkOffset + new Vector3(xy_x, xy_y, 0) * CHUNK_SIZE, m_chunkOffset + new Vector3(xy_x, xy_y, m_chunkZoneSizeToGenerate.z) * CHUNK_SIZE);
                }
            }

        }

        if ((m_axisToDrawChunkBorders & Axis.Y) == Axis.Y)
        {
            for (int xz_x = 0; xz_x < m_chunkZoneSizeToGenerate.x + 1; ++xz_x)
            {
                for (int xz_z = 0; xz_z < m_chunkZoneSizeToGenerate.z + 1; ++xz_z)
                {
                    DrawGizmoLineInLocalSpace(m_chunkOffset + new Vector3(xz_x, 0, xz_z) * CHUNK_SIZE, m_chunkOffset + new Vector3(xz_x, m_chunkZoneSizeToGenerate.y, xz_z) * CHUNK_SIZE);
                }
            }

        }

        if ((m_axisToDrawChunkBorders & Axis.Z) == Axis.Z)
        {
            for (int yz_y = 0; yz_y < m_chunkZoneSizeToGenerate.y + 1; ++yz_y)
            {
                for (int yz_z = 0; yz_z < m_chunkZoneSizeToGenerate.z + 1; ++yz_z)
                {
                    DrawGizmoLineInLocalSpace(m_chunkOffset + new Vector3(0, yz_y, yz_z) * CHUNK_SIZE, m_chunkOffset + new Vector3(m_chunkZoneSizeToGenerate.x, yz_y, yz_z) * CHUNK_SIZE);
                }
            }
        }
    }

    private void DrawGizmoLineInLocalSpace(Vector3 from, Vector3 to)
    {
        Gizmos.DrawLine(transform.TransformPoint(from), transform.TransformPoint(to));
    }
#endif
}

public static class Extensions
{
    public unsafe static T[] Resize<T>(this T[] array, int size) where T : unmanaged
    {
        T[] result = new T[size];

        if (size == 0 ||  array.Length == 0)
        {
            return result;
        }

        fixed (T* arrayRawPtr = &array[0])
        {
            fixed (T* resultRawPtr = &result[0])
            {
                Buffer.MemoryCopy(arrayRawPtr, resultRawPtr, size * sizeof(T), Math.Min(array.Length, size) * sizeof(T));
            }
        }

        return result;
    }

    public static bool Between<TVal, TComp>(this TVal x, TComp a, TComp b, bool includeBounds = true) 
        where TVal : IComparable<TComp> 
        where TComp : IComparable<TVal>
    {
        return includeBounds ? a.SmallerEqualsThan(x) && x.SmallerEqualsThan(b) : a.SmallerThan(x) && x.SmallerThan(b);
    }

    public static bool GreaterThan<TVal, TComp>(this TVal x, TComp a) where TVal : IComparable<TComp>
    {
        return x.CompareTo(a) == 1;
    }

    public static bool GreaterEqualsThan<TVal, TComp>(this TVal x, TComp a) where TVal : IComparable<TComp>
    {
        return x.CompareTo(a) != -1;
    }

    public static bool SmallerThan<TVal, TComp>(this TVal x, TComp a) where TVal : IComparable<TComp>
    {
        return x.CompareTo(a) == -1;
    }

    public static bool SmallerEqualsThan<TVal, TComp>(this TVal x, TComp a) where TVal : IComparable<TComp>
    {
        return x.CompareTo(a) != 1;
    }
}
