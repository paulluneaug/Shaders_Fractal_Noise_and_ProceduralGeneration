using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class PerlinNoise3DController : MonoBehaviour
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

    private const string KERNEL_NAME = "PerlinNoise3D";

    private const string RESULT_TEXTURE = "_ResultTexture";
    private const string RESULT_BUFFER = "_ResultBuffer";
    private const string RESULT_BUFFER_DIM = "_ResultBufferDim";

    private const string NOISE_LAYER_COUNT = "_NoiseLayersCount";
    private const string NOISE_LAYERS = "_NoiseLayers";
    private const string NOISE_WEIGHTS_MULTIPLIER = "_NoiseWeigthsMultiplier";

    private const string GRADIENT = "_Gradient";
    private const string GRADIENT_SIZE = "_GradientSize";

    private const string FLOAT_BUFFER = "_FloatBuffer";
    private const string FLOAT_BUFFER_SIZE_X = "_FloatBufferSizeX";

    private const string BASE_MAP = "_BaseMap";

    [SerializeField] private ComputeShader m_perlinNoiseShader = null;

    [SerializeField] private int m_textureDimension = 512;

    [SerializeField] private NoiseLayer3D[] m_noiseLayers = null;

    [SerializeField] private bool m_useColors;

    [SerializeField] private RenderTextureFormat m_renderTexFormat;

    [SerializeField] private Gradient m_colorGradient = new Gradient();

    [NonSerialized] private int m_kernelID = 0;

    [NonSerialized] private int m_resultTexturePropertyID = 0;
    [NonSerialized] private int m_resultBufferPropertyID = 0;
    [NonSerialized] private int m_resultBufferDimPropertyID = 0;

    [NonSerialized] private int m_noiseLayerCountPropertyID = 0;
    [NonSerialized] private int m_noiseLayersPropertyID = 0;
    [NonSerialized] private int m_noiseWeightsMultiplierPropertyID = 0;

    [NonSerialized] private int m_floatBufferPropertyID = 0;
    [NonSerialized] private int m_floatBufferSizeXPropertyID = 0;

    [NonSerialized] private int m_baseMapPropertyID = 0;

    [NonSerialized] private int m_gradientPropertyID = 0;
    [NonSerialized] private int m_gradientSizePropertyID = 0;

    [NonSerialized] private MaterialPropertyBlock m_rendererPropertyBlock = null;

    [NonSerialized] private RenderTexture m_resultTexture = null;
    [NonSerialized] private ComputeBuffer m_resultBuffer = null;
    [NonSerialized] private ComputeBuffer m_noiseLayersBuffer = null;

    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;

    [NonSerialized] private ComputeBuffer m_gradientBuffer;

    private void Awake()
    {
        m_kernelID = m_perlinNoiseShader.FindKernel(KERNEL_NAME);

        m_noiseLayerCountPropertyID = Shader.PropertyToID(NOISE_LAYER_COUNT);
        m_noiseLayersPropertyID = Shader.PropertyToID(NOISE_LAYERS);
        m_noiseWeightsMultiplierPropertyID = Shader.PropertyToID(NOISE_WEIGHTS_MULTIPLIER);

        m_resultTexturePropertyID = Shader.PropertyToID(RESULT_TEXTURE);
        m_resultBufferPropertyID = Shader.PropertyToID(RESULT_BUFFER);
        m_resultBufferDimPropertyID = Shader.PropertyToID(RESULT_BUFFER_DIM);

        m_floatBufferPropertyID = Shader.PropertyToID(FLOAT_BUFFER);
        m_floatBufferSizeXPropertyID = Shader.PropertyToID(FLOAT_BUFFER_SIZE_X);

        m_baseMapPropertyID = Shader.PropertyToID(BASE_MAP);

        m_gradientPropertyID = Shader.PropertyToID(GRADIENT);
        m_gradientSizePropertyID = Shader.PropertyToID(GRADIENT_SIZE);

        m_rendererPropertyBlock = new MaterialPropertyBlock();

        m_recorder = new ScriptExecutionTimeRecorder();

        UpdateShaderProperty();
    }

    private void OnDestroy()
    {
        m_resultBuffer.Release();
        m_noiseLayersBuffer.Release();
        m_gradientBuffer.Release();
    }

    public void UpdateShaderProperty()
    {
        m_recorder.Reset();

        SetShaderProperties();

        m_recorder.AddEvent("Shader properties assignation");

        int groupX = Mathf.CeilToInt(m_textureDimension / 8.0f);
        int groupY = Mathf.CeilToInt(m_textureDimension / 8.0f);
        int groupZ = Mathf.CeilToInt(m_textureDimension / 8.0f);
        m_perlinNoiseShader.Dispatch(m_kernelID, groupX, groupY, groupZ);

        m_recorder.AddEvent("Shader Dispatch");

        float[] result = new float[m_textureDimension * m_textureDimension * m_textureDimension];
        m_resultBuffer.GetData(result);


        m_recorder.AddEvent("Data acquisition from shader");


        m_resultTexture.SaveRenderTexture("TestTex3D");
        m_recorder.AddEvent("Renderer things");


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

        // Result Buffer
        m_resultBuffer?.Release();
        m_resultBuffer = new ComputeBuffer(m_textureDimension * m_textureDimension * m_textureDimension, sizeof(float));
        m_perlinNoiseShader.SetBuffer(m_kernelID, m_resultBufferPropertyID, m_resultBuffer);

        m_perlinNoiseShader.SetInt(m_resultBufferDimPropertyID, m_textureDimension);

        // Result Texture
        m_resultTexture = new RenderTexture(m_textureDimension, m_textureDimension, 0, m_renderTexFormat);
        m_resultTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        m_resultTexture.volumeDepth = m_textureDimension;
        m_resultTexture.enableRandomWrite = true;

        m_perlinNoiseShader.SetTexture(m_kernelID, m_resultTexturePropertyID, m_resultTexture);

        // Layers
        m_perlinNoiseShader.SetInt(m_noiseLayerCountPropertyID, layerCount);

        m_noiseLayersBuffer?.Release();
        m_noiseLayersBuffer = new ComputeBuffer(layerCount, Marshal.SizeOf(typeof(GPUNoiseLayer3D)));
        m_noiseLayersBuffer.SetData(gpuNoiseLayers);
        m_perlinNoiseShader.SetBuffer(m_kernelID, m_noiseLayersPropertyID, m_noiseLayersBuffer);

        m_perlinNoiseShader.SetFloat(m_noiseWeightsMultiplierPropertyID, weightMultiplier);

        // Gradient
        m_gradientBuffer?.Release();
        m_gradientBuffer = new ComputeBuffer(m_colorGradient.colorKeys.Length, 5 * sizeof(float));
        float[] managedBuffer = new float[5 * m_colorGradient.colorKeys.Length];

        int i = 0;
        foreach (GradientColorKey key in m_colorGradient.colorKeys)
        {
            managedBuffer[i++] = key.time;
            managedBuffer[i++] = key.color.r;
            managedBuffer[i++] = key.color.g;
            managedBuffer[i++] = key.color.b;
            managedBuffer[i++] = key.color.a;
        }
        m_gradientBuffer.SetData(managedBuffer);
        m_perlinNoiseShader.SetBuffer(m_kernelID, m_gradientPropertyID, m_gradientBuffer);
        m_perlinNoiseShader.SetInt(m_gradientSizePropertyID, m_colorGradient.colorKeys.Length);
    }
}
