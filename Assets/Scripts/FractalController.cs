using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FractalController : MonoBehaviour
{
    private const string C_X_PROPERTY = "_Cx";
    private const string C_Y_PROPERTY = "_Cy";
    private const string MAX_ITERATIONS_PROPERTY = "_MaxIterations";
    private const string TRESHOLD_PROPERTY = "_Treshold";
    private const string POS_AND_SIZE_PROPERTY = "_PosAndSize";

    [SerializeField] private Material m_material;
    [SerializeField] private Rect m_startViewport = new Rect();

    [SerializeField] private float m_moveSpeed = 1.0f;
    [SerializeField] private float m_zoomSpeed = 1.0f;

    [SerializeField] private InputActionReference m_moveInput = null;
    [SerializeField] private InputActionReference m_zoomInInput = null;
    [SerializeField] private InputActionReference m_zoomOutInput = null;

    [NonSerialized] private Rect m_currentViewport;
    [NonSerialized] private float m_zoom = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        if (m_moveInput.action.expectedControlType != "Vector2" ||
            m_zoomInInput.action.expectedControlType != "Axis" ||
            m_zoomOutInput.action.expectedControlType != "Axis")
        {
            throw new Exception("Invalid controlType");
        }
        m_moveInput.action.actionMap.Enable();

        m_currentViewport = m_startViewport;
        m_material.SetVector(POS_AND_SIZE_PROPERTY, ToVector(m_currentViewport));

    }

    // Update is called once per frame
    void Update()
    {
        Vector2 posInput = m_moveInput.action.ReadValue<Vector2>();
        float zoomInput = m_zoomInInput.action.ReadValue<float>() - m_zoomOutInput.action.ReadValue<float>();

        bool viewportMoved = false;

        if (posInput != Vector2.zero)
        {
            viewportMoved = true;
            m_currentViewport.position += posInput * m_moveSpeed * Time.deltaTime * m_zoom;
        }

        if (zoomInput != 0.0f)
        {
            viewportMoved = true;
            m_zoom += zoomInput * m_zoomSpeed * Time.deltaTime;
            m_currentViewport.size = m_startViewport.size * m_zoom;
        }

        if (viewportMoved)
        {
            m_material.SetVector(POS_AND_SIZE_PROPERTY, ToVector(m_currentViewport));
        }
    }

    private Vector4 ToVector(Rect rect)
    {
        return new Vector4(rect.x, rect.y, rect.width, rect.height);
    }
}
