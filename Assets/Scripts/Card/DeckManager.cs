using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 牌库管理器 - 管理玩家手牌
/// </summary>
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("设置")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private GameObject cardPrefab;

    [Header("手牌布局")]
    [Tooltip("手牌容器的RectTransform")]
    [SerializeField] private RectTransform handContainerRect;
    [Tooltip("手牌的Horizontal Layout Group")]
    [SerializeField] private HorizontalLayoutGroup handLayoutGroup;
    [Tooltip("卡牌宽度（用于计算间距）")]
    [SerializeField] private float cardWidth = 100f;
    [Tooltip("默认卡牌间距")]
    [SerializeField] private float defaultSpacing = 10f;
    [Tooltip("最小卡牌间距（负数表示重叠）")]
    [SerializeField] private float minSpacing = -80f;

    [Header("拖放目标区域")]
    [SerializeField] private RectTransform sellArea;
    [SerializeField] private RectTransform useArea;

    [Header("初始牌库")]
    [SerializeField] private List<CardData> initialCards = new List<CardData>();

    // 当前手牌
    private List<CardUI> handCards = new List<CardUI>();

    // 当前正在拖拽的卡牌
    private CardUI draggingCard;

    // 点数系统
    public int CurrentPoints { get; private set; }

    // 记录原始容器宽度
    private float containerWidth;

    // 事件
    public System.Action<int> OnPointsChanged;
    public System.Action<CardUI> OnCardSold;
    public System.Action<CardUI> OnCardUsed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 自动获取 RectTransform
        if (handContainerRect == null && handContainer != null)
        {
            handContainerRect = handContainer as RectTransform;
            if (handContainerRect == null)
            {
                handContainerRect = handContainer.GetComponent<RectTransform>();
            }
        }

        // 记录容器宽度
        if (handContainerRect != null)
        {
            containerWidth = handContainerRect.rect.width;
            if (containerWidth <= 0)
            {
                // 如果 rect.width 为0，尝试用 sizeDelta
                containerWidth = handContainerRect.sizeDelta.x;
            }
        }

        // 自动获取 Layout Group
        if (handLayoutGroup == null && handContainer != null)
        {
            handLayoutGroup = handContainer.GetComponent<HorizontalLayoutGroup>();
        }

        // 自动获取卡牌宽度
        if (cardWidth <= 0 && cardPrefab != null)
        {
            RectTransform cardRect = cardPrefab.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardWidth = cardRect.sizeDelta.x > 0 ? cardRect.sizeDelta.x : 100f;
            }
        }

        Debug.Log($"[DeckManager] 初始化: 容器宽度={containerWidth}, 卡牌宽度={cardWidth}");
    }

    /// <summary>
    /// 初始化牌库
    /// </summary>
    public void Initialize(int startingPoints, List<CardData> cards = null)
    {
        CurrentPoints = startingPoints;
        OnPointsChanged?.Invoke(CurrentPoints);

        // 清空现有手牌
        ClearHand();

        // 添加初始卡牌
        List<CardData> cardsToAdd = cards ?? initialCards;
        foreach (var cardData in cardsToAdd)
        {
            AddCard(cardData);
        }
    }

    /// <summary>
    /// 添加一张卡牌到手牌
    /// </summary>
    public bool AddCard(CardData cardData)
    {
        if (cardPrefab == null || handContainer == null)
        {
            Debug.LogError("CardPrefab 或 HandContainer 未设置!");
            return false;
        }

        GameObject cardObj = Instantiate(cardPrefab, handContainer);
        CardUI cardUI = cardObj.GetComponent<CardUI>();

        if (cardUI != null)
        {
            cardUI.Setup(cardData);
            handCards.Add(cardUI);

            // 更新手牌布局
            UpdateHandLayout();

            return true;
        }

        return false;
    }

    /// <summary>
    /// 从手牌移除卡牌
    /// </summary>
    public void RemoveCard(CardUI card)
    {
        if (handCards.Contains(card))
        {
            handCards.Remove(card);
            Destroy(card.gameObject);

            // 更新手牌布局
            UpdateHandLayout();
        }
    }

    /// <summary>
    /// 清空手牌
    /// </summary>
    public void ClearHand()
    {
        foreach (var card in handCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        handCards.Clear();

        // 重置布局
        if (handLayoutGroup != null)
        {
            handLayoutGroup.spacing = defaultSpacing;
        }
    }

    /// <summary>
    /// 更新手牌布局（根据卡牌数量自动调整间距）
    /// </summary>
    public void UpdateHandLayout()
    {
        if (handLayoutGroup == null) return;

        // 确保有 RectTransform
        if (handContainerRect == null && handContainer != null)
        {
            handContainerRect = handContainer as RectTransform;
        }
        if (handContainerRect == null) return;

        int cardCount = handCards.Count;
        if (cardCount <= 1)
        {
            handLayoutGroup.spacing = defaultSpacing;
            RefreshLayout();
            return;
        }

        // 获取当前可用宽度
        float currentWidth = handContainerRect.rect.width;
        if (currentWidth <= 0)
        {
            currentWidth = handContainerRect.sizeDelta.x;
        }
        if (currentWidth <= 0)
        {
            currentWidth = containerWidth > 0 ? containerWidth : 800f;
        }

        // 确保卡牌宽度有效
        float actualCardWidth = cardWidth > 0 ? cardWidth : 100f;

        // 计算padding占用的宽度
        float paddingWidth = handLayoutGroup.padding.left + handLayoutGroup.padding.right;
        float availableWidth = currentWidth - paddingWidth;

        // 计算所需的总宽度（如果使用默认间距）
        float neededWidth = cardCount * actualCardWidth + (cardCount - 1) * defaultSpacing;

        float targetSpacing;
        if (neededWidth <= availableWidth)
        {
            // 空间足够，使用默认间距
            targetSpacing = defaultSpacing;
        }
        else
        {
            // 空间不够，计算需要的间距使卡牌重叠
            // availableWidth = cardCount * cardWidth + (cardCount - 1) * spacing
            // spacing = (availableWidth - cardCount * cardWidth) / (cardCount - 1)
            targetSpacing = (availableWidth - cardCount * actualCardWidth) / (cardCount - 1);

            // 限制最小间距
            targetSpacing = Mathf.Max(targetSpacing, minSpacing);
        }

        handLayoutGroup.spacing = targetSpacing;
        RefreshLayout();

        Debug.Log($"[DeckManager] 更新手牌布局: 卡牌数={cardCount}, 可用宽度={availableWidth}, 卡牌宽度={actualCardWidth}, 间距={targetSpacing}");
    }

    /// <summary>
    /// 强制刷新布局
    /// </summary>
    private void RefreshLayout()
    {
        if (handContainerRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(handContainerRect);
        }
    }

    /// <summary>
    /// 设置手牌容器宽度（供DeckShop调用）
    /// </summary>
    public void SetContainerWidth(float width)
    {
        containerWidth = width;
        UpdateHandLayout();
    }

    /// <summary>
    /// 通知容器尺寸变化（供DeckShop调用）
    /// </summary>
    public void OnContainerSizeChanged()
    {
        UpdateHandLayout();
    }

    /// <summary>
    /// 检查是否可以添加卡牌到手牌（无上限，始终返回true）
    /// </summary>
    public bool CanAddCard()
    {
        return true;
    }

    /// <summary>
    /// 获取当前手牌数量
    /// </summary>
    public int GetHandCount()
    {
        return handCards.Count;
    }

    /// <summary>
    /// 添加卡牌到手牌（从牌库购买时调用）
    /// </summary>
    public bool AddCardToHand(CardData cardData)
    {
        return AddCard(cardData);
    }

    /// <summary>
    /// 修改点数
    /// </summary>
    public void ModifyPoints(int amount)
    {
        CurrentPoints += amount;
        CurrentPoints = Mathf.Max(0, CurrentPoints);
        OnPointsChanged?.Invoke(CurrentPoints);
    }

    /// <summary>
    /// 设置点数
    /// </summary>
    public void SetPoints(int amount)
    {
        CurrentPoints = Mathf.Max(0, amount);
        OnPointsChanged?.Invoke(CurrentPoints);
    }

    #region 拖拽处理

    /// <summary>
    /// 卡牌开始拖拽
    /// </summary>
    public void OnCardBeginDrag(CardUI card)
    {
        draggingCard = card;
    }

    /// <summary>
    /// 卡牌结束拖拽
    /// </summary>
    public bool OnCardEndDrag(CardUI card, PointerEventData eventData, CardSlot previousSlot = null)
    {
        draggingCard = null;

        CardData cardData = card.CardData;

        // 检查是否拖到了出售区域
        if (sellArea != null && RectTransformUtility.RectangleContainsScreenPoint(sellArea, eventData.position))
        {
            // 法术牌不能出售（不获得点数）
            if (cardData != null && cardData.IsSpellCard)
            {
                Debug.Log("[DeckManager] 法术牌不能出售");
                return false;
            }

            SellCard(card);
            return true;
        }

        // 检查是否拖到了使用区域
        if (useArea != null && RectTransformUtility.RectangleContainsScreenPoint(useArea, eventData.position))
        {
            // 只有法术牌可以拖入USE区域
            if (cardData != null && cardData.IsSpellCard)
            {
                UseCard(card);
                return true;
            }
            else
            {
                Debug.Log("[DeckManager] 只有法术牌可以拖入USE区域");
                return false;
            }
        }

        // 检查卡牌是否已经被放入卡槽（由CardSlot的OnDrop处理）
        if (card.CurrentSlot != null)
        {
            // 卡牌成功放入卡槽，从手牌列表移除
            if (handCards.Contains(card))
            {
                handCards.Remove(card);
            }
            return true;
        }

        // 没有放到有效位置，返回false让CardUI处理
        return false;
    }

    /// <summary>
    /// 牌库卡牌拖拽结束（购买逻辑）
    /// </summary>
    public bool OnDeckCardEndDrag(CardUI card, PointerEventData eventData)
    {
        draggingCard = null;

        if (card == null || card.CardData == null)
        {
            return false;
        }

        CardData cardData = card.CardData;

        // 检查是否拖到了手牌区域
        RectTransform handRect = handContainerRect != null ? handContainerRect : handContainer as RectTransform;
        bool isInHandArea = false;

        if (handRect != null)
        {
            // 使用Camera参数进行更准确的检测
            Canvas canvas = handRect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }
            isInHandArea = RectTransformUtility.RectangleContainsScreenPoint(handRect, eventData.position, cam);

            Debug.Log($"[DeckManager] OnDeckCardEndDrag: 手牌区域检测={isInHandArea}, 鼠标位置={eventData.position}, 手牌区域={handRect.rect}");
        }

        if (isInHandArea)
        {
            Debug.Log("[DeckManager] 检测到拖入手牌区域，尝试购买");
            // 尝试购买到手牌
            return DeckShop.Instance?.PurchaseCard(card) ?? false;
        }

        // 检查是否拖到了使用区域（只有法术牌可以）
        if (useArea != null && RectTransformUtility.RectangleContainsScreenPoint(useArea, eventData.position))
        {
            if (cardData.IsSpellCard)
            {
                // 法术牌免费使用
                DeckShop.Instance?.UseDisplayCard(card);
                SpellManager.Instance?.UseSpellCard(cardData);
                Destroy(card.gameObject);
                return true;
            }
        }

        // 检查是否拖到了卡槽
        if (card.CurrentSlot != null)
        {
            // 法术牌不能放入卡槽
            if (cardData.IsSpellCard)
            {
                card.CurrentSlot.RemoveCard();
                return false;
            }

            // 购买并直接放入卡槽
            if (DeckShop.Instance != null)
            {
                // 检查点数
                int cost = cardData.buyCost;
                if (CurrentPoints < cost)
                {
                    // 点数不足，从卡槽移除
                    card.CurrentSlot.RemoveCard();
                    return false;
                }

                // 扣除点数
                ModifyPoints(-cost);

                // 从牌库移除
                DeckShop.Instance.UseDisplayCard(card);

                Debug.Log($"[DeckManager] 从牌库购买卡牌直接放入卡槽: {card.CardData.cardName}");
                return true;
            }
        }

        Debug.Log("[DeckManager] OnDeckCardEndDrag: 没有放到有效位置");
        // 没有放到有效位置
        return false;
    }

    /// <summary>
    /// 尝试将卡牌返回手牌
    /// </summary>
    public void TryReturnCardToHand(CardUI card)
    {
        if (card == null) return;

        // 返回手牌（无上限）
        card.SetParentAndReset(handContainer);
        if (!handCards.Contains(card))
        {
            handCards.Add(card);
        }
        Debug.Log("卡牌返回手牌");
    }

    #endregion

    /// <summary>
    /// 出售卡牌
    /// </summary>
    private void SellCard(CardUI card)
    {
        // 如果卡牌在卡槽中，先移除
        if (card.CurrentSlot != null)
        {
            card.CurrentSlot.RemoveCard();
        }

        // 法术牌不能出售（不获得点数）
        if (card.CardData != null && card.CardData.IsSpellCard)
        {
            Debug.Log($"[DeckManager] 法术牌不能出售: {card.CardData.cardName}");
            return;
        }

        // 出售获得点数（使用CardData中的sellValue）
        int sellValue = card.CardData != null ? card.CardData.sellValue : 1;
        ModifyPoints(sellValue);
        Debug.Log($"出售卡牌: {card.CardData?.cardName ?? "未知"}, 获得 {sellValue} 点数");

        handCards.Remove(card);
        Destroy(card.gameObject);

        OnCardSold?.Invoke(card);
    }

    /// <summary>
    /// 使用卡牌（法术牌专用）
    /// </summary>
    private void UseCard(CardUI card)
    {
        Debug.Log($"使用法术牌: {card.CardData?.cardName ?? "未知"}");

        // 如果卡牌在卡槽中，先移除
        if (card.CurrentSlot != null)
        {
            card.CurrentSlot.RemoveCard();
        }

        // 执行法术牌效果
        if (card.CardData != null && card.CardData.IsSpellCard)
        {
            SpellManager.Instance?.UseSpellCard(card.CardData);
        }

        handCards.Remove(card);
        Destroy(card.gameObject);

        OnCardUsed?.Invoke(card);
    }

    /// <summary>
    /// 将卡牌从卡槽返回手牌
    /// </summary>
    public void ReturnCardToHand(CardUI card)
    {
        if (card.CurrentSlot != null)
        {
            card.CurrentSlot.RemoveCard();
        }

        if (!handCards.Contains(card))
        {
            card.SetParentAndReset(handContainer);
            handCards.Add(card);
            UpdateHandLayout();
        }
    }
}