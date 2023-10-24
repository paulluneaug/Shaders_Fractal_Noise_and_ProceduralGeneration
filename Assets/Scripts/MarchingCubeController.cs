using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
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

    private const string NOISE_TEXTURE = "_NoiseTexture";
    private const string GENERATED_MESHES = "_GeneratedCells";

    private const string CHUNK_ZONE_TO_GENERATE_SIZE = "_ChunkZoneSizeToGenerate";
    private const string CHUNK_OFFSET = "_ChunkOffset";

    private const string THRESHOLD = "_Threshold";

    private const string NOISE_LAYERS_COUNT = "_NoiseLayersCount";
    private const string NOISE_LAYERS = "_NoiseLayers";
    private const string NOISE_WEIGHTS_MULTIPLIER = "_NoiseWeigthsMultiplier";

    // MeshSimplifier CS
    private const string CHUNKIFY_MESHES_KERNEL_NAME = "ChunkifyMeshes";
    private const string SIMPLIFY_CHUNKS_KERNEL_NAME = "SimplifyChunks";

    private const string REORGANIZED_MESHES_INDEX_MAP = "_ReorganizedMeshesIndexMap";
    private const string RESULT_CHUNKS = "_ResultChunks";
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
    #endregion

    // Cache
    #region Shader Properties IDs
    // MarchingCube CS
    [NonSerialized] private int m_noiseKernelID = 0;
    [NonSerialized] private int m_marchingCubeKernelID = 0;

    [NonSerialized] private int m_noiseTexturePropertyID = 0;
    [NonSerialized] private int m_generatedMeshesPropertyID = 0;

    [NonSerialized] private int m_chunkZoneSizeToGeneratePropertyID = 0;
    [NonSerialized] private int m_chunkOffsetPropertyID = 0;

    [NonSerialized] private int m_thresholdID = 0;

    [NonSerialized] private int m_noiseLayerCountPropertyID = 0;
    [NonSerialized] private int m_noiseLayersPropertyID = 0;
    [NonSerialized] private int m_noiseWeightsMultiplierPropertyID = 0;

    // MeshSimplifier CS
    [NonSerialized] private int m_chunkifyMeshesKernelID = 0;
    [NonSerialized] private int m_simplifyChunksKernelID = 0;

    [NonSerialized] private int m_reorganizedMeshesIndexMapPropertyID = 0;
    [NonSerialized] private int m_resultChunksPropertyID = 0;
    #endregion

    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;

    [NonSerialized] private RenderTexture m_noiseTexture = null;
    [NonSerialized] private ComputeBuffer m_noiseLayersBuffer = null;
    [NonSerialized] private ComputeBuffer m_generatedMeshesBuffer = null;
    [NonSerialized] private ComputeBuffer m_reorganizedMeshesIndexMapBuffer = null;

    [NonSerialized] private Transform m_transform = null;

    [NonSerialized] private ChunkifierUtils m_utils;

    [NonSerialized] private CellMesh[] m_generatedCells = null;
    [NonSerialized] private ChunkMesh[] m_generatedChunks = null;

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

        m_noiseTexturePropertyID = Shader.PropertyToID(NOISE_TEXTURE);
        m_generatedMeshesPropertyID = Shader.PropertyToID(GENERATED_MESHES);

        m_chunkZoneSizeToGeneratePropertyID = Shader.PropertyToID(CHUNK_ZONE_TO_GENERATE_SIZE);
        m_chunkOffsetPropertyID = Shader.PropertyToID(CHUNK_OFFSET);

        m_thresholdID = Shader.PropertyToID(THRESHOLD);

        m_noiseLayersPropertyID = Shader.PropertyToID(NOISE_LAYERS);
        m_noiseLayerCountPropertyID = Shader.PropertyToID(NOISE_LAYERS_COUNT);
        m_noiseWeightsMultiplierPropertyID = Shader.PropertyToID(NOISE_WEIGHTS_MULTIPLIER);

        // MeshSimplifier CS
        m_chunkifyMeshesKernelID = m_meshSimplifierCS.FindKernel(CHUNKIFY_MESHES_KERNEL_NAME);
        m_simplifyChunksKernelID = m_meshSimplifierCS.FindKernel(SIMPLIFY_CHUNKS_KERNEL_NAME);

        m_reorganizedMeshesIndexMapPropertyID = Shader.PropertyToID(REORGANIZED_MESHES_INDEX_MAP);
        m_resultChunksPropertyID = Shader.PropertyToID(RESULT_CHUNKS);
    }

    private void UpdateShaderProperty()
    {
        m_recorder.Reset();

        SetMarchingCubeShaderProperties();
        SetMeshSimplifierShaderProperties();

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

        m_meshSimplifierCS.Dispatch(m_chunkifyMeshesKernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Chunkify Mesh Shader Dispatch");

        m_generatedCells = new CellMesh[CellsToGenerateSize.x * CellsToGenerateSize.y * CellsToGenerateSize.z];
        m_generatedMeshesBuffer.GetData(m_generatedCells);
        m_recorder.AddEvent("Mesh acquisition from shader");

        ChunkifyMeshes(m_generatedCells);

        m_recorder.AddEvent("Chunkify Operation");

        for (int i = 0; i < m_generatedChunks.Length; i++)
        {
            CreateMesh(m_generatedChunks[i], i);
        }

        m_recorder.AddEvent("Meshes Generation");

        m_recorder.LogAllEventsTimeSpan();
    }

    private void SetMeshSimplifierShaderProperties()
    {
        m_meshSimplifierCS.SetBuffer(m_chunkifyMeshesKernelID, m_generatedMeshesPropertyID, m_generatedMeshesBuffer);

        m_meshSimplifierCS.SetInts(m_chunkZoneSizeToGeneratePropertyID, m_chunkZoneSizeToGenerate.x, m_chunkZoneSizeToGenerate.y, m_chunkZoneSizeToGenerate.z);

        m_reorganizedMeshesIndexMapBuffer?.Release();
        m_reorganizedMeshesIndexMapBuffer = new ComputeBuffer(CellsToGenerateSize.x * CellsToGenerateSize.y * CellsToGenerateSize.z, sizeof(int));
        m_meshSimplifierCS.SetBuffer(m_chunkifyMeshesKernelID, m_reorganizedMeshesIndexMapPropertyID, m_reorganizedMeshesIndexMapBuffer);

    }

    private void SetMarchingCubeShaderProperties()
    {
        GPUNoiseLayer3D[] gpuNoiseLayers = m_noiseLayers
            .Where(layer => layer.Enabled)
            .Select(layer => layer.ToGPUNoiseLayer())
            .ToArray();
        int layerCount = gpuNoiseLayers.Length;
        float weightMultiplier = 1.0f / gpuNoiseLayers.Select(layer => layer.LayerWeigth).Sum();

        // Meshes Buffer
        m_generatedMeshesBuffer?.Release();
        m_generatedMeshesBuffer = new ComputeBuffer(CellsToGenerateSize.x * CellsToGenerateSize.y * CellsToGenerateSize.z, Marshal.SizeOf(typeof(CellMesh)));
        m_marchingCubeCS.SetBuffer(m_marchingCubeKernelID, m_generatedMeshesPropertyID, m_generatedMeshesBuffer);

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
    private void ChunkifyMeshes(CellMesh[] cells)
    {
        int chunkCount = m_chunkZoneSizeToGenerate.x * m_chunkZoneSizeToGenerate.y * m_chunkZoneSizeToGenerate.z;
        m_generatedChunks = new ChunkMesh[chunkCount];
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
        int chunkOffset = chunkIndex * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;

        Vector3[] chunkVertices = new Vector3[12 * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];
        int[] chunkTriangles = new int[12 * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];

        int[] vertexMap = new int[12 * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];
        int nextVertexIndex = 0;
        int nextTriangleIndex = 0;

        for (uint ix = 0; ix < CHUNK_SIZE; ix++)
        {
            for (uint iy = 0; iy < CHUNK_SIZE; iy++)
            {
                for (uint iz = 0; iz < CHUNK_SIZE; iz++)
                {
                    uint i = ix + iy * CHUNK_SIZE + iz * CHUNK_SIZE * CHUNK_SIZE;
                    CellMesh currentMesh = m_generatedCells[chunkOffset + i];
                    Vector3[] currentVertices = currentMesh.GetVertices();
                    int[] currentTriangles = currentMesh.GetTriangles();

                    for (uint j = 0; j < 12; j++)
                    {
                        int rawVertexIndex = currentTriangles[j];
                        if (rawVertexIndex == -1)
                        {
                            break;
                        }

                        if (rawVertexIndex >= 12 * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE)
                        {
                            Debug.LogError($"{rawVertexIndex}");
                        }

                        if (vertexMap[rawVertexIndex] == 0)
                        {
                            chunkVertices[nextVertexIndex] = currentVertices[rawVertexIndex % 12] + new Vector3(ix, iy, iz);

                            nextVertexIndex++;
                            vertexMap[rawVertexIndex] = nextVertexIndex;

                        }
                        chunkTriangles[nextTriangleIndex++] = vertexMap[rawVertexIndex] - 1;
                    }
                }
            } 
        }

        ChunkMesh chunk = new ChunkMesh(chunkVertices.Resize(nextVertexIndex), chunkTriangles.Resize(nextTriangleIndex));

        m_generatedChunks[chunkIndex] = chunk;
    }
}

public static class ArrayExtension
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
}
