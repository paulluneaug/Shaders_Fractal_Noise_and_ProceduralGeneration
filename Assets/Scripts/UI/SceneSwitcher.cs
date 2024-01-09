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

    private void OnEnable()
    {
        m_scene = SceneManager.GetSceneByName(m_sceneToSwitchTo);
        if (!m_scene.IsValid())
        {
            Debug.LogError($"Could not find the scene {m_sceneToSwitchTo}");
        }
    }

    public void SwitchToScene()
    {
        if (m_scene.IsValid())
        {
            LoadingScreen.Instance.FadeIn(m_fadeTime, OnFadeInOver);
        }
    }

    private void OnSwitchOver(AsyncOperation op)
    {
        LoadingScreen.Instance.FadeOut(m_fadeTime, () => { });
        op.completed -= OnSwitchOver;
    }

    private void OnFadeInOver()
    {
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        SceneManager.LoadScene(m_scene.buildIndex);
        unloadOp.completed += OnSwitchOver;
    }
}
