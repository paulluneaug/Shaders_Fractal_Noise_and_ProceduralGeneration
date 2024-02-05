using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSceneSwitcher : SceneSwitcher
{
    [SerializeField] private Button m_button;

    [SerializeField] private Image m_buttonImage = null;
    [SerializeField] private TMP_Text m_buttonText = null;

    public virtual void SetSpriteAndText(Sprite sprite, string text)
    {
        m_buttonImage.sprite = sprite;
        m_buttonText.text = text;
    }

    protected override void Awake()
    {
        base.Awake();
        m_button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        m_button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        SwitchToScene();
    }
}
