using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 牌库管理器 - 管理玩家手牌
/// </summary>
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("设置")]
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private Transform handContainer;
    [SerializeField] private GameObject cardPrefab;

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

        // 检查是否拖到了手牌区域
        if (handContainer != null && RectTransformUtility.RectangleContainsScreenPoint(handContainer as RectTransform, eventData.position))
        {
            // 尝试购买到手牌
            return DeckShop.Instance?.PurchaseCard(card) ?? false;
        }

        // 检查是否拖到了卡槽
        if (card.CurrentSlot != null)
        {
            // 购买并直接放入卡槽
            if (DeckShop.Instance != null)
            {
                // 检查点数
                int cost = card.CardData.buyCost;
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

        // 检查是否拖到了使用区域
        if (useArea != null && RectTransformUtility.RectangleContainsScreenPoint(useArea, eventData.position))
        {
            // 直接使用牌库卡牌
            if (DeckShop.Instance != null)
            {
                DeckShop.Instance.UseDisplayCard(card);
                // TODO: 执行卡牌效果
                Debug.Log($"[DeckManager] 直接使用牌库卡牌: {card.CardData.cardName}");
                Destroy(card.gameObject);
                return true;
            }
        }

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

        if (handCards.Count < maxHandSize && !handCards.Contains(card))
        {
            card.SetParentAndReset(handContainer);
            handCards.Add(card);
        }
    }

    /// <summary>
    /// 获取最大手牌数量
    /// </summary>
    public int GetMaxHandSize()
    {
        return maxHandSize;
    }
}