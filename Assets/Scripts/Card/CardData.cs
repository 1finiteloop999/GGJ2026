using UnityEngine;

/// <summary>
/// 方向类型（包含无方向）
/// </summary>
public enum DirectionType
{
    None,   // 无方向
    Up,     // 上
    Down,   // 下
    Left,   // 左
    Right   // 右
}

/// <summary>
/// 动作类型
/// </summary>
public enum ActionType
{
    None,       // 无动作
    Bow,        // 鞠躬
    Jump,       // 跳跃
    SitDown,    // 坐下
    Wave        // 招手
}

/// <summary>
/// 表情类型
/// </summary>
public enum ExpressionType
{
    None,   // 无表情
    Laugh,  // 笑
    Angry   // 愤怒
}

/// <summary>
/// 卡牌数据 - ScriptableObject用于配置卡牌属性
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "MaskMimicry/Card Data")]
public class CardData : ScriptableObject
{
    [Header("===== 基本信息 =====")]
    public string cardName;

    [Header("卡牌美术")]
    [Tooltip("卡牌显示的图片")]
    public Sprite cardSprite;

    [Tooltip("卡牌背景颜色")]
    public Color cardColor = Color.white;

    [Header("===== 卡牌类型 =====")]
    [Tooltip("是否为法术牌（法术牌只能拖入USE区域使用）")]
    public bool isSpellCard = false;

    [Header("===== 行动牌效果 =====")]

    [Header("1. 方向（仅管理转向）")]
    [Tooltip("移动方向，None表示不改变方向")]
    public DirectionType direction = DirectionType.None;

    [Header("2. 行动步数")]
    [Tooltip("移动步数：0=不移动，1/2/3=移动对应格数")]
    [Range(0, 3)]
    public int stepCount = 0;

    [Header("3. 停顿")]
    [Tooltip("是否为停顿卡（停顿一拍，不做任何动作）")]
    public bool isPause = false;

    [Header("4. 动作")]
    [Tooltip("动作类型，None表示无动作")]
    public ActionType actionType = ActionType.None;

    [Header("5. 表情")]
    [Tooltip("表情类型，None表示无表情")]
    public ExpressionType expressionType = ExpressionType.None;

    [Header("===== 法术牌效果 =====")]
    [Tooltip("使用后获得的点数（0-4）")]
    [Range(0, 4)]
    public int spellGainPoints = 0;

    [Tooltip("使用后是否让NPC重走一次路径")]
    public bool spellReplayNPC = false;

    [Tooltip("使用后随机增加的手牌数量（0-3）")]
    [Range(0, 3)]
    public int spellAddCards = 0;

    [Header("===== 价值与经济 =====")]
    [Tooltip("卡牌价值点数（用于计算得分）")]
    public int valuePoints = 2;

    [Tooltip("购买消耗的点数")]
    public int buyCost = 1;

    [Tooltip("出售获得的点数")]
    public int sellValue = 1;

    /// <summary>
    /// 获取卡牌描述
    /// </summary>
    public string GetDescription()
    {
        // 法术牌
        if (isSpellCard)
        {
            return GetSpellDescription();
        }

        // 停顿卡
        if (isPause)
        {
            return "停顿";
        }

        // 方向卡
        if (direction != DirectionType.None && stepCount == 0)
        {
            return direction switch
            {
                DirectionType.Up => "向上",
                DirectionType.Down => "向下",
                DirectionType.Left => "向左",
                DirectionType.Right => "向右",
                _ => "?"
            };
        }

        // 步数卡
        if (stepCount > 0 && direction == DirectionType.None)
        {
            return $"{stepCount}步";
        }

        // 方向+步数组合卡
        if (direction != DirectionType.None && stepCount > 0)
        {
            string dirName = direction switch
            {
                DirectionType.Up => "上",
                DirectionType.Down => "下",
                DirectionType.Left => "左",
                DirectionType.Right => "右",
                _ => "?"
            };
            return $"{dirName}{stepCount}步";
        }

        // 动作卡
        if (actionType != ActionType.None)
        {
            return actionType switch
            {
                ActionType.Bow => "鞠躬",
                ActionType.Jump => "跳跃",
                ActionType.SitDown => "坐下",
                ActionType.Wave => "招手",
                _ => "动作"
            };
        }

        // 表情卡
        if (expressionType != ExpressionType.None)
        {
            return expressionType switch
            {
                ExpressionType.Laugh => "笑",
                ExpressionType.Angry => "愤怒",
                _ => "表情"
            };
        }

        return cardName;
    }

    /// <summary>
    /// 获取法术牌描述
    /// </summary>
    private string GetSpellDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (spellGainPoints > 0)
        {
            sb.Append($"+{spellGainPoints}点");
        }

        if (spellReplayNPC)
        {
            if (sb.Length > 0) sb.Append(" ");
            sb.Append("重播NPC");
        }

        if (spellAddCards > 0)
        {
            if (sb.Length > 0) sb.Append(" ");
            sb.Append($"+{spellAddCards}牌");
        }

        return sb.Length > 0 ? sb.ToString() : "法术";
    }

    /// <summary>
    /// 获取用于比较的唯一标识
    /// </summary>
    public string GetCompareKey()
    {
        // 法术牌不参与比较
        if (isSpellCard)
        {
            return "SPELL_" + cardName;
        }

        // 停顿卡
        if (isPause)
        {
            return "PAUSE";
        }

        // 组合所有有效属性作为key
        string key = "";

        if (direction != DirectionType.None)
        {
            key += $"DIR_{direction}_";
        }

        if (stepCount > 0)
        {
            key += $"STEP_{stepCount}_";
        }

        if (actionType != ActionType.None)
        {
            key += $"ACT_{actionType}_";
        }

        if (expressionType != ExpressionType.None)
        {
            key += $"EXP_{expressionType}_";
        }

        // 如果什么都没有，返回卡牌名
        if (string.IsNullOrEmpty(key))
        {
            return cardName;
        }

        return key.TrimEnd('_');
    }

    /// <summary>
    /// 是否是法术牌
    /// </summary>
    public bool IsSpellCard => isSpellCard;

    /// <summary>
    /// 是否是行动牌（非法术牌）
    /// </summary>
    public bool IsActionTypeCard => !isSpellCard;

    /// <summary>
    /// 是否是纯方向卡
    /// </summary>
    public bool IsDirectionCard => !isSpellCard && direction != DirectionType.None && stepCount == 0 && !isPause && actionType == ActionType.None && expressionType == ExpressionType.None;

    /// <summary>
    /// 是否是纯步数卡
    /// </summary>
    public bool IsStepCard => !isSpellCard && stepCount > 0 && direction == DirectionType.None && !isPause && actionType == ActionType.None && expressionType == ExpressionType.None;

    /// <summary>
    /// 是否是停顿卡
    /// </summary>
    public bool IsPauseCard => !isSpellCard && isPause;

    /// <summary>
    /// 是否是动作卡
    /// </summary>
    public bool IsActionCard => !isSpellCard && actionType != ActionType.None;

    /// <summary>
    /// 是否是表情卡
    /// </summary>
    public bool IsExpressionCard => !isSpellCard && expressionType != ExpressionType.None;

    /// <summary>
    /// 是否会产生移动
    /// </summary>
    public bool HasMovement => !isSpellCard && (direction != DirectionType.None || stepCount > 0);

    /// <summary>
    /// 是否可以放入卡槽（行动牌可以，法术牌不可以）
    /// </summary>
    public bool CanPlaceInSlot => !isSpellCard;

    /// <summary>
    /// 是否可以拖入USE区域使用（法术牌可以，行动牌不可以）
    /// </summary>
    public bool CanUseDirectly => isSpellCard;
}