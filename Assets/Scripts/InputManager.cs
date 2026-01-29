using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入管理器 - 处理玩家输入
/// 挂载到 GameManager 物体上
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("卡牌生成设置")]
    [SerializeField] private int maxCards = 5;              // 最大卡牌数量
    [SerializeField] private List<CardData> cardPool;       // 可生成的卡牌池（可选）
    
    [Header("测试用颜色（如果没有CardData）")]
    [SerializeField] private Color[] testColors = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.cyan,
        Color.magenta
    };
    
    private int cardCount = 0;
    
    private void Update()
    {
        // 按 J 获得卡牌
        if (Input.GetKeyDown(KeyCode.J))
        {
            TryAddCard();
        }
        
        // 按 K 清空所有卡牌（测试用）
        if (Input.GetKeyDown(KeyCode.K))
        {
            ClearAllCards();
        }
    }
    
    /// <summary>
    /// 尝试添加一张卡牌
    /// </summary>
    private void TryAddCard()
    {
        if (CardManager.Instance == null)
        {
            Debug.LogError("InputManager: CardManager 不存在");
            return;
        }
        
        // 检查卡牌数量限制
        if (cardCount >= maxCards)
        {
            Debug.Log($"已达到最大卡牌数量 ({maxCards})");
            return;
        }
        
        // 创建卡牌数据
        CardData newCardData = CreateRandomCardData();
        
        // 添加到卡池
        CardUI newCard = CardManager.Instance.CreateCard(newCardData);
        
        if (newCard != null)
        {
            cardCount++;
            Debug.Log($"获得卡牌！当前数量: {cardCount}/{maxCards}");
        }
    }
    
    /// <summary>
    /// 创建随机卡牌数据
    /// </summary>
    private CardData CreateRandomCardData()
    {
        // 如果有预设的卡牌池，从中随机选择
        if (cardPool != null && cardPool.Count > 0)
        {
            return cardPool[Random.Range(0, cardPool.Count)];
        }
        
        // 否则创建测试用卡牌
        CardData card = ScriptableObject.CreateInstance<CardData>();
        
        // 随机方向
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1)
        };
        
        string[] directionNames = new string[]
        {
            "上", "下", "左", "右", "右上", "左下", "右下", "左上"
        };
        
        int randomIndex = Random.Range(0, directions.Length);
        
        card.cardName = $"移动({directionNames[randomIndex]})";
        card.description = $"向{directionNames[randomIndex]}移动1格";
        card.cardColor = testColors[cardCount % testColors.Length];
        card.cardType = CardType.Move;
        card.moveDirection = directions[randomIndex];
        card.moveDistance = 1;
        card.actionCost = 1;
        
        return card;
    }
    
    /// <summary>
    /// 清空所有卡牌（测试用）
    /// </summary>
    private void ClearAllCards()
    {
        // 这里需要在 CardManager 中添加清空方法
        // 暂时只重置计数
        cardCount = 0;
        Debug.Log("卡牌计数已重置（需要实现完整清空功能）");
    }
    
    /// <summary>
    /// 减少卡牌计数（当卡牌被使用时调用）
    /// </summary>
    public void OnCardUsed(int count = 1)
    {
        cardCount = Mathf.Max(0, cardCount - count);
    }
}
