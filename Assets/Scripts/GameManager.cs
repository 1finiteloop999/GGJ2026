using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// 游戏管理器 - 管理游戏流程、行动力、回合等
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("行动力设置")]
    [SerializeField] private int maxActionPoints = 3;
    [SerializeField] private int currentActionPoints = 3;
    
    [Header("UI引用")]
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private CanvasGroup boardCanvasGroup;    // 棋盘的CanvasGroup
    
    [Header("棋盘设置")]
    [SerializeField] private RectTransform boardTransform;
    [SerializeField] private float boardZoomScale = 1.5f;     // 棋盘放大倍数
    
    // 玩家位置（棋盘坐标）
    private Vector2Int playerPosition = Vector2Int.zero;
    
    // 游戏状态
    public enum GameState
    {
        SelectingCards,     // 选牌阶段
        ExecutingCards,     // 执行卡牌阶段
        AnimatingMove       // 播放移动动画阶段
    }
    
    private GameState currentState = GameState.SelectingCards;
    
    // 事件
    public System.Action<Vector2Int, Vector2Int> OnPlayerMoved;  // 玩家移动事件 (from, to)
    public System.Action OnTurnEnded;
    public System.Action OnTurnStarted;
    
    private void Awake()
    {
        // 单例
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
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(EndTurn);
        }
        
        // 订阅卡牌使用事件
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnCardsUsed += OnCardsUsed;
        }
        
        // 初始化UI
        UpdateActionPointsUI();
        
        // 开始第一回合
        StartTurn();
    }
    
    #region 公共方法
    
    /// <summary>
    /// 检查是否有足够的行动力
    /// </summary>
    public bool HasEnoughActionPoints(int cost)
    {
        return currentActionPoints >= cost;
    }
    
    /// <summary>
    /// 消耗行动力
    /// </summary>
    public bool ConsumeActionPoints(int cost)
    {
        if (currentActionPoints >= cost)
        {
            currentActionPoints -= cost;
            UpdateActionPointsUI();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 获取当前行动力
    /// </summary>
    public int GetCurrentActionPoints()
    {
        return currentActionPoints;
    }
    
    /// <summary>
    /// 获取玩家位置
    /// </summary>
    public Vector2Int GetPlayerPosition()
    {
        return playerPosition;
    }
    
    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    #endregion
    
    #region 回合管理
    
    /// <summary>
    /// 开始新回合
    /// </summary>
    private void StartTurn()
    {
        currentState = GameState.SelectingCards;
        
        // 恢复行动力
        currentActionPoints = maxActionPoints;
        UpdateActionPointsUI();
        
        // 显示卡牌UI
        if (CardManager.Instance != null)
        {
            CardManager.Instance.ShowCardUI();
        }
        
        // 棋盘恢复正常大小
        ResetBoardZoom();
        
        OnTurnStarted?.Invoke();
        
        Debug.Log("回合开始");
    }
    
    /// <summary>
    /// 结束回合
    /// </summary>
    public void EndTurn()
    {
        // 清空出牌区
        if (CardManager.Instance != null)
        {
            CardManager.Instance.ClearPlayArea();
        }
        
        OnTurnEnded?.Invoke();
        
        Debug.Log("回合结束");
        
        // TODO: 这里可以加入敌人回合等逻辑
        
        // 开始新回合
        StartTurn();
    }
    
    #endregion
    
    #region 卡牌执行
    
    /// <summary>
    /// 卡牌使用后的处理
    /// </summary>
    private void OnCardsUsed(System.Collections.Generic.List<CardData> cards)
    {
        currentState = GameState.ExecutingCards;
        
        // 计算总消耗
        int totalCost = 0;
        foreach (var card in cards)
        {
            totalCost += card.actionCost;
        }
        
        // 消耗行动力
        ConsumeActionPoints(totalCost);
        
        // 隐藏卡牌UI，放大棋盘
        StartCoroutine(ExecuteCardsCoroutine(cards));
    }
    
    /// <summary>
    /// 执行卡牌效果协程
    /// </summary>
    private IEnumerator ExecuteCardsCoroutine(System.Collections.Generic.List<CardData> cards)
    {
        // 隐藏卡牌UI
        if (CardManager.Instance != null)
        {
            CardManager.Instance.HideCardUI();
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // 放大棋盘
        ZoomInBoard();
        
        yield return new WaitForSeconds(0.3f);
        
        currentState = GameState.AnimatingMove;
        
        // 计算并执行移动
        Vector2Int startPos = playerPosition;
        Vector2Int endPos = CalculateFinalPosition(cards);
        
        // 播放移动动画
        yield return StartCoroutine(PlayMoveAnimation(startPos, endPos, cards));
        
        // 更新玩家位置
        playerPosition = endPos;
        OnPlayerMoved?.Invoke(startPos, endPos);
        
        yield return new WaitForSeconds(0.5f);
        
        // 恢复棋盘大小，显示卡牌UI
        ResetBoardZoom();
        
        yield return new WaitForSeconds(0.3f);
        
        if (CardManager.Instance != null)
        {
            CardManager.Instance.ShowCardUI();
        }
        
        currentState = GameState.SelectingCards;
        
        Debug.Log($"玩家从 {startPos} 移动到 {endPos}");
    }
    
    /// <summary>
    /// 计算最终位置
    /// </summary>
    private Vector2Int CalculateFinalPosition(System.Collections.Generic.List<CardData> cards)
    {
        Vector2Int finalPos = playerPosition;
        
        foreach (var card in cards)
        {
            if (card.cardType == CardType.Move)
            {
                finalPos += card.moveDirection * card.moveDistance;
            }
        }
        
        return finalPos;
    }
    
    /// <summary>
    /// 播放移动动画
    /// </summary>
    private IEnumerator PlayMoveAnimation(Vector2Int from, Vector2Int to, System.Collections.Generic.List<CardData> cards)
    {
        // TODO: 这里由 BoardManager 处理具体的移动动画
        // 暂时只是等待一段时间
        
        Debug.Log($"播放移动动画: {from} -> {to}");
        
        // 通知 BoardManager 播放动画（如果存在）
        if (BoardManager.Instance != null)
        {
            yield return StartCoroutine(BoardManager.Instance.PlayMoveAnimation(from, to));
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }
    }
    
    #endregion
    
    #region 棋盘缩放
    
    /// <summary>
    /// 放大棋盘
    /// </summary>
    private void ZoomInBoard()
    {
        if (boardTransform != null)
        {
            boardTransform.DOScale(boardZoomScale, 0.4f).SetEase(Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// 恢复棋盘大小
    /// </summary>
    private void ResetBoardZoom()
    {
        if (boardTransform != null)
        {
            boardTransform.DOScale(1f, 0.4f).SetEase(Ease.OutQuad);
        }
    }
    
    #endregion
    
    #region UI更新
    
    /// <summary>
    /// 更新行动力UI
    /// </summary>
    private void UpdateActionPointsUI()
    {
        if (actionPointsText != null)
        {
            actionPointsText.text = $"行动力: {currentActionPoints}/{maxActionPoints}";
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // 取消订阅
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnCardsUsed -= OnCardsUsed;
        }
    }
}
