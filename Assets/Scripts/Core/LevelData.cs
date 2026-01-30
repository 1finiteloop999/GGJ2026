using UnityEngine;
using System.Collections.Generic;

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
    
    [Header("NPC路径")]
    [Tooltip("NPC的移动指令序列")]
    public List<MoveCommandData> npcPath = new List<MoveCommandData>();
    
    [Header("通关条件")]
    [Tooltip("通关所需模仿度(0-100)")]
    [Range(0, 100)]
    public int requiredMimicryPercent = 70;
    
    [Header("初始手牌")]
    [Tooltip("玩家初始获得的卡牌")]
    public List<CardData> initialCards = new List<CardData>();
    
    [Header("卡槽数量")]
    [Tooltip("本关卡可用的卡槽数量")]
    public int slotCount = 6;
    
    /// <summary>
    /// 获取NPC移动指令列表
    /// </summary>
    public List<MoveCommand> GetNPCCommands()
    {
        List<MoveCommand> commands = new List<MoveCommand>();
        
        foreach (var cmdData in npcPath)
        {
            commands.Add(new MoveCommand(cmdData.direction, cmdData.steps));
        }
        
        return commands;
    }
}

/// <summary>
/// 用于在Inspector中配置移动指令
/// </summary>
[System.Serializable]
public class MoveCommandData
{
    public Direction direction;
    [Range(1, 5)]
    public int steps = 1;
}
