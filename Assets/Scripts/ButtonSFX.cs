using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按钮音效组件 - 挂到Button上自动播放点击音效
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    [Tooltip("Set to false to skip button click sound (e.g. for deck button)")]
    [SerializeField] private bool playClickSound = true;
    
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        
        if (button != null && playClickSound)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        SoundManager.Instance?.PlayButtonClick();
    }
}
