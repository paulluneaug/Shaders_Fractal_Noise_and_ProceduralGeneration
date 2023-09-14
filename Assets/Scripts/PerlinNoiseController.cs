using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class PerlinNoiseController : MonoBehaviour
{
    [Serializable]
    private struct NoiseLayer
    {
        public int NoiseScale;
        public bool UseSmootherStep;
        public int GradientOffset;
        public float LayerWeight;

        public GPUNoiseLayer ToGPUNoiseLayer(Vector2Int textureSize)
        {
            return new GPUNoiseLayer(
                LayerWeight,
                GradientOffset,
                Mathf.CeilToInt((float)textureSize.x / NoiseScale),
                Mathf.CeilToInt((float)textureSize.x / NoiseScale),
                UseSmootherStep);
        }
    }

    private readonly struct GPUNoiseLayer
    {
        public readonly float LayerWeigth;
        
        public readonly int GradientOffset;
        public readonly int GradientSizeX;
        public readonly int GradientSizeY;

        public readonly bool UseSmootherStep;

        public GPUNoiseLayer(float layerWeigth, int gradientOffset, int gradientSizeX, int gradientSizeY, bool useSmootherStep)
        {
            LayerWeigth = layerWeigth;
            GradientOffset = gradientOffset;
            GradientSizeX = gradientSizeX;
            GradientSizeY = gradientSizeY;
            UseSmootherStep = useSmootherStep;
        }
    }

    private const string KERNEL_NAME = "PerlinNoise";

    private const string RESULT_BUFFER = "_ResultBuffer";
    private const string RESULT_BUFFER_SIZE_X = "_ResultBufferSizeX";

    private const string NOISE_LAYER_COUNT = "_NoiseLayersCount";
    private const string NOISE_LAYERS = "_NoiseLayers";
    private const string NOISE_WEIGHTS_MULTIPLIER = "_NoiseWeigthsMultiplier";

    [SerializeField] private ComputeShader m_perlinNoiseShader = null;

    [SerializeField] private Renderer m_renderer = null;
    [SerializeField] private Vector2Int m_textureSize = Vector2Int.one;

    [SerializeField] private NoiseLayer[] m_noiseLayers = null;

    [SerializeField] private Terrain m_terrain;
    [SerializeField] private int m_terrainHeight;

    [NonSerialized] private int m_kernelID = 0;

    [NonSerialized] private int m_resultBufferPropertyID = 0;
    [NonSerialized] private int m_resultBufferSizeXPropertyID = 0;

    [NonSerialized] private int m_noiseLayerCountPropertyID = 0;
    [NonSerialized] private int m_noiseLayersPropertyID = 0;
    [NonSerialized] private int m_noiseWeightsMultiplierPropertyID = 0;

    [NonSerialized] private MaterialPropertyBlock m_propertyBlock = null;
    [NonSerialized] private ComputeBuffer m_resultBuffer = null;
    [NonSerialized] private ComputeBuffer m_noiseLayersBuffer = null;

    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;

    private void Awake()
    {
        m_kernelID = m_perlinNoiseShader.FindKernel(KERNEL_NAME);

        m_noiseLayerCountPropertyID = Shader.PropertyToID(NOISE_LAYER_COUNT);
        m_noiseLayersPropertyID = Shader.PropertyToID(NOISE_LAYERS);
        m_noiseWeightsMultiplierPropertyID = Shader.PropertyToID(NOISE_WEIGHTS_MULTIPLIER);
        m_resultBufferPropertyID = Shader.PropertyToID(RESULT_BUFFER);
        m_resultBufferSizeXPropertyID = Shader.PropertyToID(RESULT_BUFFER_SIZE_X);

        m_propertyBlock = new MaterialPropertyBlock();

        m_recorder = new ScriptExecutionTimeRecorder();

        UpdateShaderProperty();
    }

    private void OnDestroy()
    {
        m_resultBuffer.Release();
        m_noiseLayersBuffer.Release();
    }

    public void UpdateShaderProperty()
    {
        m_recorder.Reset();

        int layerCount = m_noiseLayers.Length;

        GPUNoiseLayer[] gpuNoiseLayers = m_noiseLayers.Select(layer => layer.ToGPUNoiseLayer(m_textureSize)).ToArray();
        float weightMultiplier = 1.0f / m_noiseLayers.Select(layer => layer.LayerWeight).Sum();

        m_resultBuffer?.Release();
        m_resultBuffer = new ComputeBuffer(m_textureSize.x * m_textureSize.y, sizeof(float));
        m_perlinNoiseShader.SetBuffer(m_kernelID, m_resultBufferPropertyID, m_resultBuffer);

        m_perlinNoiseShader.SetInt(m_noiseLayerCountPropertyID, layerCount);

        m_noiseLayersBuffer?.Release();
        m_noiseLayersBuffer = new ComputeBuffer(m_textureSize.x * m_textureSize.y, Marshal.SizeOf(typeof(GPUNoiseLayer)));
        m_perlinNoiseShader.SetBuffer(m_kernelID, m_noiseLayersPropertyID, m_noiseLayersBuffer);

        m_perlinNoiseShader.SetInt(m_resultBufferSizeXPropertyID, m_textureSize.x);

        m_perlinNoiseShader.SetFloat(m_noiseWeightsMultiplierPropertyID, weightMultiplier);

        m_recorder.AddEvent("Shader properties assignation");

        int groupX = Mathf.CeilToInt(m_textureSize.x / 8.0f);
        int groupY = Mathf.CeilToInt(m_textureSize.y / 8.0f);
        m_perlinNoiseShader.Dispatch(m_kernelID, groupX, groupY, 1);

        m_recorder.AddEvent("Shader Dispatch");

        float[] result = new float[m_textureSize.x * m_textureSize.y];
        m_resultBuffer.GetData(result);

        m_recorder.AddEvent("Data acquisition from shader");

        float[,] terrainDatas = new float[m_textureSize.x, m_textureSize.y];
        for (int i = 0; i < m_textureSize.x * m_textureSize.y; i++)
        {
            terrainDatas[i % m_textureSize.x, i / m_textureSize.x] = result[i];
        }

        m_recorder.AddEvent("Buffer un-flattening");

        m_terrain.terrainData.heightmapResolution = m_textureSize.y + 1;
        m_terrain.terrainData.SetHeights(0, 0, terrainDatas);

        m_recorder.AddEvent("Terrain height set");

        m_terrain.Flush();
        m_recorder.AddEvent("Terrain Flush");

        m_recorder.LogAllEventsTimeSpan();
    }

    //private Vector2Int FillGradientBuffer()
    //{

    //    int gradientLength = gradientSize.x * gradientSize.y;
    //    m_gradientBuffer = new ComputeBuffer(gradientLength, sizeof(float) * 2);

    //    float[] gradientValues = new float[gradientLength * 2];
    //    for (int i = 0; i < gradientLength * 2; i++)
    //    {
    //        gradientValues[i] = UnityEngine.Random.value;
    //    }

    //    m_gradientBuffer.SetData(gradientValues);

    //    return gradientSize;
    //}
}
