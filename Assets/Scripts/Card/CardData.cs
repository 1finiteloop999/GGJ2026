using UnityEngine;

/// <summary>
/// 卡牌数据 - ScriptableObject
/// 用于定义每张卡牌的属性
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "CardGame/Card Data")]
public class CardData : ScriptableObject
{
    [Header("基本信息")]
    public string cardName = "新卡牌";
    public string description = "卡牌描述";
    public Sprite cardImage;        // 卡牌图片（暂时用纯色方块）
    public Color cardColor = Color.white;  // 暂时用颜色代替图片
    
    [Header("费用")]
    public int actionCost = 1;      // 行动力消耗
    
    [Header("效果 - 移动")]
    public Vector2Int moveDirection;  // 移动方向
    public int moveDistance = 1;      // 移动距离
    
    [Header("卡牌类型")]
    public CardType cardType = CardType.Move;
}

/// <summary>
/// 卡牌类型枚举
/// </summary>
public enum CardType
{
    Move,       // 移动牌
    Attack,     // 攻击牌（预留）
    Skill       // 技能牌（预留）
}
