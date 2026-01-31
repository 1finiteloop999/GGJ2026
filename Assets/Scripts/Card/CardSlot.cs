using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
/// 卡槽 - 接收拖拽的卡牌，支持叠加相同卡牌
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
    [SerializeField] private Color invalidColor = new Color(0.8f, 0.3f, 0.3f, 0.7f);
    [SerializeField] private Color stackHighlightColor = new Color(0.8f, 0.8f, 0.3f, 0.7f); // 叠加高亮色

    [Header("叠加设置")]
    [Tooltip("叠加卡牌的Y轴偏移")]
    [SerializeField] private float stackOffsetY = -30f;

    // 卡牌堆栈（第一张是底牌，后面是叠加的牌）
    private List<CardUI> cardStack = new List<CardUI>();

    // 当前放置的主卡牌（底牌）
    public CardUI CurrentCard => cardStack.Count > 0 ? cardStack[0] : null;

    // 所有卡牌（包括叠加的）
    public List<CardUI> AllCards => cardStack;

    // 叠加的卡牌数量（不包括底牌）
    public int StackCount => Mathf.Max(0, cardStack.Count - 1);

    // 卡槽索引（执行顺序）
    public int SlotIndex => slotIndex;

    // 卡槽类型
    public SlotType Type => slotType;

    // 是否为空
    public bool IsEmpty => cardStack.Count == 0;

    // 是否有叠加卡牌
    public bool HasStack => cardStack.Count > 1;

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
    /// 检查卡牌是否可以放入此卡槽（类型检查）
    /// </summary>
    public bool CanAcceptCard(CardData cardData)
    {
        if (cardData == null) return false;

        // 法术牌不能放入卡槽
        if (cardData.IsSpellCard) return false;

        switch (slotType)
        {
            case SlotType.Direction:
                return cardData.direction != DirectionType.None;

            case SlotType.Step:
                return cardData.stepCount > 0;

            case SlotType.Action:
                return cardData.IsActionCard || cardData.IsPauseCard || cardData.IsExpressionCard;

            default:
                return false;
        }
    }

    /// <summary>
    /// 检查卡牌是否可以叠加到当前卡槽
    /// </summary>
    public bool CanStackCard(CardData cardData)
    {
        if (cardData == null) return false;
        if (IsEmpty) return false; // 空卡槽不能叠加

        // 检查是否是相同的卡牌（使用CompareKey比较）
        CardData baseCard = CurrentCard.CardData;
        if (baseCard == null) return false;

        return baseCard.GetCompareKey() == cardData.GetCompareKey();
    }

    /// <summary>
    /// 检查特定卡牌UI是否可以叠加（防止自己叠自己）
    /// </summary>
    public bool CanStackCardUI(CardUI card)
    {
        if (card == null || card.CardData == null) return false;
        if (IsEmpty) return false;

        // 检查这张卡是否已经在这个卡槽的堆栈中
        if (cardStack.Contains(card)) return false;

        // 检查是否是相同的卡牌
        return CanStackCard(card.CardData);
    }

    /// <summary>
    /// 放置卡牌（第一张底牌）
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

        // 添加到堆栈
        cardStack.Add(card);
        card.CurrentSlot = this;
        card.StackIndex = 0;

        // 设置父级
        card.transform.SetParent(transform);
        card.transform.localScale = Vector3.one;

        // 设置位置（居中）
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            // 重置锚点和位置确保居中
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
        }

        // 底牌在最上层显示
        card.transform.SetAsLastSibling();

        UpdateVisual();
        SlotManager.Instance?.OnSlotChanged();

        Debug.Log($"[CardSlot] 放置底牌: {card.CardData?.GetDescription()}");
        return true;
    }

    /// <summary>
    /// 叠加卡牌（必须是相同的卡牌）
    /// </summary>
    public bool StackCard(CardUI card)
    {
        if (IsEmpty)
        {
            // 空卡槽，直接放置
            return PlaceCard(card);
        }

        // 检查是否可以叠加（包括防止自己叠自己）
        if (!CanStackCardUI(card))
        {
            Debug.Log($"[CardSlot] 无法叠加此卡牌！");
            return false;
        }

        // 添加到堆栈
        int stackIndex = cardStack.Count;
        cardStack.Add(card);
        card.CurrentSlot = this;
        card.StackIndex = stackIndex;

        // 设置父级
        card.transform.SetParent(transform);
        card.transform.localScale = Vector3.one;

        // 设置锚点
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
        }

        // 重新排列所有卡牌位置
        RefreshCardPositions();

        UpdateVisual();
        SlotManager.Instance?.OnSlotChanged();

        Debug.Log($"[CardSlot] 叠加卡牌: {card.CardData?.GetDescription()}, 当前堆栈数={cardStack.Count}");
        return true;
    }

    /// <summary>
    /// 刷新所有卡牌的位置（叠加效果）
    /// 底牌（索引0）完整显示在最上面（视觉位置）
    /// 后来的牌向下偏移，露出下半部分
    /// 只能拖拽最后放入的牌（索引最大的）
    /// </summary>
    private void RefreshCardPositions()
    {
        for (int i = 0; i < cardStack.Count; i++)
        {
            CardUI card = cardStack[i];
            if (card == null) continue;

            card.StackIndex = i;

            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                // 底牌（索引0）在位置0
                // 后来的牌（索引1, 2, 3...）向下偏移
                cardRect.anchoredPosition = new Vector2(0, i * stackOffsetY);
            }

            // 后来的牌（索引大）在更上层渲染，这样它们会覆盖在底牌上面
            // SetSiblingIndex: 越大越在上面
            card.transform.SetSiblingIndex(i);

            // 只有最后一张牌（索引最大）可以接收点击
            // 其他牌禁用射线检测
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                bool isTopCard = (i == cardStack.Count - 1);
                cg.blocksRaycasts = isTopCard;
            }
        }
    }

    /// <summary>
    /// 移除卡牌（从顶部移除，即最后叠加的牌）
    /// </summary>
    public CardUI RemoveCard()
    {
        if (IsEmpty)
        {
            return null;
        }

        // 从堆栈顶部移除（最后叠加的牌）
        int lastIndex = cardStack.Count - 1;
        CardUI card = cardStack[lastIndex];
        cardStack.RemoveAt(lastIndex);

        // 清除卡牌对卡槽的引用
        if (card != null)
        {
            card.CurrentSlot = null;
            card.StackIndex = -1;

            // 恢复射线检测（因为它可能被禁用了）
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = true;
            }
        }

        // 刷新剩余卡牌的位置（这会更新新顶部牌的交互状态）
        RefreshCardPositions();

        UpdateVisual();
        SlotManager.Instance?.OnSlotChanged();

        Debug.Log($"[CardSlot] 移除卡牌: {card?.CardData?.GetDescription()}, 剩余堆栈数={cardStack.Count}");
        return card;
    }

    /// <summary>
    /// 获取最顶部的卡牌（最后叠加的）
    /// </summary>
    public CardUI GetTopCard()
    {
        if (IsEmpty) return null;
        return cardStack[cardStack.Count - 1];
    }

    /// <summary>
    /// 清空卡槽（包括所有叠加的牌）
    /// </summary>
    public void Clear()
    {
        foreach (var card in cardStack)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        cardStack.Clear();
        UpdateVisual();
    }

    /// <summary>
    /// 计算此卡槽的得分
    /// </summary>
    /// <param name="correctCardKey">正确答案卡牌的CompareKey</param>
    /// <returns>得分</returns>
    public int CalculateScore(string correctCardKey)
    {
        if (IsEmpty) return 0;

        CardData baseCard = CurrentCard.CardData;
        if (baseCard == null) return 0;

        // 检查底牌是否正确
        bool isCorrect = baseCard.GetCompareKey() == correctCardKey;

        if (!isCorrect)
        {
            // 底牌错误，整个卡槽0分
            return 0;
        }

        // 底牌正确：底牌价值 + 叠加牌数量（每张+1分）
        int score = baseCard.valuePoints + StackCount;

        Debug.Log($"[CardSlot] 计算得分: 底牌={baseCard.GetDescription()}, 价值={baseCard.valuePoints}, 叠加数={StackCount}, 总分={score}");
        return score;
    }

    #region 拖放事件

    public void OnDrop(PointerEventData eventData)
    {
        CardUI draggedCard = eventData.pointerDrag?.GetComponent<CardUI>();

        if (draggedCard == null || draggedCard.CardData == null) return;

        // 检查卡牌类型是否匹配
        if (!CanAcceptCard(draggedCard.CardData))
        {
            Debug.Log($"[CardSlot] 卡牌类型不匹配，无法放入！");
            return;
        }

        // 如果卡牌原来在其他卡槽，先移除（但不是本卡槽）
        if (draggedCard.CurrentSlot != null && draggedCard.CurrentSlot != this)
        {
            draggedCard.CurrentSlot.RemoveCard();
        }

        bool success;

        if (IsEmpty)
        {
            // 空卡槽，放置底牌
            success = PlaceCard(draggedCard);
        }
        else if (CanStackCardUI(draggedCard))
        {
            // 可以叠加
            success = StackCard(draggedCard);
        }
        else
        {
            // 不能叠加（卡牌不相同）
            Debug.Log($"[CardSlot] 卡槽已有不同卡牌，无法放入！");
            success = false;
        }

        if (!success)
        {
            Debug.Log("放置失败，卡牌将返回");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            CardUI draggedCard = eventData.pointerDrag.GetComponent<CardUI>();

            if (draggedCard != null && draggedCard.CardData != null)
            {
                // 检查是否可以接受这张卡
                if (!CanAcceptCard(draggedCard.CardData))
                {
                    slotImage.color = invalidColor; // 类型不匹配
                }
                else if (IsEmpty)
                {
                    slotImage.color = highlightColor; // 可以放入
                }
                else if (CanStackCardUI(draggedCard))
                {
                    slotImage.color = stackHighlightColor; // 可以叠加
                }
                else
                {
                    slotImage.color = invalidColor; // 卡牌不相同或已在此卡槽，不能放入
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