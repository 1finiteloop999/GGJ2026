using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 牌库管理器 - 管理牌库的展开/收起和卡牌抽取
/// </summary>
public class DeckShop : MonoBehaviour
{
    public static DeckShop Instance { get; private set; }

    [Header("UI引用")]
    [Tooltip("牌库展开/收起按钮")]
    [SerializeField] private Button deckToggleButton;

    [Tooltip("牌库展开面板")]
    [SerializeField] private GameObject deckPanel;

    [Tooltip("卡牌显示容器（需要Horizontal Layout Group）")]
    [SerializeField] private Transform cardDisplayContainer;

    [Tooltip("提示文本")]
    [SerializeField] private TextMeshProUGUI tipText;

    [Tooltip("卡牌预制体")]
    [SerializeField] private GameObject cardPrefab;

    [Header("设置")]
    [Tooltip("每次显示的卡牌数量")]
    [SerializeField] private int displayCount = 3;

    [Tooltip("提示消失时间")]
    [SerializeField] private float tipFadeDuration = 1f;

    // 牌库状态
    private List<CardData> availableDeck = new List<CardData>();  // 可用的牌库（未被抽走的）
    private List<CardData> currentDisplayCards = new List<CardData>();  // 当前展示的卡牌数据
    private List<CardUI> displayCardUIs = new List<CardUI>();  // 当前展示的卡牌UI
    private bool isExpanded = false;

    // 提示动画
    private Coroutine tipFadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 绑定按钮事件
        if (deckToggleButton != null)
        {
            deckToggleButton.onClick.AddListener(OnToggleDeck);
        }

        // 初始状态为收起
        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }

        // 隐藏提示
        if (tipText != null)
        {
            tipText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 初始化牌库（关卡开始时调用）
    /// </summary>
    public void Initialize(List<CardData> deckCards, int displayNum = 3)
    {
        // 清空之前的状态
        availableDeck.Clear();
        currentDisplayCards.Clear();
        ClearDisplayCards();

        // 复制牌库
        if (deckCards != null)
        {
            availableDeck.AddRange(deckCards);
        }

        displayCount = displayNum;
        isExpanded = false;

        // 收起牌库
        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }

        Debug.Log($"[DeckShop] 初始化牌库，共 {availableDeck.Count} 张卡牌");
    }

    /// <summary>
    /// 切换牌库展开/收起
    /// </summary>
    public void OnToggleDeck()
    {
        if (isExpanded)
        {
            CollapseDeck();
        }
        else
        {
            ExpandDeck();
        }
    }

    /// <summary>
    /// 展开牌库
    /// </summary>
    private void ExpandDeck()
    {
        // 检查牌库是否为空
        if (availableDeck.Count == 0 && currentDisplayCards.Count == 0)
        {
            ShowTip("The deck is empty!");
            return;
        }

        isExpanded = true;

        if (deckPanel != null)
        {
            deckPanel.SetActive(true);
        }

        // 补充展示卡牌
        RefillDisplayCards();

        // 创建卡牌UI
        UpdateDisplayUI();

        Debug.Log($"[DeckShop] 展开牌库，显示 {currentDisplayCards.Count} 张卡牌");
    }

    /// <summary>
    /// 收起牌库
    /// </summary>
    private void CollapseDeck()
    {
        isExpanded = false;

        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }

        Debug.Log("[DeckShop] 收起牌库");
    }

    /// <summary>
    /// 补充展示卡牌到指定数量
    /// </summary>
    private void RefillDisplayCards()
    {
        // 计算需要补充的数量
        int needCount = displayCount - currentDisplayCards.Count;

        if (needCount <= 0 || availableDeck.Count == 0)
        {
            return;
        }

        // 从牌库中随机抽取
        for (int i = 0; i < needCount && availableDeck.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableDeck.Count);
            CardData card = availableDeck[randomIndex];

            currentDisplayCards.Add(card);
            availableDeck.RemoveAt(randomIndex);

            Debug.Log($"[DeckShop] 从牌库抽取: {card.cardName}，剩余 {availableDeck.Count} 张");
        }
    }

    /// <summary>
    /// 更新展示UI
    /// </summary>
    private void UpdateDisplayUI()
    {
        // 清空现有UI
        ClearDisplayCards();

        if (cardPrefab == null || cardDisplayContainer == null)
        {
            Debug.LogError("[DeckShop] 缺少卡牌预制体或显示容器！");
            return;
        }

        // 为每张展示卡牌创建UI
        foreach (var cardData in currentDisplayCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardDisplayContainer);
            CardUI cardUI = cardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.Setup(cardData);
                displayCardUIs.Add(cardUI);

                // 添加购买事件
                // 注意：这里需要特殊处理，让卡牌可以被拖拽购买
            }
        }
    }

    /// <summary>
    /// 清空展示卡牌UI
    /// </summary>
    private void ClearDisplayCards()
    {
        foreach (var cardUI in displayCardUIs)
        {
            if (cardUI != null)
            {
                Destroy(cardUI.gameObject);
            }
        }
        displayCardUIs.Clear();
    }

    /// <summary>
    /// 购买卡牌（从展示区移到手牌）
    /// </summary>
    public bool PurchaseCard(CardUI cardUI)
    {
        if (cardUI == null || cardUI.CardData == null)
        {
            return false;
        }

        CardData cardData = cardUI.CardData;

        // 检查点数是否足够
        int cost = cardData.buyCost;
        if (DeckManager.Instance != null && DeckManager.Instance.CurrentPoints < cost)
        {
            ShowTip("Not enough points!");
            return false;
        }

        // 检查手牌是否已满
        if (DeckManager.Instance != null && !DeckManager.Instance.CanAddCard())
        {
            ShowTip("Hand is full!");
            return false;
        }

        // 扣除点数
        DeckManager.Instance?.ModifyPoints(-cost);

        // 从展示列表中移除
        currentDisplayCards.Remove(cardData);
        displayCardUIs.Remove(cardUI);

        // 销毁展示区的卡牌UI
        Destroy(cardUI.gameObject);

        // 添加到手牌
        DeckManager.Instance?.AddCardToHand(cardData);

        Debug.Log($"[DeckShop] 购买卡牌: {cardData.cardName}，花费 {cost} 点数");

        // 如果展示区空了且牌库也空了，显示提示
        if (currentDisplayCards.Count == 0 && availableDeck.Count == 0)
        {
            ShowTip("The deck is empty!");
            CollapseDeck();
        }

        return true;
    }

    /// <summary>
    /// 使用展示区的卡牌（直接使用，不加入手牌）
    /// </summary>
    public void UseDisplayCard(CardUI cardUI)
    {
        if (cardUI == null || cardUI.CardData == null)
        {
            return;
        }

        CardData cardData = cardUI.CardData;

        // 从展示列表中移除
        currentDisplayCards.Remove(cardData);
        displayCardUIs.Remove(cardUI);

        // 销毁卡牌UI
        Destroy(cardUI.gameObject);

        Debug.Log($"[DeckShop] 使用展示区卡牌: {cardData.cardName}");

        // 检查是否需要提示牌库空了
        if (currentDisplayCards.Count == 0 && availableDeck.Count == 0)
        {
            ShowTip("The deck is empty!");
        }
    }

    /// <summary>
    /// 显示提示文本
    /// </summary>
    private void ShowTip(string message)
    {
        if (tipText == null) return;

        // 停止之前的动画
        if (tipFadeCoroutine != null)
        {
            StopCoroutine(tipFadeCoroutine);
        }

        tipText.text = message;
        tipText.gameObject.SetActive(true);
        tipText.alpha = 1f;

        // 开始渐隐动画
        tipFadeCoroutine = StartCoroutine(FadeTipCoroutine());
    }

    /// <summary>
    /// 提示渐隐协程
    /// </summary>
    private IEnumerator FadeTipCoroutine()
    {
        // 显示1秒
        yield return new WaitForSeconds(1f);

        // 渐隐
        float elapsed = 0f;
        while (elapsed < tipFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / tipFadeDuration);
            tipText.alpha = alpha;
            yield return null;
        }

        tipText.gameObject.SetActive(false);
        tipText.alpha = 1f;
        tipFadeCoroutine = null;
    }

    /// <summary>
    /// 获取牌库剩余数量
    /// </summary>
    public int GetRemainingCount()
    {
        return availableDeck.Count + currentDisplayCards.Count;
    }

    /// <summary>
    /// 检查牌库是否为空
    /// </summary>
    public bool IsDeckEmpty()
    {
        return availableDeck.Count == 0 && currentDisplayCards.Count == 0;
    }

    /// <summary>
    /// 检查卡牌是否来自牌库展示区
    /// </summary>
    public bool IsFromDeckDisplay(CardUI cardUI)
    {
        return displayCardUIs.Contains(cardUI);
    }
}