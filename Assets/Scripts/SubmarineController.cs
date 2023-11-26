using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SubmarineController : MonoBehaviour
{
    [SerializeField] private Transform m_submarineTransform = null;

    [Header("Inputs")]
    [SerializeField] private InputActionReference m_directionInput = null;
    [SerializeField] private InputActionReference m_throttleInput = null;
    [SerializeField] private InputActionReference m_verticalInput = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
