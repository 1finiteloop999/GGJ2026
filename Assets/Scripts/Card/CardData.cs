using UnityEngine;

/// <summary>
/// 卡牌类型
/// </summary>
public enum CardType
{
    Direction,  // 方向牌（上下左右）
    Step,       // 步数牌（1、2、3步）
    Pause,      // 停顿牌
    Action,     // 动作牌（鞠躬、跳跃、坐下、招手）
    Expression, // 表情牌（笑、愤怒）
    Spell       // 法术牌
}

/// <summary>
/// 卡牌数据 - ScriptableObject用于配置卡牌属性
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "MaskMimicry/Card Data")]
public class CardData : ScriptableObject
{
    [Header("基本信息")]
    public string cardName;
    public CardType cardType;
    public Sprite cardSprite;

    [Header("卡牌效果")]
    [Tooltip("方向牌：表示移动方向")]
    public Direction direction;

    [Tooltip("步数牌：表示移动步数")]
    public int stepCount = 1;

    [Tooltip("动作牌：动作名称")]
    public string actionName;

    [Tooltip("表情牌：表情名称")]
    public string expressionName;

    [Header("价值与经济")]
    [Tooltip("卡牌价值点数（用于计算得分）")]
    public int valuePoints = 2;

    [Tooltip("购买消耗的点数")]
    public int buyCost = 1;

    [Tooltip("出售获得的点数")]
    public int sellValue = 1;

    [Header("颜色标识")]
    public Color cardColor = Color.white;

    /// <summary>
    /// 获取卡牌描述
    /// </summary>
    public string GetDescription()
    {
        switch (cardType)
        {
            case CardType.Direction:
                return direction switch
                {
                    Direction.Up => "向上",
                    Direction.Down => "向下",
                    Direction.Left => "向左",
                    Direction.Right => "向右",
                    _ => "?"
                };
            case CardType.Step:
                return $"{stepCount}步";
            case CardType.Pause:
                return "停顿";
            case CardType.Action:
                return actionName;
            case CardType.Expression:
                return expressionName;
            case CardType.Spell:
                return cardName;
            default:
                return cardName;
        }
    }

    /// <summary>
    /// 获取用于比较的唯一标识
    /// </summary>
    public string GetCompareKey()
    {
        switch (cardType)
        {
            case CardType.Direction:
                return $"DIR_{direction}";
            case CardType.Step:
                return $"STEP_{stepCount}";
            case CardType.Pause:
                return "PAUSE";
            case CardType.Action:
                return $"ACT_{actionName}";
            case CardType.Expression:
                return $"EXP_{expressionName}";
            default:
                return cardName;
        }
    }
}