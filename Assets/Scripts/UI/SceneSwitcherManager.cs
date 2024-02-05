using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneSwitcherManager : MonoBehaviour
{
    [SerializeField] private RectTransform m_sceneSwitcherRoot;
    [SerializeField] private InputActionReference m_openMenuKey;

    private void Start()
    {
        LoadingScreen.Instance.OnFadeOver += OnFadeOver;
        m_openMenuKey.action.actionMap.Enable();
        m_openMenuKey.action.performed += OnOpenMenuPerformed;
    }

    private void OnOpenMenuPerformed(InputAction.CallbackContext context)
    {
        m_sceneSwitcherRoot.gameObject.SetActive(!m_sceneSwitcherRoot.gameObject.activeSelf);
    }

    private void OnFadeOver(bool fadedIn)
    {
        if (fadedIn)
        {
            m_sceneSwitcherRoot.gameObject.SetActive(false);
        }
    }
}
