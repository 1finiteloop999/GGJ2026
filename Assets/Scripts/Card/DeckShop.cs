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

    [Tooltip("手牌容器的原始宽度（如果自动检测失败，手动设置）")]
    [SerializeField] private float handContainerOriginalWidth = 0f;

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

    // 提示动画
    private Coroutine tipFadeCoroutine;

    // 记录原始offsetMax
    private Vector2 handContainerOriginalOffsetMax;
    private bool hasRecordedOriginalOffset = false;

    // 牌库数量变化事件
    public System.Action<int> OnDeckCountChanged;

    /// <summary>
    /// 通知牌库数量变化
    /// </summary>
    private void NotifyDeckCountChanged()
    {
        OnDeckCountChanged?.Invoke(GetRemainingCount());
    }

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

        // 在Awake中记录原始值（最早执行）
        RecordOriginalValues();
    }

    private void RecordOriginalValues()
    {
        if (hasRecordedOriginalOffset) return;

        if (handContainer != null)
        {
            // 记录原始 offsetMax
            handContainerOriginalOffsetMax = handContainer.offsetMax;

            // 使用 rect.size 获取实际渲染尺寸
            float actualWidth = handContainer.rect.width;
            float actualHeight = handContainer.rect.height;

            // 如果自动检测失败，使用Inspector中设置的值
            if (actualWidth <= 0 && handContainerOriginalWidth > 0)
            {
                actualWidth = handContainerOriginalWidth;
            }

            if (actualWidth > 0)
            {
                handContainerOriginalSizeDelta = new Vector2(actualWidth, actualHeight);
            }

            hasRecordedOriginalOffset = true;
            Debug.Log($"[DeckShop] 记录原始值: offsetMax={handContainerOriginalOffsetMax}, rect.width={actualWidth}, Inspector设置={handContainerOriginalWidth}");
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

        // 确保记录了原始值
        RecordOriginalValues();

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

        // 重置手牌容器（使用offsetMax重置）
        if (handContainer != null)
        {
            DOTween.Kill(handContainer);
            // 重置为原始offsetMax
            handContainer.offsetMax = handContainerOriginalOffsetMax;
        }

        // 通知DeckManager更新布局
        DeckManager.Instance?.OnContainerSizeChanged();

        // 通知牌库数量变化
        NotifyDeckCountChanged();

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
        if (handContainer == null)
        {
            Debug.LogError("[DeckShop] AnimateHandShrink: handContainer 为空！");
            return;
        }

        // 收缩时：在原始offsetMax基础上减少（向左收缩）
        // 展开时：恢复到原始offsetMax
        Vector2 targetOffsetMax;
        if (shrink)
        {
            targetOffsetMax = new Vector2(
                handContainerOriginalOffsetMax.x - handShrinkAmount,
                handContainerOriginalOffsetMax.y
            );
        }
        else
        {
            targetOffsetMax = handContainerOriginalOffsetMax;
        }

        Debug.Log($"[DeckShop] 手牌{(shrink ? "收缩" : "展开")}: 原始={handContainerOriginalOffsetMax}, 当前={handContainer.offsetMax}, 目标={targetOffsetMax}");

        // 使用 DOTween 动画 offsetMax
        DOTween.To(
            () => handContainer.offsetMax,
            x => handContainer.offsetMax = x,
            targetOffsetMax,
            animationDuration
        )
        .SetEase(Ease.OutCubic)
        .OnUpdate(() => {
            // 动画过程中持续更新手牌布局
            DeckManager.Instance?.OnContainerSizeChanged();
        })
        .OnComplete(() => {
            // 动画完成后最终更新
            DeckManager.Instance?.OnContainerSizeChanged();
            Debug.Log($"[DeckShop] 手牌动画完成: rect.width={handContainer.rect.width}");
        });
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

        // 通知牌库数量变化
        NotifyDeckCountChanged();
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

        // 添加到手牌（DeckManager.AddCardToHand会自动更新布局）
        DeckManager.Instance?.AddCardToHand(cardData);

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