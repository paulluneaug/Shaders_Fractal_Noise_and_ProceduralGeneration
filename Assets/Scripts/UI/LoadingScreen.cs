using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUtility.Singletons;

public class LoadingScreen : SingletonMonoBehaviour<LoadingScreen>
{
    [SerializeField] private Image m_background;
    [SerializeField] private TMP_Text m_message;

    [NonSerialized] private bool m_fading = false;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        gameObject.SetActive(false);
        m_fading = false;
    }

    public void SetMessage(string message)
    {
        m_message.text = message;
    }

    public void FadeIn(float fadeInTime, Action onFadeOver)
    {
        if (m_fading)
        {
            return;
        }
        Color targetColor = m_background.color;
        targetColor.a = 1.0f;
        gameObject.SetActive(true);
        StartCoroutine(FadeCoroutine(fadeInTime, targetColor, onFadeOver));
    }

    public void FadeOut(float fadeOutTime, Action onFadeOver)
    {
        if (m_fading)
        {
            return;
        }
        Color targetColor = m_background.color;
        targetColor.a = 0.0f;

        void onFadeOutOver()
        {
            onFadeOver();
            gameObject.SetActive(false);
        }

        StartCoroutine(FadeCoroutine(fadeOutTime, targetColor, onFadeOutOver));
    }

    private IEnumerator FadeCoroutine(float fadeTime, Color targetColor, Action onFadeOver)
    {
        m_fading = true;
        float fadeTimer = 0.0f;

        Color startColor = m_background.color;

        while (fadeTimer < fadeTime)
        {
            m_background.color = Color.Lerp(startColor, targetColor, fadeTimer / fadeTime);
            yield return new WaitForEndOfFrame();
            fadeTimer += Time.deltaTime;
        }

        onFadeOver(); 
        m_fading = false;
    }
}
