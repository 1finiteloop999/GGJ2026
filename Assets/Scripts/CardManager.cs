using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 卡牌管理器 - 管理卡牌的创建和区域交互
/// </summary>
public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }
    
    [Header("预制体")]
    [SerializeField] private GameObject cardPrefab;
    
    [Header("容器引用")]
    [SerializeField] private CardContainer cardPoolContainer;     // 卡池区容器
    [SerializeField] private CardContainer playAreaContainer;     // 出牌区容器
    [SerializeField] private Transform dragLayer;                 // 拖拽层（最顶层）
    
    [Header("出牌区设置")]
    [SerializeField] private int maxPlayAreaCards = 3;            // 出牌区最大卡牌数
    
    [Header("UI引用")]
    [SerializeField] private Button useCardsButton;               // 出牌按钮
    [SerializeField] private CanvasGroup cardUICanvasGroup;       // 整体卡牌UI（用于隐藏）
    
    // 事件
    public System.Action<List<CardData>> OnCardsUsed;
    
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
        // 绑定按钮
        if (useCardsButton != null)
        {
            useCardsButton.onClick.AddListener(OnUseButtonClicked);
        }
        
        UpdateUseButtonState();
    }
    
    #region 公共方法
    
    /// <summary>
    /// 创建卡牌并添加到卡池
    /// </summary>
    public CardUI CreateCard(CardData cardData)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("CardManager: 没有卡牌预制体");
            return null;
        }
        
        // 创建卡牌
        GameObject cardObj = Instantiate(cardPrefab, cardPoolContainer.transform);
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        
        if (cardUI != null)
        {
            cardUI.Initialize(cardData);
            cardUI.SetDragParent(dragLayer);
            
            // 注册事件
            cardUI.OnCardClicked += OnCardClicked;
            cardUI.OnDragEnded += OnCardDragEnded;
            
            // 添加到卡池
            cardPoolContainer.AddCard(cardUI, false);
        }
        
        return cardUI;
    }
    
    /// <summary>
    /// 批量创建卡牌
    /// </summary>
    public void CreateCards(List<CardData> cards)
    {
        foreach (var card in cards)
        {
            CreateCard(card);
        }
    }
    
    /// <summary>
    /// 获取出牌区的卡牌数据
    /// </summary>
    public List<CardData> GetPlayAreaCardData()
    {
        List<CardData> result = new List<CardData>();
        foreach (var card in playAreaContainer.GetCards())
        {
            result.Add(card.GetCardData());
        }
        return result;
    }
    
    /// <summary>
    /// 隐藏卡牌UI
    /// </summary>
    public void HideCardUI(float duration = 0.3f)
    {
        if (cardUICanvasGroup != null)
        {
            cardUICanvasGroup.DOFade(0f, duration);
            cardUICanvasGroup.interactable = false;
            cardUICanvasGroup.blocksRaycasts = false;
        }
    }
    
    /// <summary>
    /// 显示卡牌UI
    /// </summary>
    public void ShowCardUI(float duration = 0.3f)
    {
        if (cardUICanvasGroup != null)
        {
            cardUICanvasGroup.DOFade(1f, duration);
            cardUICanvasGroup.interactable = true;
            cardUICanvasGroup.blocksRaycasts = true;
        }
    }
    
    /// <summary>
    /// 清空出牌区（卡牌返回卡池）
    /// </summary>
    public void ClearPlayArea()
    {
        List<CardUI> cardsToMove = new List<CardUI>(playAreaContainer.GetCards());
        foreach (var card in cardsToMove)
        {
            playAreaContainer.RemoveCard(card, false);
            cardPoolContainer.AddCard(card, true);
        }
        UpdateUseButtonState();
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 卡牌点击事件
    /// </summary>
    private void OnCardClicked(CardUI card)
    {
        CardContainer container = card.GetContainer();
        
        if (container == cardPoolContainer)
        {
            // 从卡池点击 -> 移到出牌区
            TryMoveToPlayArea(card);
        }
        else if (container == playAreaContainer)
        {
            // 从出牌区点击 -> 移回卡池
            MoveToCardPool(card);
        }
    }
    
    /// <summary>
    /// 尝试移动到出牌区
    /// </summary>
    private void TryMoveToPlayArea(CardUI card)
    {
        if (playAreaContainer.CardCount >= maxPlayAreaCards)
        {
            // 出牌区已满
            card.Shake();
            Debug.Log("出牌区已满");
            return;
        }
        
        cardPoolContainer.RemoveCard(card);
        playAreaContainer.AddCard(card);
        UpdateUseButtonState();
    }
    
    /// <summary>
    /// 移回卡池
    /// </summary>
    private void MoveToCardPool(CardUI card)
    {
        playAreaContainer.RemoveCard(card);
        cardPoolContainer.AddCard(card);
        UpdateUseButtonState();
    }
    
    /// <summary>
    /// 卡牌拖拽结束
    /// </summary>
    private void OnCardDragEnded(CardUI card)
    {
        UpdateUseButtonState();
    }
    
    /// <summary>
    /// 出牌按钮点击
    /// </summary>
    private void OnUseButtonClicked()
    {
        if (playAreaContainer.CardCount == 0)
        {
            Debug.Log("出牌区没有卡牌");
            return;
        }
        
        // 计算总费用
        int totalCost = 0;
        foreach (var card in playAreaContainer.GetCards())
        {
            totalCost += card.GetCardData().actionCost;
        }
        
        // 检查行动力
        if (GameManager.Instance != null && !GameManager.Instance.HasEnoughActionPoints(totalCost))
        {
            useCardsButton.transform.DOShakePosition(0.3f, new Vector3(10, 0, 0), 20, 90);
            Debug.Log("行动力不足");
            return;
        }
        
        // 获取卡牌数据
        List<CardData> usedCards = GetPlayAreaCardData();
        
        // 播放使用动画
        UseCards(() =>
        {
            OnCardsUsed?.Invoke(usedCards);
        });
    }
    
    /// <summary>
    /// 使用出牌区的卡牌
    /// </summary>
    private void UseCards(System.Action onComplete)
    {
        Vector3 targetPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        
        List<CardUI> cardsToUse = new List<CardUI>(playAreaContainer.GetCards());
        int completedCount = 0;
        
        foreach (var card in cardsToUse)
        {
            playAreaContainer.RemoveCard(card, false);
            
            card.PlayUseAnimation(targetPos, () =>
            {
                completedCount++;
                if (completedCount >= cardsToUse.Count)
                {
                    onComplete?.Invoke();
                }
            });
        }
        
        UpdateUseButtonState();
    }
    
    /// <summary>
    /// 更新出牌按钮状态
    /// </summary>
    private void UpdateUseButtonState()
    {
        if (useCardsButton != null)
        {
            useCardsButton.interactable = playAreaContainer != null && playAreaContainer.CardCount > 0;
        }
    }
    
    #endregion
}
