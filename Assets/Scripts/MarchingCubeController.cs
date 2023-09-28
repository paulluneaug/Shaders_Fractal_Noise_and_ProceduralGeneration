using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class MarchingCubeController : MonoBehaviour
{
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

    [StructLayout(LayoutKind.Sequential)]
    private struct CubeMesh
    {
        public FixedSizeArray<Vector3> Vertices;
        public FixedSizeArray<int> Triangles;
    }

    private const string NOISE_KERNEL_NAME = "ComputeNoise";
    private const string MARCHING_CUBE_KERNEL_NAME = "MarchingCubes";

    private const string NOISE_TEXTURE = "_NoiseTexture";
    private const string RESULT_MESHES = "_ResultMeshes";

    private const string ZONE_TO_GENERATE_SIZE = "_ZoneToGenerateSize";
    private const string OFFSET = "_Offset";

    private const string THRESHOLD = "_Threshold";

    private const string NOISE_LAYERS = "_NoiseLayers";
    private const string NOISE_LAYERS_COUNT = "_NoiseLayers";
    private const string NOISE_WEIGHTS_MULTIPLIER = "_NoiseWeigthsMultiplier";

    [SerializeField] private ComputeShader m_marchingCubeCS = null;

    [SerializeField] private Vector3Int m_offset = Vector3Int.zero;
    [SerializeField] private Vector3Int m_zoneToGenerateSize = Vector3Int.one * 64;

    [SerializeField, Range(0.0f, 1.0f)] private float m_threshold = 0.5f;

    [SerializeField] private NoiseLayer3D[] m_noiseLayers = null;

    // Cache
    [NonSerialized] private int m_noiseKernelID = 0;
    [NonSerialized] private int m_marchingCubeKernelID = 0;

    [NonSerialized] private int m_noiseTexturePropertyID = 0;
    [NonSerialized] private int m_resultMeshesPropertyID = 0;

    [NonSerialized] private int m_zoneToGenerateSizePropertyID = 0;
    [NonSerialized] private int m_offsetPropertyID = 0;

    [NonSerialized] private int m_thresholdID = 0;

    [NonSerialized] private int m_noiseLayerCountPropertyID = 0;
    [NonSerialized] private int m_noiseLayersPropertyID = 0;
    [NonSerialized] private int m_noiseWeightsMultiplierPropertyID = 0;

    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;

    [NonSerialized] private RenderTexture m_noiseTexture = null;
    [NonSerialized] private ComputeBuffer m_resultMeshesBuffer = null;
    [NonSerialized] private ComputeBuffer m_noiseLayersBuffer = null;

    private void Awake()
    {
        m_noiseKernelID = m_marchingCubeCS.FindKernel(NOISE_KERNEL_NAME);
        m_marchingCubeKernelID = m_marchingCubeCS.FindKernel(MARCHING_CUBE_KERNEL_NAME);

        m_noiseTexturePropertyID = Shader.PropertyToID(NOISE_TEXTURE);
        m_resultMeshesPropertyID = Shader.PropertyToID(RESULT_MESHES);

        m_zoneToGenerateSizePropertyID = Shader.PropertyToID(ZONE_TO_GENERATE_SIZE);
        m_offsetPropertyID = Shader.PropertyToID(OFFSET);

        m_thresholdID = Shader.PropertyToID(THRESHOLD);

        m_noiseLayersPropertyID = Shader.PropertyToID(NOISE_LAYERS);
        m_noiseLayerCountPropertyID = Shader.PropertyToID(NOISE_LAYERS_COUNT);
        m_noiseWeightsMultiplierPropertyID = Shader.PropertyToID(NOISE_WEIGHTS_MULTIPLIER);

        m_recorder = new ScriptExecutionTimeRecorder();

        UpdateShaderProperty();
    }

    private void UpdateShaderProperty()
    {
        m_recorder.Reset();

        SetShaderProperties();

        m_recorder.AddEvent("Shader properties assignation");

        int groupX = Mathf.CeilToInt(m_zoneToGenerateSize.x + 1 / 8.0f);
        int groupY = Mathf.CeilToInt(m_zoneToGenerateSize.y + 1 / 8.0f);
        int groupZ = Mathf.CeilToInt(m_zoneToGenerateSize.z + 1 / 8.0f);
        m_marchingCubeCS.Dispatch(m_noiseKernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Noise Shader Dispatch");

        groupX = Mathf.CeilToInt(m_zoneToGenerateSize.x / 8.0f);
        groupY = Mathf.CeilToInt(m_zoneToGenerateSize.y / 8.0f);
        groupZ = Mathf.CeilToInt(m_zoneToGenerateSize.z / 8.0f);
        m_marchingCubeCS.Dispatch(m_marchingCubeKernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Marching cubes Shader Dispatch");

        CubeMesh[] result = new CubeMesh[m_zoneToGenerateSize.x * m_zoneToGenerateSize.y * m_zoneToGenerateSize.z];
        m_resultMeshesBuffer.GetData(result);
        m_recorder.AddEvent("Data acquisition from shader");

        GenerateMeshes(result);
        m_recorder.AddEvent("Meshes Generation");


        m_recorder.LogAllEventsTimeSpan();
    }

    private void SetShaderProperties()
    {
        GPUNoiseLayer3D[] gpuNoiseLayers = m_noiseLayers
            .Where(layer => layer.Enabled)
            .Select(layer => layer.ToGPUNoiseLayer())
            .ToArray();
        int layerCount = gpuNoiseLayers.Length;
        float weightMultiplier = 1.0f / gpuNoiseLayers.Select(layer => layer.LayerWeigth).Sum();

        // Meshes Buffer
        m_resultMeshesBuffer?.Release();
        m_resultMeshesBuffer = new ComputeBuffer(m_zoneToGenerateSize.x * m_zoneToGenerateSize.y * m_zoneToGenerateSize.z, Marshal.SizeOf(typeof(CubeMesh)));
        m_marchingCubeCS.SetBuffer(m_marchingCubeKernelID, m_resultMeshesPropertyID, m_resultMeshesBuffer);

        // Other variables
        m_marchingCubeCS.SetFloat(m_thresholdID, m_threshold);
        m_marchingCubeCS.SetInts(m_zoneToGenerateSizePropertyID, m_zoneToGenerateSize.x, m_zoneToGenerateSize.y, m_zoneToGenerateSize.z);
        m_marchingCubeCS.SetInts(m_offsetPropertyID, m_offset.x, m_offset.y, m_offset.z);

        // Noise Texture
        m_noiseTexture = new RenderTexture(m_zoneToGenerateSize.x + 1, m_zoneToGenerateSize.y + 1, 0, RenderTextureFormat.RFloat);
        m_noiseTexture.dimension = TextureDimension.Tex3D;
        m_noiseTexture.volumeDepth = m_zoneToGenerateSize.z + 1;
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

    private void GenerateMeshes(CubeMesh[] meshes)
    {
        for (int i = 0; i < meshes.Length; i++)
        {
            CubeMesh mesh = meshes[i];
            CreateMesh(mesh, i);
        }
    }

    private void CreateMesh(CubeMesh cubeMesh, int index)
    {
        Vector3Int coordinates = GetCoordinatesFromIndex(index);
        Vector3 meshPos = m_offset + coordinates;
        GameObject go = new GameObject($"Mesh_{index}{meshPos}");
        go.transform.position = meshPos;
        go.AddComponent<MeshRenderer>();
        MeshFilter filter = go.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        Vector3 emptyVect = -Vector3.one;
        mesh.vertices = cubeMesh.Vertices.GetArray().Where(v => v != emptyVect).ToArray();
        mesh.triangles = cubeMesh.Triangles.GetArray().Where(v => v != -1).ToArray();
        filter.mesh = mesh;
    }

    private Vector3Int GetCoordinatesFromIndex(int index)
    {
        return Vector3Int.zero;
    }
}
