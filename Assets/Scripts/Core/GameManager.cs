using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 游戏阶段
/// </summary>
public enum GamePhase
{
    Watching,   // 观看NPC路径
    Planning,   // 规划阶段（放置卡牌）
    Executing,  // 执行阶段（播放玩家移动）
    Result      // 结算阶段
}

/// <summary>
/// 游戏管理器 - 控制游戏流程
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("关卡数据")]
    [SerializeField] private LevelData currentLevel;

    [Header("棋盘引用")]
    [SerializeField] private Transform boardContainer; // 包含棋盘、NPC、玩家的父物体

    [Header("棋盘位置设置")]
    [Tooltip("观看/执行阶段的棋盘位置（居中）")]
    [SerializeField] private Vector3 boardCenterPosition = Vector3.zero;
    [Tooltip("规划阶段的棋盘位置（左移）")]
    [SerializeField] private Vector3 boardPlanningPosition = new Vector3(-3f, 0f, 0f);
    [Tooltip("规划阶段的棋盘缩放")]
    [SerializeField] private float boardPlanningScale = 0.6f;
    [Tooltip("棋盘移动/缩放动画时间")]
    [SerializeField] private float boardTransitionDuration = 0.5f;

    [Header("棋子引用")]
    [SerializeField] private PawnController npcPawn;
    [SerializeField] private PawnController playerPawn;

    [Header("棋子颜色")]
    [SerializeField] private Color npcColor = Color.red;
    [SerializeField] private Color playerColor = Color.blue;

    [Header("UI引用")]
    [SerializeField] private GameObject watchingUI;
    [SerializeField] private GameObject planningUI;
    [SerializeField] private GameObject resultUI;

    [Header("设置")]
    [SerializeField] private float npcAnimationDelay = 0.5f;

    // 当前游戏阶段
    public GamePhase CurrentPhase { get; private set; }

    // 玩家起始位置
    public Vector2Int PlayerStartPosition => currentLevel?.playerStartPosition ?? Vector2Int.zero;

    // 最近一次的分数结果
    public CompareResult LastScoreResult { get; private set; }

    /// <summary>
    /// 获取当前关卡数据
    /// </summary>
    public LevelData GetCurrentLevel()
    {
        return currentLevel;
    }

    // 事件
    public System.Action<GamePhase> OnPhaseChanged;

    // 棋盘动画协程引用
    private Coroutine boardTransitionCoroutine;

    // 棋盘动画完成回调
    private System.Action onBoardTransitionComplete;

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
        // 如果没有设置boardContainer，尝试查找GridBoard
        if (boardContainer == null)
        {
            GridBoard gridBoard = Object.FindFirstObjectByType<GridBoard>();
            if (gridBoard != null)
            {
                boardContainer = gridBoard.transform;
            }
        }

        // 记录初始位置作为居中位置
        if (boardContainer != null && boardCenterPosition == Vector3.zero)
        {
            boardCenterPosition = boardContainer.position;
        }

        // 如果有关卡数据，直接开始游戏
        if (currentLevel != null)
        {
            StartLevel(currentLevel);
        }
    }

    /// <summary>
    /// 开始关卡
    /// </summary>
    public void StartLevel(LevelData level)
    {
        currentLevel = level;

        // 初始化棋子
        InitializePawns();

        // 初始化手牌
        DeckManager.Instance?.Initialize(level.startingPoints, level.initialCards);

        // 初始化牌库商店
        DeckShop.Instance?.Initialize(level.deckCards, level.deckDisplayCount);

        // 初始化卡槽（使用场景中已有的卡槽）
        SlotManager.Instance?.Initialize();

        // 重置棋盘位置
        SetBoardTransform(boardCenterPosition, 1f);

        // 开始观看阶段
        StartWatchingPhase();
    }

    /// <summary>
    /// 初始化棋子
    /// </summary>
    private void InitializePawns()
    {
        if (npcPawn != null)
        {
            npcPawn.Initialize(currentLevel.npcStartPosition, npcColor);
        }

        if (playerPawn != null)
        {
            playerPawn.Initialize(currentLevel.playerStartPosition, playerColor);
        }
    }

    #region 棋盘位置控制

    /// <summary>
    /// 立即设置棋盘位置和缩放
    /// </summary>
    private void SetBoardTransform(Vector3 position, float scale)
    {
        if (boardContainer != null)
        {
            boardContainer.position = position;
            boardContainer.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// 动画过渡棋盘位置和缩放
    /// </summary>
    private void TransitionBoardTo(Vector3 targetPosition, float targetScale, System.Action onComplete = null)
    {
        if (boardTransitionCoroutine != null)
        {
            StopCoroutine(boardTransitionCoroutine);
        }
        onBoardTransitionComplete = onComplete;
        boardTransitionCoroutine = StartCoroutine(BoardTransitionCoroutine(targetPosition, targetScale));
    }

    /// <summary>
    /// 棋盘过渡动画协程
    /// </summary>
    private IEnumerator BoardTransitionCoroutine(Vector3 targetPosition, float targetScale)
    {
        if (boardContainer == null)
        {
            onBoardTransitionComplete?.Invoke();
            onBoardTransitionComplete = null;
            yield break;
        }

        Vector3 startPosition = boardContainer.position;
        Vector3 startScale = boardContainer.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        float elapsed = 0f;

        while (elapsed < boardTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / boardTransitionDuration;

            // 使用平滑插值
            t = t * t * (3f - 2f * t); // smoothstep

            boardContainer.position = Vector3.Lerp(startPosition, targetPosition, t);
            boardContainer.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        // 确保最终值精确
        boardContainer.position = targetPosition;
        boardContainer.localScale = endScale;

        boardTransitionCoroutine = null;

        // 调用完成回调
        onBoardTransitionComplete?.Invoke();
        onBoardTransitionComplete = null;
    }

    #endregion

    #region 游戏阶段控制

    /// <summary>
    /// 进入观看阶段 - 播放NPC路径
    /// </summary>
    public void StartWatchingPhase()
    {
        CurrentPhase = GamePhase.Watching;
        OnPhaseChanged?.Invoke(CurrentPhase);

        SetUIActive(watchingUI, true);
        SetUIActive(planningUI, false);
        SetUIActive(resultUI, false);

        // 棋盘居中
        TransitionBoardTo(boardCenterPosition, 1f);

        // 重置NPC位置
        if (npcPawn != null)
        {
            npcPawn.SetPosition(currentLevel.npcStartPosition);
        }

        // 开始播放NPC路径动画
        StartCoroutine(PlayNPCPathCoroutine());
    }

    /// <summary>
    /// 播放NPC路径动画
    /// </summary>
    private IEnumerator PlayNPCPathCoroutine()
    {
        yield return new WaitForSeconds(npcAnimationDelay);

        if (npcPawn != null && currentLevel != null)
        {
            // 使用卡牌序列执行
            List<CardData> npcCards = currentLevel.npcCardSequence;
            yield return StartCoroutine(npcPawn.ExecuteCards(npcCards));
        }

        yield return new WaitForSeconds(0.5f);

        // NPC路径播放完毕，循环播放
        StartCoroutine(LoopNPCPath());
    }

    /// <summary>
    /// 循环播放NPC路径
    /// </summary>
    private IEnumerator LoopNPCPath()
    {
        while (CurrentPhase == GamePhase.Watching)
        {
            yield return new WaitForSeconds(1f);

            // 重置位置
            if (npcPawn != null)
            {
                npcPawn.SetPosition(currentLevel.npcStartPosition);
            }

            yield return new WaitForSeconds(0.5f);

            // 再次播放
            if (npcPawn != null && CurrentPhase == GamePhase.Watching && currentLevel != null)
            {
                List<CardData> npcCards = currentLevel.npcCardSequence;
                yield return StartCoroutine(npcPawn.ExecuteCards(npcCards));
            }
        }
    }

    /// <summary>
    /// 进入规划阶段 - 玩家放置卡牌
    /// </summary>
    public void StartPlanningPhase()
    {
        CurrentPhase = GamePhase.Planning;
        OnPhaseChanged?.Invoke(CurrentPhase);

        // 停止所有NPC相关的协程
        StopAllCoroutines();

        // 停止NPC的移动动画
        if (npcPawn != null)
        {
            npcPawn.StopAllCoroutines();
        }

        SetUIActive(watchingUI, false);
        SetUIActive(planningUI, true);
        SetUIActive(resultUI, false);

        // 棋盘左移并缩小
        TransitionBoardTo(boardPlanningPosition, boardPlanningScale);

        // 重置玩家位置到起点
        if (playerPawn != null)
        {
            playerPawn.SetPosition(currentLevel.playerStartPosition);
        }

        // NPC立即移动到路径终点
        if (npcPawn != null && SlotManager.Instance != null)
        {
            Vector2Int npcEndPos = SlotManager.Instance.GetNPCEndPosition();
            npcPawn.SetPosition(npcEndPos);
            Debug.Log($"NPC停在终点: {npcEndPos}");
        }

        // 清空卡槽内容
        SlotManager.Instance?.ClearAllSlots();

        // 显示NPC路径预览
        SlotManager.Instance?.ShowNPCPathPreview();

        // 重新初始化点数
        DeckManager.Instance?.SetPoints(currentLevel.startingPoints);

        Debug.Log("进入规划阶段，请放置卡牌");
    }

    /// <summary>
    /// 进入执行阶段 - 执行玩家指令
    /// </summary>
    public void StartExecutingPhase()
    {
        CurrentPhase = GamePhase.Executing;
        OnPhaseChanged?.Invoke(CurrentPhase);

        // 隐藏规划UI
        SetUIActive(watchingUI, false);
        SetUIActive(planningUI, false);
        SetUIActive(resultUI, false);

        // 隐藏所有路径预览（玩家和NPC）
        SlotManager.Instance?.HideAllPathPreviews();

        // 检查SlotManager
        if (SlotManager.Instance == null)
        {
            Debug.LogError("SlotManager.Instance 为空！");
            ShowResult();
            return;
        }

        // 获取卡槽生成的指令
        List<MoveCommand> commands = SlotManager.Instance.GenerateCommands();

        Debug.Log($"=== 执行阶段 ===");
        Debug.Log($"生成的指令数量: {commands.Count}");

        foreach (var cmd in commands)
        {
            Debug.Log($"  指令: {cmd}");
        }

        if (commands.Count == 0)
        {
            Debug.LogWarning("没有有效的移动指令!");
            // 棋盘回到中心
            TransitionBoardTo(boardCenterPosition, 1f, () => ShowResult());
            return;
        }

        if (playerPawn == null)
        {
            Debug.LogError("playerPawn 为空！");
            ShowResult();
            return;
        }

        // 重置玩家位置
        playerPawn.SetPosition(currentLevel.playerStartPosition);

        // 获取玩家卡牌序列
        List<CardData> playerCards = SlotManager.Instance?.GetSlotCards() ?? new List<CardData>();

        // 先执行棋盘归位动画，完成后再执行玩家移动
        TransitionBoardTo(boardCenterPosition, 1f, () =>
        {
            // 棋盘动画完成后，开始执行玩家卡牌
            StartCoroutine(ExecutePlayerCardsCoroutine(playerCards));
        });
    }

    /// <summary>
    /// 执行玩家卡牌动画
    /// </summary>
    private IEnumerator ExecutePlayerCardsCoroutine(List<CardData> cards)
    {
        Debug.Log($"开始执行玩家卡牌，共 {cards.Count} 张...");

        // 短暂等待
        yield return new WaitForSeconds(0.3f);

        if (playerPawn != null)
        {
            yield return StartCoroutine(playerPawn.ExecuteCards(cards));
            Debug.Log("玩家执行完成！");
        }

        yield return new WaitForSeconds(0.5f);

        // 移动完成，进入结算
        ShowResult();
    }

    /// <summary>
    /// 显示结果
    /// </summary>
    private void ShowResult()
    {
        CurrentPhase = GamePhase.Result;
        OnPhaseChanged?.Invoke(CurrentPhase);

        SetUIActive(watchingUI, false);
        SetUIActive(planningUI, false);
        SetUIActive(resultUI, true);

        // 计算分数
        CalculateAndShowScore();
    }

    /// <summary>
    /// 计算并显示分数
    /// </summary>
    private void CalculateAndShowScore()
    {
        if (currentLevel == null)
        {
            Debug.LogError("没有关卡数据!");
            return;
        }

        // 获取NPC卡牌序列
        List<CardData> npcCards = currentLevel.npcCardSequence;

        // 获取玩家卡牌序列
        List<CardData> playerCards = SlotManager.Instance?.GetSlotCards() ?? new List<CardData>();

        // 计算分数
        CompareResult result;
        if (ScoreCalculator.Instance != null)
        {
            result = ScoreCalculator.Instance.CalculateScore(npcCards, playerCards, currentLevel);
        }
        else
        {
            // 手动计算（备用）
            result = CalculateScoreManually(npcCards, playerCards);
        }

        LastScoreResult = result;

        // 通知UI更新
        if (ResultUI.Instance != null)
        {
            ResultUI.Instance.ShowResult(result, currentLevel);
        }

        Debug.Log($"关卡完成！得分: {result.totalScore}/{result.maxScore}, 评级: {result.rank}, 通关: {result.isPassed}");
    }

    /// <summary>
    /// 手动计算分数（备用方法）
    /// </summary>
    private CompareResult CalculateScoreManually(List<CardData> npcCards, List<CardData> playerCards)
    {
        CompareResult result = new CompareResult();

        int maxSteps = Mathf.Max(npcCards.Count, playerCards.Count);
        result.totalSteps = npcCards.Count;

        // 计算满分
        foreach (var card in npcCards)
        {
            if (card != null)
            {
                result.maxScore += card.valuePoints;
            }
        }

        // 逐步对比
        for (int i = 0; i < maxSteps; i++)
        {
            CardData npcCard = i < npcCards.Count ? npcCards[i] : null;
            CardData playerCard = i < playerCards.Count ? playerCards[i] : null;

            StepCompareResult stepResult = new StepCompareResult(i, npcCard, playerCard);
            result.stepResults.Add(stepResult);

            if (stepResult.isCorrect)
            {
                result.totalScore += stepResult.scoreGained;
                result.correctCount++;
            }
        }

        // 获取评级
        if (currentLevel != null)
        {
            result.rank = currentLevel.GetRank(result.totalScore);
            result.isPassed = currentLevel.IsPassed(result.totalScore);
        }

        return result;
    }

    #endregion

    #region 公开方法（供其他脚本调用）

    /// <summary>
    /// 获取当前关卡的NPC指令
    /// </summary>
    public List<MoveCommand> GetCurrentLevelNPCCommands()
    {
        return currentLevel?.GetNPCCommands() ?? new List<MoveCommand>();
    }

    /// <summary>
    /// 获取当前关卡数据
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        return currentLevel;
    }

    #endregion

    #region 按钮回调

    /// <summary>
    /// 开始推演按钮（从观看进入规划）
    /// </summary>
    public void OnStartPlanningButton()
    {
        if (CurrentPhase == GamePhase.Watching)
        {
            StopAllCoroutines();
            StartPlanningPhase();
        }
    }

    /// <summary>
    /// 完成按钮（从规划进入执行）
    /// </summary>
    public void OnExecuteButton()
    {
        if (CurrentPhase == GamePhase.Planning)
        {
            StartExecutingPhase();
        }
    }

    /// <summary>
    /// 重新观看按钮
    /// </summary>
    public void OnReplayButton()
    {
        StopAllCoroutines();
        StartWatchingPhase();
    }

    /// <summary>
    /// 重新开始关卡
    /// </summary>
    public void OnRestartButton()
    {
        StopAllCoroutines();
        StartLevel(currentLevel);
    }

    /// <summary>
    /// 下一关按钮
    /// </summary>
    public void OnNextLevelButton()
    {
        // TODO: 加载下一关
        Debug.Log("下一关功能待实现");
    }

    #endregion

    /// <summary>
    /// 设置UI显示状态
    /// </summary>
    private void SetUIActive(GameObject uiObject, bool active)
    {
        if (uiObject != null)
        {
            uiObject.SetActive(active);
        }
    }
}