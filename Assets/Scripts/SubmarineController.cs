using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SubmarineController : MonoBehaviour
{
    private float ForwardInertiaFactor => 1.0f - m_forwardInertia;
    private float VerticalInertiaFactor => 1.0f - m_verticalInertia;

    [SerializeField] private Transform m_submarineTransform = null;

    [Header("Forward Settings")]
    [SerializeField] private float m_forwardMaxSpeed = 1.0f;
    [SerializeField] private float m_forwardAcceleration = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_forwardInertia = 1.0f;

    [Header("Vertical Settings")]
    [SerializeField] private float m_verticalMaxSpeed = 1.0f;
    [SerializeField] private float m_verticalAcceleration = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_verticalInertia = 1.0f;

    [Header("Inputs")]
    [SerializeField] private InputActionReference m_directionInputAction = null;
    [SerializeField] private InputActionReference m_throttleInputAction = null;
    [SerializeField] private InputActionReference m_verticalInputAction = null;

    // Cache
    [NonSerialized] private Vector3 m_targetForward = Vector3.zero;

    [NonSerialized] private float m_currentForwardSpeed = 0.0f;
    [NonSerialized] private float m_currentVerticalSpeed = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        m_directionInputAction.action.actionMap.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 directionInput = m_directionInputAction.action.ReadValue<Vector2>();
        float throttleInput = m_throttleInputAction.action.ReadValue<float>();
        float verticalInput = m_verticalInputAction.action.ReadValue<float>();

        m_currentForwardSpeed = Mathf.Lerp(0.0f, m_forwardMaxSpeed, Mathf.Abs(throttleInput) * ForwardInertiaFactor * Time.deltaTime) * Mathf.Sign(throttleInput);
        m_currentForwardSpeed = Math.Clamp(m_currentForwardSpeed, -m_forwardMaxSpeed, m_forwardMaxSpeed);

        m_currentVerticalSpeed = Mathf.Lerp(0.0f, m_verticalAcceleration, Mathf.Abs(verticalInput) * VerticalInertiaFactor * Time.deltaTime) * Mathf.Sign(verticalInput);
        m_currentVerticalSpeed = Math.Clamp(m_currentVerticalSpeed, -m_verticalMaxSpeed, m_verticalMaxSpeed);




        m_submarineTransform.position += m_submarineTransform.forward * m_currentForwardSpeed + Vector3.up * m_currentVerticalSpeed;
    }
}
