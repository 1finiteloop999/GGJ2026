using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 卡牌UI组件
/// 悬停时：上移 + Q弹放大
/// 移开时：恢复原位 + 恢复原大小
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CardUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("组件引用")]
    [SerializeField] private Image cardImage;           // 卡牌图片
    [SerializeField] private Image cardFrame;           // 卡牌边框

    [Header("悬停设置")]
    [SerializeField] private float hoverScale = 1.15f;   // 悬停放大倍数
    [SerializeField] private float hoverYOffset = 30f;   // 悬停时向上偏移
    [SerializeField] private float hoverDuration = 0.2f; // 动画时长

    [Header("拖拽设置")]
    [SerializeField] private float dragAlpha = 0.8f;

    // 私有变量
    private CardData cardData;
    private CardContainer currentContainer;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    // 状态
    private bool isHovering = false;
    private bool isDragging = false;
    private int baseSortOrder = 0;

    // ★ 原始缩放（固定为1）
    private readonly Vector3 originalScale = Vector3.one;

    // ★ 排列位置（由Container设置）
    private Vector3 arrangedPosition = Vector3.zero;

    // 拖拽相关
    private Transform dragParent;
    private Vector2 dragOffset;

    // 事件
    public System.Action<CardUI> OnCardClicked;
    public System.Action<CardUI> OnDragStarted;
    public System.Action<CardUI> OnDragEnded;

    #region 属性

    public CardData GetCardData() => cardData;
    public CardContainer GetContainer() => currentContainer;
    public bool IsDragging => isDragging;
    public bool IsHovering => isHovering;

    #endregion

    #region 初始化

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (cardImage == null)
            cardImage = transform.Find("CardImage")?.GetComponent<Image>();
        if (cardFrame == null)
            cardFrame = GetComponent<Image>();

        // 确保初始缩放为1
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 初始化卡牌数据
    /// </summary>
    public void Initialize(CardData data)
    {
        cardData = data;

        // 设置颜色
        if (cardImage != null)
        {
            cardImage.color = data.cardColor;
            if (data.cardImage != null)
                cardImage.sprite = data.cardImage;
        }

        // 如果没有子Image，设置自身颜色
        if (cardImage == null && cardFrame != null)
        {
            cardFrame.color = data.cardColor;
        }
    }

    #endregion

    #region Setter

    public void SetContainer(CardContainer container)
    {
        currentContainer = container;
    }

    public void SetDragParent(Transform parent)
    {
        dragParent = parent;
    }

    public void SetBaseSortOrder(int order)
    {
        baseSortOrder = order;
        transform.SetSiblingIndex(order);
    }

    /// <summary>
    /// 设置排列位置
    /// </summary>
    public void SetArrangedPosition(Vector3 position)
    {
        arrangedPosition = position;
    }

    public Vector3 GetArrangedPosition() => arrangedPosition;

    #endregion

    #region 悬停交互

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;

        isHovering = true;

        // 停止之前的动画
        transform.DOKill();

        // 移到最上层显示
        transform.SetAsLastSibling();

        // 计算悬停位置
        Vector3 hoverPos = new Vector3(arrangedPosition.x, arrangedPosition.y + hoverYOffset, arrangedPosition.z);

        // ★ 同时执行：上移 + Q弹放大
        transform.DOLocalMove(hoverPos, hoverDuration).SetEase(Ease.OutQuad);
        transform.DOScale(originalScale * hoverScale, hoverDuration).SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;

        isHovering = false;

        // 停止之前的动画
        transform.DOKill();

        // 恢复层级
        transform.SetSiblingIndex(baseSortOrder);

        // ★ 同时执行：回到原位 + 恢复原大小
        transform.DOLocalMove(arrangedPosition, hoverDuration).SetEase(Ease.OutQuad);
        transform.DOScale(originalScale, hoverDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;

        // 重置状态
        isHovering = false;
        transform.DOKill();

        // ★ 立即恢复：位置 + 大小 + 层级
        transform.SetSiblingIndex(baseSortOrder);
        transform.localPosition = arrangedPosition;
        transform.localScale = originalScale;

        // 触发点击事件
        OnCardClicked?.Invoke(this);
    }

    #endregion

    #region 拖拽交互

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isHovering = false;

        transform.DOKill();

        // ★ 恢复原大小
        transform.localScale = originalScale;

        // 移到拖拽层
        if (dragParent != null)
        {
            transform.SetParent(dragParent);
        }

        // 移到最上层
        transform.SetAsLastSibling();

        // 计算拖拽偏移
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        dragOffset = (Vector2)transform.localPosition - localPoint;

        // 半透明效果
        canvasGroup.DOFade(dragAlpha, 0.1f);

        // 从容器移除
        if (currentContainer != null)
        {
            currentContainer.RemoveCard(this, true);
        }

        OnDragStarted?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        transform.localPosition = localPoint + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        transform.DOKill();
        canvasGroup.DOFade(1f, 0.1f);

        // ★ 确保大小恢复
        transform.localScale = originalScale;

        // 检测放置位置
        CardContainer targetContainer = GetContainerUnderPointer(eventData);

        if (targetContainer != null)
        {
            int insertIndex = targetContainer.GetInsertIndex(transform.position);
            targetContainer.InsertCard(this, insertIndex, true);
        }
        else if (currentContainer != null)
        {
            currentContainer.AddCard(this, true);
        }

        OnDragEnded?.Invoke(this);
    }

    private CardContainer GetContainerUnderPointer(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            CardContainer container = result.gameObject.GetComponent<CardContainer>();
            if (container == null)
                container = result.gameObject.GetComponentInParent<CardContainer>();

            if (container != null)
                return container;
        }

        return null;
    }

    #endregion

    #region 动画方法

    public void PlayUseAnimation(Vector3 targetWorldPos, System.Action onComplete = null)
    {
        transform.DOKill();
        transform.SetAsLastSibling();

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.2f, 0.15f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMove(targetWorldPos, 0.4f).SetEase(Ease.InQuad));
        seq.Join(transform.DOScale(0f, 0.3f).SetDelay(0.2f));
        seq.Join(canvasGroup.DOFade(0f, 0.3f).SetDelay(0.2f));

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            Destroy(gameObject);
        });
    }

    public void Shake()
    {
        transform.DOKill();
        transform.DOShakePosition(0.3f, new Vector3(15f, 0, 0), 20, 90, false, true)
            .OnComplete(() => transform.localPosition = arrangedPosition);
    }

    /// <summary>
    /// 强制重置状态（用于异常情况）
    /// </summary>
    public void ForceReset()
    {
        transform.DOKill();
        isHovering = false;
        isDragging = false;
        transform.localScale = originalScale;
        transform.localPosition = arrangedPosition;
        transform.SetSiblingIndex(baseSortOrder);
        canvasGroup.alpha = 1f;
    }

    #endregion

    private void OnDestroy()
    {
        transform.DOKill();
        canvasGroup?.DOKill();
    }
}