using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityUtility.SceneReference;

public class SceneButtonsManager : MonoBehaviour
{
    [Serializable] 
    private struct DisplayableScene
    {
        public SceneReference Scene;
        public string DisplayName;
        public Sprite Sprite;
    }

    [SerializeField] private DisplayableScene[] m_displayableScenes = null;
    [SerializeField] private ButtonSceneSwitcher m_buttonPrefab = null;

    [NonSerialized] private List<ButtonSceneSwitcher> m_buttonPool = new List<ButtonSceneSwitcher>();

    private void Awake()
    {
        m_buttonPool = GetComponentsInChildren<ButtonSceneSwitcher>().ToList();
    }

    private void OnEnable()
    {
        m_buttonPool.ForEach(t => t.gameObject.SetActive(false));

        int displayedScenes = 0;
        foreach (DisplayableScene scene in m_displayableScenes)
        {
            if (SceneManager.GetActiveScene().path == scene.Scene.ScenePath)
            {
                continue;
            }

            if (m_buttonPool.Count <= displayedScenes)
            {
                ButtonSceneSwitcher newButton = Instantiate(m_buttonPrefab);
                newButton.gameObject.name = newButton.gameObject.name.Replace("(Clone)", displayedScenes.ToString());
                newButton.transform.SetParent(transform, false);
                newButton.transform.localScale = Vector3.one;
                m_buttonPool.Add(newButton);
            }
            ButtonSceneSwitcher button = m_buttonPool[displayedScenes];
            button.gameObject.SetActive(true);

            button.SetSceneToLoad(scene.Scene);
            button.SetSpriteAndText(scene.Sprite, scene.DisplayName);

            displayedScenes++;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}
