using System;
using System.Linq;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Jobs;

using UnityEngine;
using UnityEngine.Rendering;

using Cysharp.Threading.Tasks;

using static Constants;
using static MeshStructs;
using static ArrayExtension;
using Unity.Burst;
using Newtonsoft.Json.Bson;

public class MarchingCubeGenerator : MonoBehaviour
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
    #endregion

    #region Properties
    private Vector3Int CellOffset => m_chunkOffset * CHUNK_SIZE;

    private Vector3Int CellsToGenerateSize => m_chunkZoneSizeToGenerate * CHUNK_SIZE;

    private int ChunksCount => m_chunkZoneSizeToGenerate.x * m_chunkZoneSizeToGenerate.y * m_chunkZoneSizeToGenerate.z;
    private int CellsCount => ChunksCount * CHUNK_VOLUME;
    #endregion

    #region Serialized Fields
    [SerializeField] private ComputeShader m_marchingCubeCS = null;


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
    #endregion

    [NonSerialized] private Vector3Int m_chunkOffset = Vector3Int.zero;
    [NonSerialized] private Vector3Int m_chunkZoneSizeToGenerate = Vector3Int.one * 4;

    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;

    [NonSerialized] private RenderTexture m_noiseTexture = null;
    [NonSerialized] private ComputeBuffer m_noiseLayersBuffer = null;
    [NonSerialized] private ComputeBuffer m_generatedMeshesBuffer = null;

    [NonSerialized] private Transform m_transform = null;

    [NonSerialized] private NativeArray<ChunkMesh> m_generatedChunksMeshes = default;
    [NonSerialized] private Chunk[] m_generatedChunks = null;

    [NonSerialized] private Action<Chunk[]> m_generationCallback;

    private void Awake()
    {
        GetPropertiesIDs();

        m_recorder = new ScriptExecutionTimeRecorder();

        m_transform = transform;
    }

    public void GenerateZone(Vector3Int zoneToGenerate, Vector3Int offset, Action<Chunk[]> callback)
    {
        m_chunkZoneSizeToGenerate = zoneToGenerate;
        m_chunkOffset = offset;
        m_generationCallback = callback;
        GenerateChunksAsync();
        //StartCoroutine(GenerateChunksAsync().AsUniTask().ToCoroutine());
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
    }

    private void GenerateChunksAsync()
    {

        GenerateChunks();

        m_generationCallback.Invoke(m_generatedChunks);
    }

    private void GenerateChunks()
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

        //  generatedCells = new NativeArray<CellMesh>(CellsCount, Allocator.Persistent);
        AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(m_generatedMeshesBuffer);
        request.WaitForCompletion();
        NativeArray<CellMesh> generatedCells = request.GetData<CellMesh>();

        m_recorder.AddEvent("Mesh acquisition from shader");

        ChunkifyMeshes(generatedCells);

        generatedCells.Dispose();

        m_recorder.AddEvent("Chunkify Operation");

        m_generatedChunks = new Chunk[ChunksCount];
        for (int i = 0; i < ChunksCount; i++)
        {
            CreateMesh(m_generatedChunksMeshes[i], i);
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

        // Meshes Buffer
        m_generatedMeshesBuffer?.Release();
        m_generatedMeshesBuffer = new ComputeBuffer(CellsCount, Marshal.SizeOf(typeof(CellMesh)));
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
    [BurstCompile(CompileSynchronously = true)]
    struct ChunkifyCellsJob : IJobFor
    {
        [ReadOnly]
        private NativeArray<CellMesh> m_cells;

        [WriteOnly] 
        public NativeArray<ChunkMesh> GeneratedMeshes;

        public ChunkifyCellsJob(NativeArray<CellMesh> cells, NativeArray<ChunkMesh> generatedMeshes)
        {
            m_cells = cells;
            GeneratedMeshes = generatedMeshes;
        }

        public void Execute(int chunkIndex)
        {
            int chunkOffset = chunkIndex * CHUNK_VOLUME;

            NativeArray<Vector3> chunkVertices = new NativeArray<Vector3>(12 * CHUNK_VOLUME, Allocator.Temp);
            NativeArray<int> chunkTriangles = new NativeArray<int>(12 * CHUNK_VOLUME, Allocator.Temp);

            NativeArray<int> vertexMap = new NativeArray<int>(12 * CHUNK_VOLUME, Allocator.Temp);
            int nextVertexIndex = 0;
            int nextTriangleIndex = 0;


            for (int i = 0; i < CHUNK_VOLUME; ++i)
            {
                CellMesh currentMesh = m_cells[chunkOffset + i];
                NativeArray<int> currentTriangles = currentMesh.GetTrianglesNativeArray();
                NativeArray<Vector3> currentVertices = currentMesh.GetVerticesNativeArray();

                int currentMeshMinVerticeIndex = i * 12;
                int currentMeshMaxVerticeIndex = (i + 1) * 12 - 1;

                for (int j = 0; j < 12; j++)
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
                CellMesh currentMesh = m_cells[chunkOffset + i];
                NativeArray<int> currentTriangles = currentMesh.GetTrianglesNativeArray();
                for (int j = 0; j < 12; j++)
                {
                    int rawVertexIndex = currentTriangles[j];
                    if (rawVertexIndex == -1)
                    {
                        break;
                    }
                    chunkTriangles[nextTriangleIndex++] = vertexMap[rawVertexIndex] - 1;
                }
            }

            GeneratedMeshes[chunkIndex] = new ChunkMesh(chunkVertices.Resize(nextVertexIndex), chunkTriangles.Resize(nextTriangleIndex));
            //m_resultHolder.GeneratedChunksMeshes[chunkIndex] = chunk;
        }
    }

    private void ChunkifyMeshes(NativeArray<CellMesh> cells)
    {
#if false
        for (int i = 0; i < ChunksCount; ++i)
        {
            ChunkifyCellsForChunk(cells, i);
        }

        //IEnumerable<Task> chunkifyTasks = Enumerable.Range(0, ChunksCount)
        //    .Select(i => ChunkifyCellsForChunk(cells, i));

        //await Task.WhenAll(chunkifyTasks);
#else
        ChunkifyCellsJob job = new ChunkifyCellsJob(cells, new NativeArray<ChunkMesh>(cells.Length / CHUNK_VOLUME, Allocator.Persistent));
        JobHandle jobHandle = job.Schedule(ChunksCount, default);
        jobHandle.Complete();
        m_generatedChunksMeshes = job.GeneratedMeshes;
#endif
    }

    private void CreateMesh(IMesh meshStruct, int index)
    {
        Vector3Int chunkCoordinates = GetCoordinatesFromIndex(index, m_chunkZoneSizeToGenerate);
        Vector3 meshPos = CellOffset + chunkCoordinates * CHUNK_SIZE;
        GameObject go = new GameObject($"Mesh_{index}{meshPos}");
        Transform t = go.transform;
        t.position = meshPos;

        t.parent = m_transform;

        Chunk chunk = go.AddComponent<Chunk>();
        chunk.ChunkPosition = chunkCoordinates;
        m_generatedChunks[index] = chunk;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.material = m_material;

        MeshFilter filter = go.AddComponent<MeshFilter>();

        filter.mesh = meshStruct.GetMesh();
    }

    private static Vector3Int GetCoordinatesFromIndex(int index, Vector3Int zone)
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

    private void ChunkifyCellsForChunk(NativeArray<CellMesh> cells, int chunkIndex)
    {
        Checker(cells[1]);
        Checker(new ChunkMesh());
    }

    private void Checker<T>(T a) where T : unmanaged
    {

    }
}

public static class Extensions
{
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
