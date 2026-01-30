using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 卡槽 - 接收拖拽的卡牌
/// </summary>
public class CardSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("设置")]
    [SerializeField] private int slotIndex;
    [SerializeField] private Image slotImage;
    [SerializeField] private Color normalColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color highlightColor = new Color(0.5f, 0.8f, 0.5f, 0.7f);
    [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.8f, 0.5f);

    // 当前放置的卡牌
    public CardUI CurrentCard { get; private set; }

    // 卡槽索引
    public int SlotIndex => slotIndex;

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
    /// 放置卡牌
    /// </summary>
    public bool PlaceCard(CardUI card)
    {
        if (!IsEmpty)
        {
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
            // 如果卡牌原来在其他卡槽，先移除（这会返还点数）
            if (draggedCard.CurrentSlot != null)
            {
                draggedCard.CurrentSlot.RemoveCard();
            }

            // 尝试放入卡槽
            bool success = PlaceCard(draggedCard);

            // 如果放置失败（点数不足），卡牌会在OnEndDrag中返回牌库
            if (!success)
            {
                Debug.Log("放置失败，卡牌将返回牌库");
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsEmpty && eventData.pointerDrag != null)
        {
            slotImage.color = highlightColor;
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
}