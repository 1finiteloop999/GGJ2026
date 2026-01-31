using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 卡槽类型
/// </summary>
public enum SlotType
{
    Direction,  // 方向卡槽（只能填入方向不为None的卡）
    Step,       // 步数卡槽（只能填入步数不为0的卡）
    Action      // 动作卡槽（只能填入动作卡、停顿卡和表情卡）
}

/// <summary>
/// 卡槽 - 接收拖拽的卡牌
/// </summary>
public class CardSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("卡槽设置")]
    [Tooltip("卡槽编号（执行顺序）")]
    [SerializeField] private int slotIndex;

    [Tooltip("卡槽类型")]
    [SerializeField] private SlotType slotType = SlotType.Direction;

    [Header("UI设置")]
    [SerializeField] private Image slotImage;
    [SerializeField] private Color normalColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color highlightColor = new Color(0.5f, 0.8f, 0.5f, 0.7f);
    [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.8f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(0.8f, 0.3f, 0.3f, 0.7f); // 不匹配时的颜色

    // 当前放置的卡牌
    public CardUI CurrentCard { get; private set; }

    // 卡槽索引（执行顺序）
    public int SlotIndex => slotIndex;

    // 卡槽类型
    public SlotType Type => slotType;

    // 是否为空
    public bool IsEmpty => CurrentCard == null;

    private void Start()
    {
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
        }
        UpdateVisual();
    }

    /// <summary>
    /// 设置卡槽编号
    /// </summary>
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    /// <summary>
    /// 设置卡槽类型
    /// </summary>
    public void SetSlotType(SlotType type)
    {
        slotType = type;
    }

    /// <summary>
    /// 检查卡牌是否可以放入此卡槽
    /// </summary>
    public bool CanAcceptCard(CardData cardData)
    {
        if (cardData == null) return false;

        switch (slotType)
        {
            case SlotType.Direction:
                // 方向卡槽：只接受方向不为None的卡
                return cardData.direction != DirectionType.None;

            case SlotType.Step:
                // 步数卡槽：只接受步数不为0的卡
                return cardData.stepCount > 0;

            case SlotType.Action:
                // 动作卡槽：接受动作卡、停顿卡和表情卡
                return cardData.IsActionCard || cardData.IsPauseCard || cardData.IsExpressionCard;

            default:
                return false;
        }
    }

    /// <summary>
    /// 放置卡牌
    /// </summary>
    public bool PlaceCard(CardUI card)
    {
        if (!IsEmpty)
        {
            return false;
        }

        // 检查卡牌类型是否匹配
        if (card.CardData != null && !CanAcceptCard(card.CardData))
        {
            Debug.Log($"[CardSlot] 卡牌类型不匹配！卡槽类型: {slotType}, 卡牌: {card.CardData.GetDescription()}");
            return false;
        }

        CurrentCard = card;
        card.CurrentSlot = this;
        card.SetParentAndReset(transform);

        UpdateVisual();

        // 通知SlotManager更新
        SlotManager.Instance?.OnSlotChanged();

        return true;
    }

    /// <summary>
    /// 移除卡牌（不销毁卡牌对象）
    /// </summary>
    public CardUI RemoveCard()
    {
        if (IsEmpty)
        {
            return null;
        }

        CardUI card = CurrentCard;
        CurrentCard = null;

        // 清除卡牌对卡槽的引用
        if (card != null)
        {
            card.CurrentSlot = null;
        }

        UpdateVisual();

        // 通知SlotManager更新
        SlotManager.Instance?.OnSlotChanged();

        return card;
    }

    /// <summary>
    /// 清空卡槽
    /// </summary>
    public void Clear()
    {
        if (CurrentCard != null)
        {
            Destroy(CurrentCard.gameObject);
            CurrentCard = null;
        }
        UpdateVisual();
    }

    #region 拖放事件

    public void OnDrop(PointerEventData eventData)
    {
        CardUI draggedCard = eventData.pointerDrag?.GetComponent<CardUI>();

        if (draggedCard != null && IsEmpty)
        {
            // 检查卡牌类型是否匹配
            if (draggedCard.CardData != null && !CanAcceptCard(draggedCard.CardData))
            {
                Debug.Log($"[CardSlot] 卡牌类型不匹配，无法放入！");
                return;
            }

            // 如果卡牌原来在其他卡槽，先移除
            if (draggedCard.CurrentSlot != null)
            {
                draggedCard.CurrentSlot.RemoveCard();
            }

            // 尝试放入卡槽
            bool success = PlaceCard(draggedCard);

            if (!success)
            {
                Debug.Log("放置失败，卡牌将返回");
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            CardUI draggedCard = eventData.pointerDrag.GetComponent<CardUI>();

            if (draggedCard != null && IsEmpty)
            {
                // 检查是否可以接受这张卡
                if (draggedCard.CardData != null && CanAcceptCard(draggedCard.CardData))
                {
                    slotImage.color = highlightColor; // 可以放入
                }
                else
                {
                    slotImage.color = invalidColor; // 类型不匹配
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisual();
    }

    #endregion

    /// <summary>
    /// 更新视觉效果
    /// </summary>
    private void UpdateVisual()
    {
        if (slotImage != null)
        {
            slotImage.color = IsEmpty ? normalColor : occupiedColor;
        }
    }

    /// <summary>
    /// 获取卡槽类型的显示名称
    /// </summary>
    public string GetTypeName()
    {
        return slotType switch
        {
            SlotType.Direction => "方向",
            SlotType.Step => "步数",
            SlotType.Action => "动作",
            _ => "未知"
        };
    }
}