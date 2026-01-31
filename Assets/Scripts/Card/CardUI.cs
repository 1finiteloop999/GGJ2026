using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌UI - 负责卡牌的显示和拖拽
/// </summary>
public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI引用")]
    [Tooltip("卡牌背景图片（显示整张卡牌美术）")]
    [SerializeField] private Image cardBackground;

    [Header("拖拽设置")]
    [SerializeField] private float dragScale = 1.1f;

    // 卡牌数据
    public CardData CardData { get; private set; }

    // 原始位置和父物体
    private Vector3 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    // 当前所在的卡槽（如果有）
    public CardSlot CurrentSlot { get; set; }

    // 是否来自牌库展示区
    public bool IsFromDeckShop { get; set; } = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 自动获取背景图片
        if (cardBackground == null)
        {
            cardBackground = GetComponent<Image>();
        }
    }

    /// <summary>
    /// 初始化卡牌
    /// </summary>
    public void Setup(CardData data)
    {
        CardData = data;

        // 设置卡牌图片
        if (cardBackground != null && data.cardSprite != null)
        {
            cardBackground.sprite = data.cardSprite;
            cardBackground.color = Color.white; // 确保颜色正常显示
        }
    }

    // 拖拽前是否在卡槽中
    private CardSlot previousSlot;

    #region 拖拽事件

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 记录原始状态
        originalPosition = transform.position;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 记录之前所在的卡槽
        previousSlot = CurrentSlot;

        // 如果从卡槽中拖出，先清除卡槽状态
        if (CurrentSlot != null)
        {
            CurrentSlot.RemoveCard();
        }

        // 检查是否来自牌库展示区
        if (DeckShop.Instance != null && DeckShop.Instance.IsFromDeckDisplay(this))
        {
            IsFromDeckShop = true;
        }

        // 设置拖拽状态
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;
        transform.localScale = Vector3.one * dragScale;

        // 移动到Canvas最上层确保可见
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        // 通知DeckManager
        DeckManager.Instance?.OnCardBeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 跟随鼠标
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复状态
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;

        // 如果是从牌库拖出的卡牌
        if (IsFromDeckShop)
        {
            // 尝试购买（通过DeckManager处理）
            bool handled = DeckManager.Instance?.OnDeckCardEndDrag(this, eventData) ?? false;

            if (!handled)
            {
                // 购买失败，返回牌库展示区
                ReturnToOriginalPosition();
            }

            IsFromDeckShop = false;
        }
        else
        {
            // 普通卡牌拖拽
            bool handled = DeckManager.Instance?.OnCardEndDrag(this, eventData, previousSlot) ?? false;

            if (!handled)
            {
                // 没有放到有效位置，尝试返回手牌
                DeckManager.Instance?.TryReturnCardToHand(this);
            }
        }

        // 清除记录
        previousSlot = null;
    }

    #endregion

    #region 悬停效果

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    #endregion

    /// <summary>
    /// 返回原始位置
    /// </summary>
    public void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.position = originalPosition;
    }

    /// <summary>
    /// 移动到指定父物体
    /// </summary>
    public void SetParentAndReset(Transform newParent)
    {
        transform.SetParent(newParent);
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;
    }
}