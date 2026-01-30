using UnityEngine;

/// <summary>
/// 卡牌类型
/// </summary>
public enum CardType
{
    Direction,  // 方向牌（上下左右）
    Step        // 步数牌（1、2、3步）
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
    
    [Header("经济")]
    [Tooltip("购买/使用消耗的点数")]
    public int cost = 1;
    
    [Tooltip("出售获得的点数")]
    public int sellValue = 1;
    
    [Header("颜色标识")]
    public Color cardColor = Color.white;
    
    /// <summary>
    /// 获取卡牌描述
    /// </summary>
    public string GetDescription()
    {
        if (cardType == CardType.Direction)
        {
            string dirName = direction switch
            {
                Direction.Up => "向上",
                Direction.Down => "向下",
                Direction.Left => "向左",
                Direction.Right => "向右",
                _ => "?"
            };
            return dirName;
        }
        else
        {
            return $"{stepCount}步";
        }
    }
}
