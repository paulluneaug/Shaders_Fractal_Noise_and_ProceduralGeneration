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
            Debug.LogWarning($"NoiseScale : {NoiseScale}");
            Debug.LogWarning($"GradientSize : ({Mathf.CeilToInt((float)textureSize.x / NoiseScale)}; {Mathf.CeilToInt((float)textureSize.y / NoiseScale)})");
            return new GPUNoiseLayer(
                LayerWeight,
                GradientOffset,
                Mathf.CeilToInt((float)textureSize.x / NoiseScale),
                Mathf.CeilToInt((float)textureSize.y / NoiseScale),
                UseSmootherStep ? 1 : 0);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GPUNoiseLayer
    {
        public float LayerWeigth;
        
        public int GradientOffset;
        public int GradientSizeX;
        public int GradientSizeY;

        public int UseSmootherStep;

        public GPUNoiseLayer(float layerWeigth, int gradientOffset, int gradientSizeX, int gradientSizeY, int useSmootherStep)
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

    private const string FLOAT_BUFFER = "_FloatBuffer";
    private const string FLOAT_BUFFER_SIZE_X = "_FloatBufferSizeX";

    [SerializeField] private ComputeShader m_perlinNoiseShader = null;

    [SerializeField] private Renderer m_renderer = null;
    [SerializeField] private Vector2Int m_textureSize = Vector2Int.one;

    [SerializeField] private NoiseLayer[] m_noiseLayers = null;

    [SerializeField] private Terrain m_terrain;
    [SerializeField] private int m_terrainHeight;

    [SerializeField] private bool m_useColors;

    [NonSerialized] private int m_kernelID = 0;

    [NonSerialized] private int m_resultBufferPropertyID = 0;
    [NonSerialized] private int m_resultBufferSizeXPropertyID = 0;

    [NonSerialized] private int m_noiseLayerCountPropertyID = 0;
    [NonSerialized] private int m_noiseLayersPropertyID = 0;
    [NonSerialized] private int m_noiseWeightsMultiplierPropertyID = 0;

    [NonSerialized] private int m_floatBufferPropertyID = 0;
    [NonSerialized] private int m_floatBufferSizeXPropertyID = 0;

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

        m_floatBufferPropertyID = Shader.PropertyToID(FLOAT_BUFFER);
        m_floatBufferSizeXPropertyID = Shader.PropertyToID(FLOAT_BUFFER_SIZE_X);

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

        // Result Buffer
        m_resultBuffer?.Release();
        m_resultBuffer = new ComputeBuffer(m_textureSize.x * m_textureSize.y, sizeof(float));
        m_perlinNoiseShader.SetBuffer(m_kernelID, m_resultBufferPropertyID, m_resultBuffer);

        m_perlinNoiseShader.SetInt(m_resultBufferSizeXPropertyID, m_textureSize.x);

        // Layers
        m_perlinNoiseShader.SetInt(m_noiseLayerCountPropertyID, layerCount);

        m_noiseLayersBuffer?.Release();
        m_noiseLayersBuffer = new ComputeBuffer(m_textureSize.x * m_textureSize.y, Marshal.SizeOf(typeof(GPUNoiseLayer)));
        m_noiseLayersBuffer.SetData(gpuNoiseLayers);
        m_perlinNoiseShader.SetBuffer(m_kernelID, m_noiseLayersPropertyID, m_noiseLayersBuffer);

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

        Buffer.BlockCopy(result, 0, terrainDatas, 0, result.Length * sizeof(float));

        m_recorder.AddEvent("Buffer un-flattening");

        //Texture2D tex;
        //if (m_useColors)
        //{
        //    Color[] texDatas = new Color[m_textureSize.x * m_textureSize.y];
        //    for (int i = 0; i < m_textureSize.x * m_textureSize.y; i++)
        //    {
        //        float val = result[i];
        //        //terrainDatas[i % m_textureSize.x, i / m_textureSize.x] = val;
        //        texDatas[i] = new Color(val, val, val);
        //    }
        //    tex = new Texture2D(m_textureSize.x, m_textureSize.y);
        //    tex.SetPixels(0, 0, m_textureSize.x, m_textureSize.y, texDatas);
        //}
        //else
        //{
        //    tex = new Texture2D(m_textureSize.x, m_textureSize.y, TextureFormat.RFloat, true);
        //    tex.SetPixelData(result, 0);
        //}

        //tex.Apply();

        m_renderer.GetPropertyBlock(m_propertyBlock);
        Debug.Log(m_textureSize.x);
        m_propertyBlock.SetInt(m_floatBufferSizeXPropertyID, m_textureSize.x);
        m_propertyBlock.SetBuffer(m_floatBufferPropertyID, m_resultBuffer);
        m_renderer.SetPropertyBlock(m_propertyBlock);

        m_recorder.AddEvent("Renderer things");

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
