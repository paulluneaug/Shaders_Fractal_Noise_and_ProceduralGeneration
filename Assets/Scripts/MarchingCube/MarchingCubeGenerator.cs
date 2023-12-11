using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Unity.Collections;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

using Cysharp.Threading.Tasks;

using static Constants;
using static MeshStructs;

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
    private const string GENERATED_CELLS = "_GeneratedCells";

    private const string CHUNK_ZONE_TO_GENERATE_SIZE = "_ChunkZoneSizeToGenerate";
    private const string CHUNK_OFFSET = "_ChunkOffset";

    private const string THRESHOLD = "_Threshold";
    private const string SMOOTH_MESH = "_SmoothMesh";

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
    [SerializeField] private bool m_smoothMesh = true;

    [SerializeField] private NoiseLayer3D[] m_noiseLayers = null;

    [SerializeField] private Material m_terrainMaterial = null;
    [SerializeField] GradientShaderProperty m_terrainGradient = null;
    #endregion

    #region Cache
    #region Shader Properties IDs
    // MarchingCube CS
    [NonSerialized] private int m_noiseKernelID = 0;
    [NonSerialized] private int m_marchingCubeKernelID = 0;

    [NonSerialized] private int m_noiseTexturePropertyID = 0;
    [NonSerialized] private int m_generatedCellsPropertyID = 0;

    [NonSerialized] private int m_chunkZoneSizeToGeneratePropertyID = 0;
    [NonSerialized] private int m_chunkOffsetPropertyID = 0;

    [NonSerialized] private int m_thresholdID = 0;
    [NonSerialized] private int m_smoothMeshID = 0;

    [NonSerialized] private int m_noiseLayerCountPropertyID = 0;
    [NonSerialized] private int m_noiseLayersPropertyID = 0;
    [NonSerialized] private int m_noiseWeightsMultiplierPropertyID = 0;
    #endregion

    [NonSerialized] private Vector3Int m_chunkOffset = Vector3Int.zero;
    [NonSerialized] private Vector3Int m_chunkZoneSizeToGenerate = Vector3Int.one * 4;

    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;

    [NonSerialized] private RenderTexture m_noiseTexture = null;
    [NonSerialized] private ComputeBuffer m_noiseLayersBuffer = null;
    [NonSerialized] private ComputeBuffer m_generatedCellsBuffer = null;

    [NonSerialized] private Transform m_transform = null;

    [NonSerialized] private NativeArray<CellMesh> m_generatedCells;
    [NonSerialized] private IChunkMesh[] m_generatedChunksMeshes = default;
    [NonSerialized] private Chunk[] m_generatedChunks = null;

    [NonSerialized] private Action<Chunk[]> m_generationCallback;
    #endregion

    private void Awake()
    {
        GetPropertiesIDs();

        m_terrainGradient.ApplyShaderProperties(m_terrainMaterial);

        m_recorder = new ScriptExecutionTimeRecorder();

        m_generatedCells = new NativeArray<CellMesh>(CellsCount, Allocator.Persistent);

        m_transform = transform;
    }

    private void OnDestroy()
    {
        m_terrainGradient.Dispose();
        m_generatedCells.Dispose();
        m_noiseLayersBuffer?.Release();
        m_generatedCellsBuffer?.Release();
    }

    public void GenerateZone(Vector3Int zoneToGenerate, Vector3Int offset, Action<Chunk[]> callback)
    {
        m_chunkZoneSizeToGenerate = zoneToGenerate;
        m_chunkOffset = offset;
        m_generationCallback = callback;
        //GenerateChunksAsync();
        StartCoroutine(GenerateChunksAsync()/*.AsUniTask().ToCoroutine()*/);
    }

    private void GetPropertiesIDs()
    {
        // MarchingCube CS
        m_noiseKernelID = m_marchingCubeCS.FindKernel(NOISE_KERNEL_NAME);
        m_marchingCubeKernelID = m_marchingCubeCS.FindKernel(MARCHING_CUBE_KERNEL_NAME);

        m_noiseTexturePropertyID = Shader.PropertyToID(NOISE_TEXTURE);
        m_generatedCellsPropertyID = Shader.PropertyToID(GENERATED_CELLS);

        m_chunkZoneSizeToGeneratePropertyID = Shader.PropertyToID(CHUNK_ZONE_TO_GENERATE_SIZE);
        m_chunkOffsetPropertyID = Shader.PropertyToID(CHUNK_OFFSET);

        m_thresholdID = Shader.PropertyToID(THRESHOLD);
        m_smoothMeshID = Shader.PropertyToID(SMOOTH_MESH);

        m_noiseLayersPropertyID = Shader.PropertyToID(NOISE_LAYERS);
        m_noiseLayerCountPropertyID = Shader.PropertyToID(NOISE_LAYERS_COUNT);
        m_noiseWeightsMultiplierPropertyID = Shader.PropertyToID(NOISE_WEIGHTS_MULTIPLIER);
    }

    private IEnumerator GenerateChunksAsync()
    {
        yield return GenerateChunks();

        m_generationCallback?.Invoke(m_generatedChunks);
    }

    private IEnumerator GenerateChunks()
    {
        m_recorder.Reset();

        // Shader properties assignation
        Profiler.BeginSample("Shader properties assignation");
        SetMarchingCubeShaderProperties();

        m_recorder.AddEvent("Shader properties assignation");
        Profiler.EndSample();

        // Noise Shader Dispatch
        Profiler.BeginSample("Noise Shader Dispatch");
        int groupX = Mathf.CeilToInt(CellsToGenerateSize.x + 1 / 8.0f);
        int groupY = Mathf.CeilToInt(CellsToGenerateSize.y + 1 / 8.0f);
        int groupZ = Mathf.CeilToInt(CellsToGenerateSize.z + 1 / 8.0f);
        m_marchingCubeCS.Dispatch(m_noiseKernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Noise Shader Dispatch");
        Profiler.EndSample();

        // Marching cubes Shader Dispatch
        Profiler.BeginSample("Marching cubes Shader Dispatch");
        groupX = Mathf.CeilToInt(CellsToGenerateSize.x / 8.0f);
        groupY = Mathf.CeilToInt(CellsToGenerateSize.y / 8.0f);
        groupZ = Mathf.CeilToInt(CellsToGenerateSize.z / 8.0f);
        m_marchingCubeCS.Dispatch(m_marchingCubeKernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Marching cubes Shader Dispatch");
        Profiler.EndSample();

        // Mesh acquisition from shader
        AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(m_generatedCellsBuffer);
        yield return new WaitUntil(() => request.done);

        Profiler.BeginSample("Mesh acquisition from shader");

        Profiler.BeginSample("request.GetData<CellMesh>()");
        NativeArray<CellMesh> tempGeneratedCells = request.GetData<CellMesh>();
        Profiler.EndSample();

        Profiler.BeginSample("Copy");
        tempGeneratedCells.CopyTo(m_generatedCells);
        Profiler.EndSample();

        tempGeneratedCells.Dispose();

        m_recorder.AddEvent("Mesh acquisition from shader");
        Profiler.EndSample();

        // Chunkify Operation

        yield return ChunkifyMeshes(m_generatedCells);

        m_recorder.AddEvent("Chunkify Operation");

        // Meshes Generation
        Profiler.BeginSample("Meshes Generation");
        m_generatedChunks = new Chunk[ChunksCount];
        for (int i = 0; i < ChunksCount; i++)
        {
            CreateMesh(m_generatedChunksMeshes[i], i);
        }

        m_recorder.AddEvent("Meshes Generation");
        Profiler.EndSample();

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

        // Cells Buffer
        m_generatedCellsBuffer?.Release();
        m_generatedCellsBuffer = new ComputeBuffer(CellsCount, Marshal.SizeOf(typeof(CellMesh)));
        m_marchingCubeCS.SetBuffer(m_marchingCubeKernelID, m_generatedCellsPropertyID, m_generatedCellsBuffer);

        // Other variables
        m_marchingCubeCS.SetFloat(m_thresholdID, m_threshold);
        m_marchingCubeCS.SetInt(m_smoothMeshID, m_smoothMesh ? 1 : 0);
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

    private IEnumerator ChunkifyMeshes(NativeArray<CellMesh> cells)
    {

#if true
        m_generatedChunksMeshes = new IChunkMesh[ChunksCount];

        //for (int i = 0; i < ChunksCount; ++i)
        //{
        //    m_generatedChunksMeshes[i] = ChunkifyCellsForChunk<ChunkMesh>(cells, i);
        //    yield return null;
        //}
        Task t = Task.Run(() => Parallel.For(0, ChunksCount, i => { m_generatedChunksMeshes[i] = ChunkifyCellsForChunk<ChunkMesh>(cells, i); }));
        yield return t.AsUniTask().ToCoroutine();
#else
        ChunkifyCellsJob job = new ChunkifyCellsJob(cells, new NativeArray<ChunkMesh>(cells.Length / CHUNK_VOLUME, Allocator.TempJob));
        JobHandle jobHandle = job.Schedule(ChunksCount, default);
        jobHandle.Complete();
        m_generatedChunksMeshes = job.GeneratedMeshes;
#endif
    }

    private void CreateMesh(IMesh meshStruct, int index)
    {
        Vector3Int chunkCoordinates = GetCoordinatesFromIndex(index, m_chunkZoneSizeToGenerate);
        Vector3 meshPos = CellOffset + chunkCoordinates * CHUNK_SIZE;
        GameObject go = new GameObject($"Chunk_{index}_{meshPos}");
        Transform t = go.transform;
        t.position = meshPos;

        t.parent = m_transform;

        Chunk chunk = go.AddComponent<Chunk>();
        chunk.ChunkPosition = chunkCoordinates;
        m_generatedChunks[index] = chunk;

        Mesh mesh = meshStruct.GetMesh();

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.material = m_terrainMaterial;
        //renderer.shadowCastingMode = ShadowCastingMode.Off;

        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        MeshFilter filter = go.AddComponent<MeshFilter>();

        filter.mesh = mesh;

        go.isStatic = true;
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

    public static TChunk ChunkifyCellsForChunk<TChunk>(NativeArray<CellMesh> cells, int chunkIndex) where TChunk : IChunkMesh, new()
    {
        Profiler.BeginSample("ChunkifyCellsForChunk");
        Profiler.BeginSample("Memory allocation");
        int chunkOffset = chunkIndex * CHUNK_VOLUME;

        Span<Vector3> chunkVertices = stackalloc Vector3[12 * CHUNK_VOLUME];
        Span<int> chunkTriangles = stackalloc int[12 * CHUNK_VOLUME];

        //Span<int> vertexMap = stackalloc int[12 * CHUNK_VOLUME];

        //NativeArray<Vector3> chunkVertices = new NativeArray<Vector3>(12 * CHUNK_VOLUME, Allocator.Temp);
        //NativeArray<int> chunkTriangles = new NativeArray<int>(12 * CHUNK_VOLUME, Allocator.Temp);

        NativeArray<int> vertexMap = new NativeArray<int>(12 * CHUNK_VOLUME, Allocator.Persistent);

        int nextVertexIndex = 0;
        int nextTriangleIndex = 0;

        Span<int> currentTriangles = stackalloc int[CellMesh.TRIANGLES_COUNT];
        Span<Vector3> currentVertices = stackalloc Vector3[CellMesh.VERTICES_COUNT];

        Profiler.EndSample();
        Profiler.BeginSample("Vertex Map filling");
        for (int i = 0; i < CHUNK_VOLUME; ++i)
        {
            CellMesh currentMesh = cells[chunkOffset + i];

            currentMesh.FillSpanWithTriangles(currentTriangles);

            currentMesh.FillSpanWithVertices(currentVertices);

            //NativeArray<int> currentTriangles = currentMesh.GetTrianglesNativeArray();
            //NativeArray<Vector3> currentVertices = currentMesh.GetVerticesNativeArray();

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
            //currentTriangles.Dispose();
            //currentVertices.Dispose();
        }
        Profiler.EndSample();

        Profiler.BeginSample("Chunk building");
        for (int i = 0; i < CHUNK_VOLUME; ++i)
        {
            CellMesh currentMesh = cells[chunkOffset + i];
            currentMesh.FillSpanWithTriangles(currentTriangles);

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
        Profiler.EndSample();
        vertexMap.Dispose();

        TChunk chunk = new TChunk();

        //NativeArray<Vector3> resizedVertices = chunkVertices.Resize(nextVertexIndex);
        //NativeArray<int> resizedTriangles = chunkTriangles.Resize(nextTriangleIndex);

        chunk.SetTrianglesAndVertices(chunkVertices.Resize(nextVertexIndex), chunkTriangles.Resize(nextTriangleIndex));

        //chunkVertices.Dispose();
        //chunkTriangles.Dispose();
        //resizedVertices.Dispose();
        //resizedTriangles.Dispose();
        Profiler.EndSample();
        return chunk;
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


