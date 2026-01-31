using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

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

    [Header("悬停设置")]
    [Tooltip("悬停时卡牌放大倍数")]
    [SerializeField] private float hoverScale = 1.3f;
    [Tooltip("悬停动画时间")]
    [SerializeField] private float hoverAnimDuration = 0.1f;
    [Tooltip("悬停描边颜色")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [Tooltip("悬停描边粗细")]
    [SerializeField] private float outlineThickness = 3f;

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

    // 在卡槽中的堆叠索引（-1表示不在堆栈中，0是底牌，1+是叠加牌）
    public int StackIndex { get; set; } = -1;

    // 是否来自牌库展示区
    public bool IsFromDeckShop { get; set; } = false;

    // 悬停状态
    private bool isHovering = false;
    private bool isDragging = false;

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
        isDragging = true;

        // 如果正在悬停，先取消悬停效果
        if (isHovering)
        {
            CancelHover();
        }

        // 记录原始状态
        originalPosition = transform.position;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 记录之前所在的卡槽
        previousSlot = CurrentSlot;

        // 如果在卡槽中，检查是否是最顶部的卡牌
        if (CurrentSlot != null)
        {
            CardUI topCard = CurrentSlot.GetTopCard();
            if (topCard != this)
            {
                // 不是最顶部的卡牌，取消拖拽
                Debug.Log("[CardUI] 只能拖拽最顶部的卡牌！");
                isDragging = false;
                return;
            }

            // 从卡槽中移除
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
        isDragging = false;

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
                // 没有放到有效位置，直接返回手牌
                bool returnedToHand = DeckManager.Instance?.TryReturnCardToHand(this) ?? false;

                if (!returnedToHand)
                {
                    // 如果也无法返回手牌，尝试返回原卡槽
                    if (previousSlot != null)
                    {
                        if (previousSlot.IsEmpty)
                        {
                            previousSlot.PlaceCard(this);
                        }
                        else if (previousSlot.CanStackCardUI(this))
                        {
                            previousSlot.StackCard(this);
                        }
                    }
                }
            }
        }

        // 清除记录
        previousSlot = null;
    }

    #endregion

    #region 悬停效果

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;

        isHovering = true;

        // 停止之前的动画
        DOTween.Kill(transform);

        // 添加临时Canvas确保在最上层显示
        Canvas tempCanvas = GetComponent<Canvas>();
        if (tempCanvas == null)
        {
            tempCanvas = gameObject.AddComponent<Canvas>();
        }
        tempCanvas.overrideSorting = true;
        tempCanvas.sortingOrder = 100;

        // 确保有GraphicRaycaster才能接收点击
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // 添加描边效果
        ShowOutline(true);

        // 只放大，不移动位置
        transform.DOScale(hoverScale, hoverAnimDuration).SetEase(Ease.OutCubic);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;

        if (isHovering)
        {
            RestoreFromHover();
        }
    }

    /// <summary>
    /// 从悬停状态恢复
    /// </summary>
    private void RestoreFromHover()
    {
        isHovering = false;

        // 停止之前的动画
        DOTween.Kill(transform);

        // 恢复Canvas排序
        Canvas tempCanvas = GetComponent<Canvas>();
        if (tempCanvas != null)
        {
            tempCanvas.overrideSorting = false;
        }

        // 移除描边效果
        ShowOutline(false);

        // 恢复大小
        transform.DOScale(1f, hoverAnimDuration).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 取消悬停（开始拖拽时调用）
    /// </summary>
    private void CancelHover()
    {
        isHovering = false;
        DOTween.Kill(transform);
        transform.localScale = Vector3.one;

        // 恢复Canvas排序
        Canvas tempCanvas = GetComponent<Canvas>();
        if (tempCanvas != null)
        {
            tempCanvas.overrideSorting = false;
        }

        // 移除描边效果
        ShowOutline(false);
    }

    /// <summary>
    /// 显示/隐藏描边效果
    /// </summary>
    private void ShowOutline(bool show)
    {
        // 获取要添加描边的目标（cardBackground 或自身）
        Image targetImage = cardBackground != null ? cardBackground : GetComponent<Image>();

        if (targetImage == null) return;

        Outline outline = targetImage.GetComponent<Outline>();

        if (show)
        {
            // 添加或启用描边
            if (outline == null)
            {
                outline = targetImage.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(outlineThickness, -outlineThickness);
            outline.useGraphicAlpha = false;
            outline.enabled = true;

            // 添加多个Outline组件实现更完整的描边（四个方向）
            Outline[] outlines = targetImage.GetComponents<Outline>();
            if (outlines.Length < 4)
            {
                // 添加额外的Outline组件覆盖四个角
                for (int i = outlines.Length; i < 4; i++)
                {
                    Outline newOutline = targetImage.gameObject.AddComponent<Outline>();
                    newOutline.effectColor = outlineColor;
                    newOutline.useGraphicAlpha = false;

                    // 四个方向的偏移
                    switch (i)
                    {
                        case 0: newOutline.effectDistance = new Vector2(outlineThickness, outlineThickness); break;
                        case 1: newOutline.effectDistance = new Vector2(-outlineThickness, outlineThickness); break;
                        case 2: newOutline.effectDistance = new Vector2(outlineThickness, -outlineThickness); break;
                        case 3: newOutline.effectDistance = new Vector2(-outlineThickness, -outlineThickness); break;
                    }
                }
            }

            // 启用所有Outline
            foreach (var o in targetImage.GetComponents<Outline>())
            {
                o.effectColor = outlineColor;
                o.enabled = true;
            }
        }
        else
        {
            // 禁用所有Outline
            Outline[] outlines = targetImage.GetComponents<Outline>();
            foreach (var o in outlines)
            {
                o.enabled = false;
            }
        }
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

    private void OnDisable()
    {
        // 清理DOTween动画
        DOTween.Kill(transform);
    }
}