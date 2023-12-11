using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Axis
{
    X = 0,
    Y = 1,
    Z = 2,
}

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
    [SerializeField, Range(0.0f, 90.0f)] private float m_xRotationLimit = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_directionInertia = 1.0f;

    [Header("Camera Settings")]
    [SerializeField] private Transform m_camera = null;
    [SerializeField] private Transform m_cameraTargetPosition = null;
    [SerializeField] private Transform m_cameraFarLookAtPosition = null;
    [SerializeField] private Transform m_cameraCloseLookAtPosition = null;
    [SerializeField] private LayerMask m_cameraGoThroughLayers = default;

    [Header("Inputs")]
    [SerializeField] private InputActionReference m_directionInputAction = null;
    [SerializeField] private InputActionReference m_throttleInputAction = null;
    [SerializeField] private InputActionReference m_verticalInputAction = null;

    [Header("Model")]
    [SerializeField] private Transform m_propeller = null;
    [SerializeField] private Axis m_propellerRotationAxis = Axis.Z;
    [SerializeField] private float m_propellerMaxSpeed = 10.0f;


    // Cache
    [NonSerialized] private Vector3 m_currentLocalEuler = Vector3.zero;
    [NonSerialized] private Vector2 m_currentDirectionSpeed = Vector2.zero;
    [NonSerialized] private float m_currentForwardSpeed = 0.0f;
    [NonSerialized] private float m_currentVerticalSpeed = 0.0f;

    [NonSerialized] private float m_maxCameraDistance = 0.0f;

    void Start()
    {
        m_directionInputAction.action.actionMap.Enable();
        m_maxCameraDistance = (m_cameraTargetPosition.position - m_submarineTransform.position).magnitude;
        m_currentLocalEuler = m_submarineTransform.localEulerAngles;
    }

    void Update()
    {
        m_currentLocalEuler += new Vector3(m_currentDirectionSpeed.y, m_currentDirectionSpeed.x, 0) * Time.deltaTime;
        m_currentLocalEuler.x = Mathf.Clamp(m_currentLocalEuler.x, -m_xRotationLimit, m_xRotationLimit);
        m_submarineTransform.localEulerAngles = m_currentLocalEuler;

        m_submarineTransform.position +=
            m_submarineTransform.forward * m_currentForwardSpeed * Time.deltaTime +
            Vector3.up * m_currentVerticalSpeed * Time.deltaTime;

        UpdateCameraPosition();
        UpdatePropeller();
    }

    private void FixedUpdate()
    {
        Vector2 directionInput = m_directionInputAction.action.ReadValue<Vector2>();
        float throttleInput = m_throttleInputAction.action.ReadValue<float>();
        float verticalInput = m_verticalInputAction.action.ReadValue<float>();

        float targetForwardSpeed = GetTargetSpeed(m_forwardMaxSpeed, throttleInput);
        m_currentForwardSpeed = Mathf.MoveTowards(m_currentForwardSpeed, targetForwardSpeed, m_forwardAcceleration * Time.fixedDeltaTime * ForwardInertiaFactor);
        m_currentForwardSpeed = Mathf.Clamp(m_currentForwardSpeed, -m_forwardMaxSpeed, m_forwardMaxSpeed);

        float targetVerticalSpeed = GetTargetSpeed(m_verticalMaxSpeed, verticalInput);
        m_currentVerticalSpeed = Mathf.MoveTowards(m_currentVerticalSpeed, targetVerticalSpeed, m_verticalAcceleration * Time.fixedDeltaTime * VerticalInertiaFactor);
        m_currentVerticalSpeed = Mathf.Clamp(m_currentVerticalSpeed, -m_verticalMaxSpeed, m_verticalMaxSpeed);

        Vector2 targetDirectionSpeed = new Vector2(
            GetTargetSpeed(m_directionMaxSpeed, directionInput.x),
            GetTargetSpeed(m_directionMaxSpeed, directionInput.y));

        m_currentDirectionSpeed = Vector2.MoveTowards(m_currentDirectionSpeed, targetDirectionSpeed, m_directionAcceleration * Time.deltaTime * DirectionInertiaFactor);
        m_currentDirectionSpeed.x = Mathf.Clamp(m_currentDirectionSpeed.x, -m_directionMaxSpeed, m_directionMaxSpeed);
        m_currentDirectionSpeed.y = Mathf.Clamp(m_currentDirectionSpeed.y, -m_directionMaxSpeed, m_directionMaxSpeed);
    }

    private void UpdateCameraPosition()
    {
        Vector3 camPosition;

        Vector3 direction = m_cameraTargetPosition.position - m_submarineTransform.position;
        if (Physics.Raycast(m_submarineTransform.position, direction, out RaycastHit hit, m_maxCameraDistance, ~m_cameraGoThroughLayers))
        {
            camPosition = hit.point;
        }
        else
        {
            camPosition = m_cameraTargetPosition.position;
        }
        m_camera.position = camPosition;

        float factor = (camPosition - m_submarineTransform.position).sqrMagnitude / (m_maxCameraDistance * m_maxCameraDistance);

        Vector3 lookAtPosition = Vector3.Lerp(m_cameraCloseLookAtPosition.position, m_cameraFarLookAtPosition.position, factor);

        m_camera.LookAt(lookAtPosition);
    }

    private void UpdatePropeller()
    {
        m_propeller.Rotate(m_propellerRotationAxis.GetVector(), m_propellerMaxSpeed * m_currentForwardSpeed * Time.deltaTime);
    }

    private static float GetTargetSpeed(float maxSpeed, float input)
    {
        return Mathf.Lerp(0.0f, maxSpeed, Mathf.Abs(input)) * Mathf.Sign(input);
    }
}

public static class AxisExtension
{
    public static Vector3 GetVector(this Axis axis)
    {
        return axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _ => throw new NotImplementedException(),
        };
    }
}
