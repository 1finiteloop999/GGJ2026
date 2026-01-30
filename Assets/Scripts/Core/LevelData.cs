using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 评级类型
/// </summary>
public enum RankType
{
    F,   // 未通关
    A,   // 合格
    S,   // 良好
    SS,  // 优秀
    SSS  // 完美
}

/// <summary>
/// 关卡数据 - 配置每个关卡的参数
/// </summary>
[CreateAssetMenu(fileName = "NewLevel", menuName = "MaskMimicry/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("关卡信息")]
    public string levelName;
    public int levelIndex;

    [Header("起始设置")]
    [Tooltip("玩家初始点数")]
    public int startingPoints = 10;

    [Tooltip("玩家起始位置")]
    public Vector2Int playerStartPosition = new Vector2Int(0, 0);

    [Tooltip("NPC起始位置")]
    public Vector2Int npcStartPosition = new Vector2Int(0, 0);

    [Header("NPC卡牌序列")]
    [Tooltip("NPC的卡牌序列（用于对比和演示）")]
    public List<CardData> npcCardSequence = new List<CardData>();

    [Header("分数线设置")]
    [Tooltip("A级所需分数（通关线）")]
    public int scoreA = 6;

    [Tooltip("S级所需分数")]
    public int scoreS = 10;

    [Tooltip("SS级所需分数")]
    public int scoreSS = 14;

    [Tooltip("SSS级所需分数（满分）")]
    public int scoreSSS = 18;

    [Header("初始手牌")]
    [Tooltip("玩家初始获得的卡牌")]
    public List<CardData> initialCards = new List<CardData>();

    [Header("卡槽数量")]
    [Tooltip("本关卡可用的卡槽数量")]
    public int slotCount = 6;

    /// <summary>
    /// 获取NPC移动指令列表（用于演示动画）
    /// </summary>
    public List<MoveCommand> GetNPCCommands()
    {
        List<MoveCommand> commands = new List<MoveCommand>();

        Direction? currentDirection = null;

        foreach (var card in npcCardSequence)
        {
            if (card == null) continue;

            if (card.cardType == CardType.Direction)
            {
                if (currentDirection.HasValue)
                {
                    commands.Add(new MoveCommand(currentDirection.Value, 1));
                }
                currentDirection = card.direction;
            }
            else if (card.cardType == CardType.Step)
            {
                if (currentDirection.HasValue)
                {
                    commands.Add(new MoveCommand(currentDirection.Value, card.stepCount));
                    currentDirection = null;
                }
            }
            // Pause、Action、Expression 暂不生成移动指令
        }

        if (currentDirection.HasValue)
        {
            commands.Add(new MoveCommand(currentDirection.Value, 1));
        }

        return commands;
    }

    /// <summary>
    /// 计算NPC卡牌序列的总分（满分）
    /// </summary>
    public int GetMaxScore()
    {
        int total = 0;
        foreach (var card in npcCardSequence)
        {
            if (card != null)
            {
                total += card.valuePoints;
            }
        }
        return total;
    }

    /// <summary>
    /// 根据分数获取评级
    /// </summary>
    public RankType GetRank(int score)
    {
        if (score >= scoreSSS) return RankType.SSS;
        if (score >= scoreSS) return RankType.SS;
        if (score >= scoreS) return RankType.S;
        if (score >= scoreA) return RankType.A;
        return RankType.F;
    }

    /// <summary>
    /// 检查是否通关
    /// </summary>
    public bool IsPassed(int score)
    {
        return score >= scoreA;
    }
}