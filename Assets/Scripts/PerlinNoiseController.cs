using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoiseController : MonoBehaviour
{
    private const string KERNEL_NAME = "PerlinNoise";

    private const string RESULT = "_Result";
    private const string SMOOTHER_STEP = "_SmootherStep";
    private const string GRADIENT_SIZE_X = "_GradientSizeX";
    private const string GRADIENT_SIZE_Y = "_GradientSizeY";
    private const string RESULT_BUFFER = "_ResultBuffer";
    private const string GRADIENT_OFFSET = "_GradientOffet";

    [SerializeField] private ComputeShader m_perlinNoiseShader = null;

    [SerializeField] private Renderer m_renderer = null;
    [SerializeField] private int m_noiseScale = 1;
    [SerializeField] private Vector2Int m_textureSize = Vector2Int.one;

    [SerializeField] private bool m_useSmootherStep;
    [SerializeField] private int m_gradientOffset;

    [SerializeField] private Terrain m_terrain;
    [SerializeField] private int m_terrainHeight;

    [NonSerialized] private int m_kernelID = 0;

    [NonSerialized] private int m_resultPropertyID = 0;
    [NonSerialized] private int m_smootherStepPropertyID = 0;
    [NonSerialized] private int m_gradientSizeXPropertyID = 0;
    [NonSerialized] private int m_gradientSizeYPropertyID = 0;
    [NonSerialized] private int m_resultBufferPropertyID = 0;
    [NonSerialized] private int m_gradientOffsetPropertyID = 0;

    [NonSerialized] private MaterialPropertyBlock m_propertyBlock = null;
    [NonSerialized] private RenderTexture m_targetTexture = null;
    [NonSerialized] private ComputeBuffer m_resultBuffer;

    private void Awake()
    {
        m_kernelID = m_perlinNoiseShader.FindKernel(KERNEL_NAME);

        m_resultPropertyID = Shader.PropertyToID(RESULT);
        m_smootherStepPropertyID = Shader.PropertyToID(SMOOTHER_STEP);
        m_gradientSizeXPropertyID = Shader.PropertyToID(GRADIENT_SIZE_X);
        m_gradientSizeYPropertyID = Shader.PropertyToID(GRADIENT_SIZE_Y);
        m_resultBufferPropertyID = Shader.PropertyToID(RESULT_BUFFER);
        m_gradientOffsetPropertyID = Shader.PropertyToID(GRADIENT_OFFSET);

        m_propertyBlock = new MaterialPropertyBlock();

        UpdateShaderProperty();
    }

    private void OnDestroy()
    {
        m_resultBuffer.Release();
    }

    private void UpdateShaderProperty()
    {
        Vector2Int gradientSize = new Vector2Int(
            Mathf.CeilToInt((float)m_textureSize.x / m_noiseScale),
            Mathf.CeilToInt((float)m_textureSize.x / m_noiseScale));

        Debug.LogError($"GradientSize : {gradientSize}");
        Debug.LogError($"NoiseScale : {m_noiseScale}");

        m_targetTexture = new RenderTexture(m_textureSize.x, m_textureSize.y, 32/*, RenderTextureFormat.RFloat*/);
        m_targetTexture.enableRandomWrite = true;

        m_resultBuffer?.Release();
        m_resultBuffer = new ComputeBuffer(m_textureSize.x * m_textureSize.y, sizeof(float));
        m_perlinNoiseShader.SetBuffer(m_kernelID, m_resultBufferPropertyID, m_resultBuffer);

        m_perlinNoiseShader.SetTexture(m_kernelID, m_resultPropertyID, m_targetTexture);
        m_perlinNoiseShader.SetBool(m_smootherStepPropertyID, m_useSmootherStep);

        m_perlinNoiseShader.SetInt("_ResultBufferSizeX", m_textureSize.x);

        m_perlinNoiseShader.SetInt(m_gradientSizeXPropertyID, gradientSize.x);
        m_perlinNoiseShader.SetInt(m_gradientSizeYPropertyID, gradientSize.y);

        m_perlinNoiseShader.SetInt(m_gradientOffsetPropertyID, m_gradientOffset);

        int groupX = Mathf.CeilToInt(m_textureSize.x / 8.0f);
        int groupY = Mathf.CeilToInt(m_textureSize.y / 8.0f);
        m_perlinNoiseShader.Dispatch(m_kernelID, groupX, groupY, 1);

        m_renderer.material.mainTexture = m_targetTexture;

        float[] result = new float[m_textureSize.x * m_textureSize.y];
        m_resultBuffer.GetData(result);

        float[,] terrainDatas = new float[m_textureSize.x, m_textureSize.y];
        for (int i = 0; i < m_textureSize.x * m_textureSize.y; i++)
        {
            terrainDatas[i % m_textureSize.x, i / m_textureSize.x] = result[i];
        }

        m_terrain.terrainData.heightmapResolution = m_textureSize.y + 1;
        m_terrain.terrainData.SetHeights(0, 0, terrainDatas);
        m_terrain.Flush();
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

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateShaderProperty();
        }
        //Vector2Int size = new Vector2Int();
        //size.x = Mathf.RoundToInt((float)m_textureScale.x / m_noiseScale) * m_noiseScale + 1;
        //size.y = Mathf.RoundToInt((float)m_textureScale.y / m_noiseScale) * m_noiseScale + 1;
        //m_textureScale = size;

    }
}
