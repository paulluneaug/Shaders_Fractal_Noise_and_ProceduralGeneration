using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSceneSwitcher : SceneSwitcher
{
    [SerializeField] private Button m_button;

    [NonSerialized] private Image m_buttonImage = null;
    [NonSerialized] private TMP_Text m_buttonText = null;

    public virtual void SetSpriteAndText(Sprite sprite, string text)
    {
        m_buttonImage.sprite = sprite;
        m_buttonText.text = text;
    }

    protected override void Awake()
    {
        base.Awake();
        m_button.onClick.AddListener(OnButtonClicked);

        m_buttonImage = GetComponent<Image>();
        m_buttonText = GetComponentInChildren<TMP_Text>();
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
