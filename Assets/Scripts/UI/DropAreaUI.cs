using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 拖放区域类型
/// </summary>
public enum DropAreaType
{
    Sell,   // 出售区域
    Use     // 使用区域
}

/// <summary>
/// 拖放区域UI - 管理Sell和Use区域的图片状态切换和音效
/// </summary>
public class DropAreaUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("区域设置")]
    [Tooltip("区域类型")]
    [SerializeField] private DropAreaType areaType = DropAreaType.Sell;

    [Header("图片设置")]
    [Tooltip("区域图片组件")]
    [SerializeField] private Image areaImage;

    [Tooltip("默认状态图片")]
    [SerializeField] private Sprite normalSprite;

    [Tooltip("激活状态图片（可放入时）")]
    [SerializeField] private Sprite activeSprite;

    [Header("音效设置")]
    [Tooltip("成功操作音效")]
    [SerializeField] private AudioClip successSound;

    [Tooltip("音效播放器（可选，如果为空则自动查找或创建）")]
    [SerializeField] private AudioSource audioSource;

    // 当前是否处于激活状态
    private bool isActive = false;

    // 当前悬停的卡牌
    private CardUI hoveringCard = null;

    private void Awake()
    {
        // 自动获取Image组件
        if (areaImage == null)
        {
            areaImage = GetComponent<Image>();
        }

        // 确保有AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    private void Start()
    {
        // 设置默认图片
        SetNormalState();
    }

    /// <summary>
    /// 检查卡牌是否可以放入此区域
    /// </summary>
    private bool CanAcceptCard(CardUI card)
    {
        if (card == null || card.CardData == null) return false;

        CardData cardData = card.CardData;

        switch (areaType)
        {
            case DropAreaType.Sell:
                // 出售区域：所有卡牌都可以出售
                return true;

            case DropAreaType.Use:
                // 使用区域：只接受法术牌
                return cardData.IsSpellCard;

            default:
                return false;
        }
    }

    /// <summary>
    /// 设置为默认状态
    /// </summary>
    public void SetNormalState()
    {
        isActive = false;
        hoveringCard = null;

        if (areaImage != null && normalSprite != null)
        {
            areaImage.sprite = normalSprite;
        }
    }

    /// <summary>
    /// 设置为激活状态
    /// </summary>
    public void SetActiveState()
    {
        isActive = true;

        if (areaImage != null && activeSprite != null)
        {
            areaImage.sprite = activeSprite;
        }
    }

    /// <summary>
    /// 播放成功音效
    /// </summary>
    public void PlaySuccessSound()
    {
        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }
    }

    #region 指针事件

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 检查是否有卡牌正在拖拽
        if (eventData.pointerDrag != null)
        {
            CardUI draggedCard = eventData.pointerDrag.GetComponent<CardUI>();

            if (draggedCard != null && CanAcceptCard(draggedCard))
            {
                hoveringCard = draggedCard;
                SetActiveState();
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复默认状态
        SetNormalState();
    }

    #endregion

    /// <summary>
    /// 通知操作成功（由DeckManager调用）
    /// </summary>
    public void OnOperationSuccess()
    {
        PlaySuccessSound();
        SetNormalState();
    }

    /// <summary>
    /// 获取区域类型
    /// </summary>
    public DropAreaType AreaType => areaType;
}