using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 单步对比结果
/// </summary>
[System.Serializable]
public class StepCompareResult
{
    public int stepIndex;           // 第几步
    public CardData npcCard;        // NPC的卡牌
    public CardData playerCard;     // 玩家的卡牌
    public bool isCorrect;          // 是否正确
    public int scoreGained;         // 获得的分数

    public StepCompareResult(int index, CardData npc, CardData player)
    {
        stepIndex = index;
        npcCard = npc;
        playerCard = player;

        // 比较是否相同
        if (npcCard != null && playerCard != null)
        {
            isCorrect = npcCard.GetCompareKey() == playerCard.GetCompareKey();
            scoreGained = isCorrect ? npcCard.valuePoints : 0;
        }
        else if (npcCard == null && playerCard == null)
        {
            isCorrect = true;
            scoreGained = 0;
        }
        else
        {
            isCorrect = false;
            scoreGained = 0;
        }
    }
}

/// <summary>
/// 完整的对比结果
/// </summary>
[System.Serializable]
public class CompareResult
{
    public List<StepCompareResult> stepResults = new List<StepCompareResult>();
    public int totalScore;          // 玩家总得分
    public int maxScore;            // 满分
    public int correctCount;        // 正确的步数
    public int totalSteps;          // 总步数
    public RankType rank;           // 评级
    public bool isPassed;           // 是否通关

    /// <summary>
    /// 获取得分百分比
    /// </summary>
    public float GetScorePercent()
    {
        if (maxScore <= 0) return 0f;
        return (float)totalScore / maxScore * 100f;
    }
}

/// <summary>
/// 分数计算器 - 对比玩家和NPC的卡牌序列
/// </summary>
public class ScoreCalculator : MonoBehaviour
{
    public static ScoreCalculator Instance { get; private set; }

    // 最近一次的对比结果
    public CompareResult LastResult { get; private set; }

    // 事件
    public System.Action<CompareResult> OnScoreCalculated;

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
    /// 计算分数
    /// </summary>
    /// <param name="npcCards">NPC的卡牌序列</param>
    /// <param name="playerCards">玩家的卡牌序列</param>
    /// <param name="levelData">关卡数据（用于获取评级阈值）</param>
    /// <returns>对比结果</returns>
    public CompareResult CalculateScore(List<CardData> npcCards, List<CardData> playerCards, LevelData levelData)
    {
        CompareResult result = new CompareResult();

        // 获取最大步数
        int maxSteps = Mathf.Max(npcCards.Count, playerCards.Count);
        result.totalSteps = npcCards.Count;

        // 计算满分
        result.maxScore = 0;
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

            // 打印调试信息
            string npcName = npcCard != null ? npcCard.GetDescription() : "空";
            string playerName = playerCard != null ? playerCard.GetDescription() : "空";
            string correctStr = stepResult.isCorrect ? "✓" : "✗";
            Debug.Log($"[ScoreCalculator] 第{i + 1}步: NPC={npcName}, 玩家={playerName}, {correctStr}, +{stepResult.scoreGained}分");
        }

        // 获取评级
        if (levelData != null)
        {
            result.rank = levelData.GetRank(result.totalScore);
            result.isPassed = levelData.IsPassed(result.totalScore);
        }
        else
        {
            // 没有关卡数据时使用默认评级
            result.rank = result.totalScore >= result.maxScore * 0.9f ? RankType.SSS :
                          result.totalScore >= result.maxScore * 0.7f ? RankType.SS :
                          result.totalScore >= result.maxScore * 0.5f ? RankType.S :
                          result.totalScore >= result.maxScore * 0.3f ? RankType.A : RankType.F;
            result.isPassed = result.rank >= RankType.A;
        }

        Debug.Log($"[ScoreCalculator] === 最终结果 ===");
        Debug.Log($"[ScoreCalculator] 得分: {result.totalScore}/{result.maxScore} ({result.GetScorePercent():F1}%)");
        Debug.Log($"[ScoreCalculator] 正确: {result.correctCount}/{result.totalSteps}");
        Debug.Log($"[ScoreCalculator] 评级: {result.rank}");
        Debug.Log($"[ScoreCalculator] 通关: {(result.isPassed ? "是" : "否")}");

        LastResult = result;
        OnScoreCalculated?.Invoke(result);

        return result;
    }

    /// <summary>
    /// 使用SlotManager和LevelData计算分数的便捷方法
    /// </summary>
    public CompareResult CalculateFromCurrentState()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ScoreCalculator] GameManager不存在!");
            return null;
        }

        // 获取关卡数据
        LevelData levelData = GameManager.Instance.GetCurrentLevel();
        if (levelData == null)
        {
            Debug.LogError("[ScoreCalculator] 没有关卡数据!");
            return null;
        }

        // 获取NPC卡牌序列
        List<CardData> npcCards = levelData.npcCardSequence;

        // 获取玩家卡牌序列
        List<CardData> playerCards = SlotManager.Instance?.GetSlotCards() ?? new List<CardData>();

        return CalculateScore(npcCards, playerCards, levelData);
    }
}