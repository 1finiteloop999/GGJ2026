using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 牌库管理器 - 管理牌库的展开/收起和卡牌抽取
/// </summary>
public class DeckShop : MonoBehaviour
{
    public static DeckShop Instance { get; private set; }

    [Header("UI引用")]
    [Tooltip("牌库展开/收起按钮")]
    [SerializeField] private Button deckToggleButton;

    [Tooltip("牌库展开面板（RectTransform）")]
    [SerializeField] private RectTransform deckPanel;

    [Tooltip("卡牌显示容器（需要Horizontal Layout Group）")]
    [SerializeField] private Transform cardDisplayContainer;

    [Tooltip("提示文本")]
    [SerializeField] private TextMeshProUGUI tipText;

    [Tooltip("卡牌预制体")]
    [SerializeField] private GameObject cardPrefab;

    [Header("手牌区域引用")]
    [Tooltip("手牌容器的RectTransform")]
    [SerializeField] private RectTransform handContainer;

    [Tooltip("手牌的Horizontal Layout Group")]
    [SerializeField] private HorizontalLayoutGroup handLayoutGroup;

    [Header("动画设置")]
    [Tooltip("动画持续时间")]
    [SerializeField] private float animationDuration = 0.3f;

    [Tooltip("手牌收缩的距离")]
    [SerializeField] private float handShrinkAmount = 300f;

    [Header("其他设置")]
    [Tooltip("每次显示的卡牌数量")]
    [SerializeField] private int displayCount = 3;

    [Tooltip("提示消失时间")]
    [SerializeField] private float tipFadeDuration = 1f;

    // 牌库状态
    private List<CardData> availableDeck = new List<CardData>();
    private List<CardData> currentDisplayCards = new List<CardData>();
    private List<CardUI> displayCardUIs = new List<CardUI>();
    private bool isExpanded = false;
    private bool isAnimating = false;

    // 动画相关
    private float deckPanelOriginalWidth;
    private Vector2 handContainerOriginalSizeDelta;
    private float handLayoutOriginalSpacing;

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

        // 记录原始尺寸
        if (deckPanel != null)
        {
            deckPanelOriginalWidth = deckPanel.rect.width;
            // 确保初始状态是隐藏的
            deckPanel.gameObject.SetActive(false);
        }

        if (handContainer != null)
        {
            handContainerOriginalSizeDelta = handContainer.sizeDelta;
        }

        // 记录手牌布局原始值
        if (handLayoutGroup != null)
        {
            handLayoutOriginalSpacing = handLayoutGroup.spacing;
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
        isAnimating = false;

        // 重置牌库面板
        if (deckPanel != null)
        {
            DOTween.Kill(deckPanel);
            deckPanel.gameObject.SetActive(false);
        }

        // 重置手牌容器
        if (handContainer != null)
        {
            DOTween.Kill(handContainer);
            handContainer.sizeDelta = handContainerOriginalSizeDelta;
        }

        // 重置手牌布局
        if (handLayoutGroup != null)
        {
            handLayoutGroup.spacing = handLayoutOriginalSpacing;
        }

        Debug.Log($"[DeckShop] 初始化牌库，共 {availableDeck.Count} 张卡牌");
    }

    /// <summary>
    /// 切换牌库展开/收起
    /// </summary>
    public void OnToggleDeck()
    {
        if (isAnimating) return;

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
        isAnimating = true;

        // 补充展示卡牌
        RefillDisplayCards();

        // 显示面板并播放动画
        if (deckPanel != null)
        {
            // 设置初始状态：缩放X为0，从右边开始展开
            deckPanel.localScale = new Vector3(0, 1, 1);
            deckPanel.gameObject.SetActive(true);

            // 动画：X缩放从0到1
            deckPanel.DOScaleX(1, animationDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    // 动画完成后创建卡牌UI
                    UpdateDisplayUI();
                    isAnimating = false;
                });
        }
        else
        {
            UpdateDisplayUI();
            isAnimating = false;
        }

        // 手牌收缩动画
        AnimateHandShrink(true);

        Debug.Log($"[DeckShop] 展开牌库，显示 {currentDisplayCards.Count} 张卡牌");
    }

    /// <summary>
    /// 收起牌库
    /// </summary>
    private void CollapseDeck()
    {
        isExpanded = false;
        isAnimating = true;

        // 先清除卡牌UI
        ClearDisplayCards();

        // 动画：X缩放从1到0
        if (deckPanel != null)
        {
            deckPanel.DOScaleX(0, animationDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    deckPanel.gameObject.SetActive(false);
                    isAnimating = false;
                });
        }
        else
        {
            isAnimating = false;
        }

        // 手牌展开动画
        AnimateHandShrink(false);

        Debug.Log("[DeckShop] 收起牌库");
    }

    /// <summary>
    /// 手牌收缩/展开动画
    /// </summary>
    private void AnimateHandShrink(bool shrink)
    {
        if (handContainer != null)
        {
            Vector2 targetSize = shrink
                ? new Vector2(handContainerOriginalSizeDelta.x - handShrinkAmount, handContainerOriginalSizeDelta.y)
                : handContainerOriginalSizeDelta;

            handContainer.DOSizeDelta(targetSize, animationDuration).SetEase(Ease.OutCubic);
        }

        // 调整手牌间距让卡牌重叠
        if (handLayoutGroup != null)
        {
            // 计算目标间距：收缩时减少间距（负数让卡牌重叠）
            float targetSpacing = shrink
                ? CalculateShrunkSpacing()
                : handLayoutOriginalSpacing;

            // 使用DOTween动画调整间距
            float currentSpacing = handLayoutGroup.spacing;
            DOTween.To(
                () => currentSpacing,
                x => {
                    currentSpacing = x;
                    handLayoutGroup.spacing = x;
                    // 强制刷新布局
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(handContainer);
                },
                targetSpacing,
                animationDuration
            ).SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// 计算收缩时的手牌间距
    /// </summary>
    private float CalculateShrunkSpacing()
    {
        if (handContainer == null || handLayoutGroup == null)
        {
            return handLayoutOriginalSpacing;
        }

        // 获取手牌数量
        int cardCount = DeckManager.Instance?.GetHandCount() ?? 0;
        if (cardCount <= 1)
        {
            return handLayoutOriginalSpacing;
        }

        // 获取单张卡牌宽度
        float cardWidth = GetCardWidth();

        // 计算收缩后可用宽度
        float shrunkWidth = handContainerOriginalSizeDelta.x - handShrinkAmount;
        float paddingWidth = handLayoutGroup.padding.left + handLayoutGroup.padding.right;
        float availableWidth = shrunkWidth - paddingWidth;

        // 计算需要的间距
        // 公式：availableWidth = cardWidth + (cardCount - 1) * (cardWidth + spacing)
        // 解出 spacing = (availableWidth - cardWidth * cardCount) / (cardCount - 1)
        float neededSpacing = (availableWidth - cardWidth * cardCount) / (cardCount - 1);

        // 限制最小间距（最多重叠卡牌宽度的80%）
        float minSpacing = -cardWidth * 0.8f;

        Debug.Log($"[DeckShop] 手牌收缩计算: 卡牌数={cardCount}, 卡牌宽度={cardWidth}, 可用宽度={availableWidth}, 计算间距={neededSpacing}");

        return Mathf.Max(neededSpacing, minSpacing);
    }

    /// <summary>
    /// 获取卡牌宽度
    /// </summary>
    private float GetCardWidth()
    {
        // 尝试从预制体获取
        if (cardPrefab != null)
        {
            RectTransform cardRect = cardPrefab.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                return cardRect.rect.width > 0 ? cardRect.rect.width : cardRect.sizeDelta.x;
            }
        }

        // 尝试从现有手牌获取
        if (handContainer != null && handContainer.childCount > 0)
        {
            RectTransform firstCard = handContainer.GetChild(0) as RectTransform;
            if (firstCard != null)
            {
                return firstCard.rect.width > 0 ? firstCard.rect.width : firstCard.sizeDelta.x;
            }
        }

        // 默认值
        return 120f;
    }

    /// <summary>
    /// 补充展示卡牌到指定数量
    /// </summary>
    private void RefillDisplayCards()
    {
        int needCount = displayCount - currentDisplayCards.Count;

        if (needCount <= 0 || availableDeck.Count == 0)
        {
            return;
        }

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
        ClearDisplayCards();

        if (cardPrefab == null || cardDisplayContainer == null)
        {
            Debug.LogError("[DeckShop] 缺少卡牌预制体或显示容器！");
            return;
        }

        foreach (var cardData in currentDisplayCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardDisplayContainer);
            CardUI cardUI = cardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.Setup(cardData);
                displayCardUIs.Add(cardUI);
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
        int cost = 0;

        // 法术牌不消耗点数
        if (!cardData.IsSpellCard)
        {
            // 检查点数是否足够
            cost = cardData.buyCost;
            if (DeckManager.Instance != null && DeckManager.Instance.CurrentPoints < cost)
            {
                ShowTip("Not enough points!");
                return false;
            }

            // 扣除点数
            DeckManager.Instance?.ModifyPoints(-cost);
        }

        // 从展示列表中移除
        currentDisplayCards.Remove(cardData);
        displayCardUIs.Remove(cardUI);

        // 销毁展示区的卡牌UI
        Destroy(cardUI.gameObject);

        // 添加到手牌
        DeckManager.Instance?.AddCardToHand(cardData);

        // 更新手牌间距
        UpdateHandSpacing();

        if (cardData.IsSpellCard)
        {
            Debug.Log($"[DeckShop] 获得法术牌: {cardData.cardName}（免费）");
        }
        else
        {
            Debug.Log($"[DeckShop] 购买卡牌: {cardData.cardName}，花费 {cost} 点数");
        }

        // 如果展示区空了且牌库也空了，显示提示并收起
        if (currentDisplayCards.Count == 0 && availableDeck.Count == 0)
        {
            ShowTip("The deck is empty!");
            CollapseDeck();
        }

        return true;
    }

    /// <summary>
    /// 更新手牌间距（购买新卡牌后调用）
    /// </summary>
    private void UpdateHandSpacing()
    {
        if (!isExpanded || handLayoutGroup == null) return;

        float targetSpacing = CalculateShrunkSpacing();
        handLayoutGroup.spacing = targetSpacing;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(handContainer);
    }

    /// <summary>
    /// 使用展示区的卡牌
    /// </summary>
    public void UseDisplayCard(CardUI cardUI)
    {
        if (cardUI == null || cardUI.CardData == null)
        {
            return;
        }

        CardData cardData = cardUI.CardData;

        currentDisplayCards.Remove(cardData);
        displayCardUIs.Remove(cardUI);

        Debug.Log($"[DeckShop] 使用展示区卡牌: {cardData.cardName}");

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

        if (tipFadeCoroutine != null)
        {
            StopCoroutine(tipFadeCoroutine);
        }

        tipText.text = message;
        tipText.gameObject.SetActive(true);
        tipText.alpha = 1f;

        tipFadeCoroutine = StartCoroutine(FadeTipCoroutine());
    }

    /// <summary>
    /// 提示渐隐协程
    /// </summary>
    private IEnumerator FadeTipCoroutine()
    {
        yield return new WaitForSeconds(1f);

        float elapsed = 0f;
        while (elapsed < tipFadeDuration)
        {
            elapsed += Time.deltaTime;
            tipText.alpha = 1f - (elapsed / tipFadeDuration);
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