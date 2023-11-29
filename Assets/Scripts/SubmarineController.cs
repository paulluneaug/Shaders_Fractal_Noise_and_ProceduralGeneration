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

    [Header("Direction Settings")]
    [SerializeField, Range(0.0f, 180.0f)] private float m_xRotationLimit = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_directionInertia = 1.0f;


    [Header("Inputs")]
    [SerializeField] private InputActionReference m_directionInputAction = null;
    [SerializeField] private InputActionReference m_throttleInputAction = null;
    [SerializeField] private InputActionReference m_verticalInputAction = null;

    // Cache
    [NonSerialized] private Quaternion m_localTargetRotation = Quaternion.identity;

    [NonSerialized] private float m_currentForwardSpeed = 0.0f;
    [NonSerialized] private float m_currentVerticalSpeed = 0.0f;
    [NonSerialized] private float m_targetForwardSpeed = 0.0f;
    [NonSerialized] private float m_targetVerticalSpeed = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        m_directionInputAction.action.actionMap.Enable();
        m_localTargetRotation = m_submarineTransform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 directionInput = m_directionInputAction.action.ReadValue<Vector2>();
        float throttleInput = m_throttleInputAction.action.ReadValue<float>();
        float verticalInput = m_verticalInputAction.action.ReadValue<float>();

        m_targetForwardSpeed = Mathf.Lerp(0.0f, m_forwardMaxSpeed, Mathf.Abs(throttleInput)) * Mathf.Sign(throttleInput);
        m_currentForwardSpeed = Mathf.MoveTowards(m_currentForwardSpeed, m_targetForwardSpeed, m_forwardAcceleration * Time.deltaTime * ForwardInertiaFactor);
        m_currentForwardSpeed = Mathf.Clamp(m_currentForwardSpeed, -m_forwardMaxSpeed, m_forwardMaxSpeed);

        m_targetVerticalSpeed = Mathf.Lerp(0.0f, m_verticalMaxSpeed, Mathf.Abs(verticalInput)) * Mathf.Sign(verticalInput);
        m_currentVerticalSpeed = Mathf.MoveTowards(m_currentVerticalSpeed, m_targetVerticalSpeed, m_verticalAcceleration * Time.deltaTime * VerticalInertiaFactor);
        m_currentVerticalSpeed = Mathf.Clamp(m_currentVerticalSpeed, -m_verticalMaxSpeed, m_verticalMaxSpeed);

        Quaternion currentLocalRotation = m_submarineTransform.localRotation;
        Vector3 currentLocalEuler = currentLocalRotation.eulerAngles;
        Vector2 currentXYRot = new Vector2(currentLocalEuler.x, currentLocalEuler.y);

        Vector2 targetXYRot = currentXYRot + directionInput.normalized;

        m_localTargetRotation *= Quaternion.AngleAxis(12, Vector3.one);




        m_submarineTransform.position += m_submarineTransform.forward * m_currentForwardSpeed + Vector3.up * m_currentVerticalSpeed;
    }
}
