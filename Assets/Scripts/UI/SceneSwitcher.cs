using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private string m_sceneToSwitchTo = string.Empty;
    [SerializeField] private float m_fadeTime = 1.0f;

    [NonSerialized] private Scene m_scene;

    protected virtual void Awake()
    {
    }

    public void SetSceneToLoad(string sceneName)
    {
        m_sceneToSwitchTo = sceneName;
    }

    public void SwitchToScene()
    {
        LoadingScreen.Instance.FadeIn(m_fadeTime, OnFadeInOver);
    }

    private void CheckScene()
    {
        if (!m_scene.IsValid())
        {
            Debug.LogError($"Could not find the scene {m_sceneToSwitchTo}");
        }
    }

    private void OnSwitchOver(AsyncOperation op)
    {
        LoadingScreen.Instance.FadeOut(m_fadeTime, () => { });
        op.completed -= OnSwitchOver;
    }

    private void OnFadeInOver()
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(m_sceneToSwitchTo, LoadSceneMode.Single);
        loadOp.completed += OnSwitchOver;
    }
}
