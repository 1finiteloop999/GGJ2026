using UnityEngine;

/// <summary>
/// 角色视觉数据 - 存储角色的所有图标（NPC和玩家各一份）
/// </summary>
[CreateAssetMenu(fileName = "NewPawnVisual", menuName = "MaskMimicry/Pawn Visual Data")]
public class PawnVisualData : ScriptableObject
{
    [Header("===== 基本图标 =====")]
    [Tooltip("默认/待机图标")]
    public Sprite spriteIdle;
    
    [Header("方向图标（可选，用于显示朝向）")]
    [Tooltip("朝上图标")]
    public Sprite spriteUp;
    [Tooltip("朝下图标")]
    public Sprite spriteDown;
    [Tooltip("朝左图标")]
    public Sprite spriteLeft;
    [Tooltip("朝右图标")]
    public Sprite spriteRight;
    
    [Header("===== 动作图标 =====")]
    [Tooltip("鞠躬图标")]
    public Sprite spriteBow;
    [Tooltip("跳跃图标")]
    public Sprite spriteJump;
    [Tooltip("坐下图标")]
    public Sprite spriteSitDown;
    [Tooltip("招手图标")]
    public Sprite spriteWave;
    
    [Header("===== 表情图标 =====")]
    [Tooltip("笑图标")]
    public Sprite spriteLaugh;
    [Tooltip("愤怒图标")]
    public Sprite spriteAngry;
    
    /// <summary>
    /// 获取方向对应的图标
    /// </summary>
    public Sprite GetDirectionSprite(DirectionType direction)
    {
        Sprite result = direction switch
        {
            DirectionType.Up => spriteUp,
            DirectionType.Down => spriteDown,
            DirectionType.Left => spriteLeft,
            DirectionType.Right => spriteRight,
            _ => null
        };
        
        // 如果没有对应方向图标，返回默认图标
        return result != null ? result : spriteIdle;
    }
    
    /// <summary>
    /// 获取动作对应的图标
    /// </summary>
    public Sprite GetActionSprite(ActionType action)
    {
        return action switch
        {
            ActionType.Bow => spriteBow,
            ActionType.Jump => spriteJump,
            ActionType.SitDown => spriteSitDown,
            ActionType.Wave => spriteWave,
            _ => null
        };
    }
    
    /// <summary>
    /// 获取表情对应的图标
    /// </summary>
    public Sprite GetExpressionSprite(ExpressionType expression)
    {
        return expression switch
        {
            ExpressionType.Laugh => spriteLaugh,
            ExpressionType.Angry => spriteAngry,
            _ => null
        };
    }
}
