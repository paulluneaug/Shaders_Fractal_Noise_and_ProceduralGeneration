using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

public class FractalController : MonoBehaviour
{
    private const string C_X_PROPERTY = "_Cx";
    private const string C_Y_PROPERTY = "_Cy";
    private const string MAX_ITERATIONS_PROPERTY = "_MaxIterations";
    private const string THRESHOLD_PROPERTY = "_Treshold";
    private const string POS_AND_SIZE_PROPERTY = "_PosAndSize";
    private const string GRADIENT_PROPERTY = "_Gradient";
    private const string GRADIENT_SIZE_PROPERTY = "_GradientSize";

    [SerializeField] private Renderer m_renderer;
    [SerializeField] private Rect m_startViewport = new Rect();

    [SerializeField] private Gradient m_colorGradient = new Gradient();

    [SerializeField] private float m_moveSpeed = 1.0f;
    [SerializeField] private float m_moveConstSpeed = 1.0f;
    [SerializeField] private float m_zoomSpeed = 1.0f;
    [SerializeField] private float m_lerpFactor = 1.0f;

    [SerializeField] private InputActionReference m_moveInput = null;
    [SerializeField] private InputActionReference m_moveConstInput = null;
    [SerializeField] private InputActionReference m_zoomInput = null;

    [NonSerialized] private MaterialPropertyBlock m_materialPropBlock = null;

    [NonSerialized] private int m_cxProprtyID = 0;
    [NonSerialized] private int m_cyProprtyID = 0;
    [NonSerialized] private int m_maxIterationsProprtyID = 0;
    [NonSerialized] private int m_thresholdProprtyID = 0;
    [NonSerialized] private int m_posAndSizeProprtyID = 0;
    [NonSerialized] private int m_gradientPropertyID = 0;
    [NonSerialized] private int m_gradientSizePropertyID = 0;

    [NonSerialized] private Vector4 m_currentViewport = Vector4.zero;
    [NonSerialized] private Vector4 m_targetViewport = Vector4.zero;
    [NonSerialized] private float m_zoom = 1.0f;

    [SerializeField] private Vector2 m_currentConst = Vector2.zero;
    [NonSerialized] private Vector2 m_targetConst = Vector2.zero;

    [NonSerialized] private ComputeBuffer m_gradientBuffer;

    // Start is called before the first frame update
    void Start()
    {
        if (m_moveInput.action.expectedControlType != "Vector2" || 
            m_moveConstInput.action.expectedControlType != "Vector2" ||
            m_zoomInput.action.expectedControlType != "Axis")
        {
            throw new Exception("Invalid controlType");
        }
        m_moveInput.action.actionMap.Enable();

        m_materialPropBlock = new MaterialPropertyBlock();
        m_renderer.GetPropertyBlock(m_materialPropBlock);

        m_cxProprtyID = Shader.PropertyToID(C_X_PROPERTY);
        m_cyProprtyID = Shader.PropertyToID(C_Y_PROPERTY);
        m_maxIterationsProprtyID = Shader.PropertyToID(MAX_ITERATIONS_PROPERTY);
        m_thresholdProprtyID = Shader.PropertyToID(THRESHOLD_PROPERTY);
        m_posAndSizeProprtyID = Shader.PropertyToID(POS_AND_SIZE_PROPERTY);
        m_gradientPropertyID = Shader.PropertyToID(GRADIENT_PROPERTY);
        m_gradientSizePropertyID = Shader.PropertyToID(GRADIENT_SIZE_PROPERTY);

        m_gradientBuffer = new ComputeBuffer(m_colorGradient.colorKeys.Length, 5 * sizeof(float));
        float[] managedBuffer = new float[5 * m_colorGradient.colorKeys.Length];

        int i = 0;
        foreach(GradientColorKey key in m_colorGradient.colorKeys)
        {
            managedBuffer[i++] = key.time;
            managedBuffer[i++] = key.color.r;
            managedBuffer[i++] = key.color.g;
            managedBuffer[i++] = key.color.b;
            managedBuffer[i++] = key.color.a;
        }
        m_gradientBuffer.SetData(managedBuffer);
        m_materialPropBlock.SetBuffer(m_gradientPropertyID, m_gradientBuffer);
        m_materialPropBlock.SetInt(m_gradientSizePropertyID, m_colorGradient.colorKeys.Length);

        m_currentViewport = ToVector(m_startViewport);
        m_targetViewport = m_currentViewport;
        m_materialPropBlock.SetVector(m_posAndSizeProprtyID, m_currentViewport);

        m_renderer.SetPropertyBlock(m_materialPropBlock);

    }

    // Update is called once per frame
    void Update()
    {
        Vector2 posInput = -m_moveInput.action.ReadValue<Vector2>();
        Vector2 moveConstInput = m_moveConstInput.action.ReadValue<Vector2>();
        float zoomInput = m_zoomInput.action.ReadValue<float>();

        if (posInput != Vector2.zero)
        {
            Add2Vect2(ref m_targetViewport, posInput * m_moveSpeed * Time.deltaTime * m_zoom, Vector2.zero);
        }

        if (moveConstInput != Vector2.zero)
        {
            m_targetConst += moveConstInput * m_moveConstSpeed * Time.deltaTime * m_zoom;
            //m_targetConst = new Vector2(Mathf.Clamp01(m_targetConst.x), Mathf.Clamp01(m_targetConst.y));
        }

        if (zoomInput != 0.0f)
        {
            m_zoom *= 1 + zoomInput * m_zoomSpeed * Time.deltaTime;
            Vector2 newSize = m_startViewport.size * m_zoom;
            m_targetViewport.z = newSize.x;
            m_targetViewport.w = newSize.y;
        }

        m_currentViewport = Vector4.Lerp(m_currentViewport, m_targetViewport, m_lerpFactor * Time.deltaTime);
        m_materialPropBlock.SetVector(m_posAndSizeProprtyID, m_currentViewport);

        m_currentConst = Vector2.Lerp(m_currentConst, m_targetConst, m_lerpFactor * Time.deltaTime);
        m_materialPropBlock.SetFloat(m_cxProprtyID, m_currentConst.x);
        m_materialPropBlock.SetFloat(m_cyProprtyID, m_currentConst.y);

        m_renderer.SetPropertyBlock(m_materialPropBlock);
    }

    private void OnDestroy()
    {
        m_gradientBuffer.Release();
    }

    private Vector4 ToVector(Rect rect)
    {
        return new Vector4(rect.x, rect.y, rect.width, rect.height);
    }

    private Rect ToRect(Vector4 v)
    {
        return new Rect(v.z, v.y, v.z, v.w);
    }

    private void Add2Vect2(ref Vector4 v4, Vector2 v2a, Vector2 v2b)
    {
        v4.x += v2a.x;
        v4.y += v2a.y;
        v4.z += v2b.x;
        v4.w += v2b.y;
    }
}
