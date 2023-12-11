using System;
using UnityEngine;

[Serializable]
public class GradientShaderProperty : IDisposable
{
    private const string GRADIENT_PROPERTY = "_Gradient";
    private const string GRADIENT_SIZE_PROPERTY = "_GradientSize";
    private const string BLEND_GRADIENT_PROPERTY = "_BlendGradient";

    [SerializeField] private Gradient m_colorGradient = new Gradient();

    [NonSerialized] private int m_gradientPropertyID = 0;
    [NonSerialized] private int m_gradientSizePropertyID = 0;
    [NonSerialized] private int m_blendGradientPropertyID = 0;

    [NonSerialized] private ComputeBuffer m_gradientBuffer;

    public void ApplyShaderProperties(MaterialPropertyBlock materialPropertyBlock)
    {
        InitializeBuffer();
        materialPropertyBlock.SetBuffer(m_gradientPropertyID, m_gradientBuffer);
        materialPropertyBlock.SetInt(m_gradientSizePropertyID, m_colorGradient.colorKeys.Length);
        materialPropertyBlock.SetInt(m_blendGradientPropertyID, m_colorGradient.mode == GradientMode.Blend ? 1 : 0);
    }

    public void ApplyShaderProperties(Material material)
    {
        InitializeBuffer();
        material.SetBuffer(m_gradientPropertyID, m_gradientBuffer);
        material.SetInt(m_gradientSizePropertyID, m_colorGradient.colorKeys.Length);
        material.SetInt(m_blendGradientPropertyID, m_colorGradient.mode == GradientMode.Blend ? 1 : 0);
    }

    public void Dispose()
    {
        m_gradientBuffer.Release();
    }

    private void InitializeBuffer()
    {
        m_gradientPropertyID = Shader.PropertyToID(GRADIENT_PROPERTY);
        m_gradientSizePropertyID = Shader.PropertyToID(GRADIENT_SIZE_PROPERTY);
        m_blendGradientPropertyID = Shader.PropertyToID(BLEND_GRADIENT_PROPERTY);

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
    }
}
