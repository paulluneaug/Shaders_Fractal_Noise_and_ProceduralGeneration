using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SubmarineController : MonoBehaviour
{
    private float DirectionInertiaFactor => 1.0f - m_directionInertia;
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
    [SerializeField] private float m_directionMaxSpeed = 1.0f;
    [SerializeField] private float m_directionAcceleration = 1.0f;
    [SerializeField, Range(0.0f, 180.0f)] private float m_xRotationLimit = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_directionInertia = 1.0f;


    [Header("Inputs")]
    [SerializeField] private InputActionReference m_directionInputAction = null;
    [SerializeField] private InputActionReference m_throttleInputAction = null;
    [SerializeField] private InputActionReference m_verticalInputAction = null;

    // Cache
    [NonSerialized] private Quaternion m_localTargetRotation = Quaternion.identity;

    [NonSerialized] private Vector2 m_currentDirectionSpeed = Vector2.zero;
    [NonSerialized] private float m_currentForwardSpeed = 0.0f;
    [NonSerialized] private float m_currentVerticalSpeed = 0.0f;

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

        float targetForwardSpeed = GetTargetSpeed(m_forwardMaxSpeed, throttleInput);
        m_currentForwardSpeed = Mathf.MoveTowards(m_currentForwardSpeed, targetForwardSpeed, m_forwardAcceleration * Time.deltaTime * ForwardInertiaFactor);
        m_currentForwardSpeed = Mathf.Clamp(m_currentForwardSpeed, -m_forwardMaxSpeed, m_forwardMaxSpeed);

        float targetVerticalSpeed = GetTargetSpeed(m_verticalMaxSpeed, verticalInput);
        m_currentVerticalSpeed = Mathf.MoveTowards(m_currentVerticalSpeed, targetVerticalSpeed, m_verticalAcceleration * Time.deltaTime * VerticalInertiaFactor);
        m_currentVerticalSpeed = Mathf.Clamp(m_currentVerticalSpeed, -m_verticalMaxSpeed, m_verticalMaxSpeed);


        Vector2 targetDirectionSpeed = new Vector2(
            GetTargetSpeed(m_directionMaxSpeed, directionInput.x),
            GetTargetSpeed(m_directionMaxSpeed, directionInput.y));

        m_currentDirectionSpeed = Vector2.MoveTowards(m_currentDirectionSpeed, targetDirectionSpeed, m_directionAcceleration * Time.deltaTime * DirectionInertiaFactor);
        m_currentDirectionSpeed.x = Mathf.Clamp(m_currentDirectionSpeed.x, -m_directionMaxSpeed, m_directionMaxSpeed);
        m_currentDirectionSpeed.y = Mathf.Clamp(m_currentDirectionSpeed.y, -m_directionMaxSpeed, m_directionMaxSpeed);

        m_submarineTransform.localEulerAngles += new Vector3(m_currentDirectionSpeed.y, m_currentDirectionSpeed.x, 0);

        m_submarineTransform.position += m_submarineTransform.forward * m_currentForwardSpeed + Vector3.up * m_currentVerticalSpeed;
    }

    private static float GetTargetSpeed(float maxSpeed, float input)
    {
        return Mathf.Lerp(0.0f, maxSpeed, Mathf.Abs(input)) * Mathf.Sign(input);
    }
}
