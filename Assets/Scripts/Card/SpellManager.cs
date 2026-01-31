using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 法术牌管理器 - 处理法术牌的使用效果
/// </summary>
public class SpellManager : MonoBehaviour
{
    public static SpellManager Instance { get; private set; }

    [Header("随机抽卡来源")]
    [Tooltip("随机抽卡的卡池（通常是当前关卡的牌库）")]
    [SerializeField] private List<CardData> randomCardPool = new List<CardData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 设置随机抽卡的卡池
    /// </summary>
    public void SetRandomCardPool(List<CardData> cards)
    {
        randomCardPool.Clear();
        if (cards != null)
        {
            // 只添加非法术牌到卡池
            foreach (var card in cards)
            {
                if (card != null && !card.IsSpellCard)
                {
                    randomCardPool.Add(card);
                }
            }
        }
        Debug.Log($"[SpellManager] 设置随机卡池，共 {randomCardPool.Count} 张卡");
    }

    /// <summary>
    /// 使用法术牌
    /// </summary>
    public void UseSpellCard(CardData spellCard)
    {
        if (spellCard == null || !spellCard.IsSpellCard)
        {
            Debug.LogWarning("[SpellManager] 尝试使用非法术牌！");
            return;
        }

        Debug.Log($"[SpellManager] 使用法术牌: {spellCard.GetDescription()}");

        // 效果1：获得点数
        if (spellCard.spellGainPoints > 0)
        {
            GainPoints(spellCard.spellGainPoints);
        }

        // 效果2：NPC重走路径
        if (spellCard.spellReplayNPC)
        {
            ReplayNPCPath();
        }

        // 效果3：随机增加手牌
        if (spellCard.spellAddCards > 0)
        {
            AddRandomCards(spellCard.spellAddCards);
        }
    }

    /// <summary>
    /// 获得点数
    /// </summary>
    private void GainPoints(int points)
    {
        DeckManager.Instance?.ModifyPoints(points);
        Debug.Log($"[SpellManager] 获得 {points} 点数");
    }

    /// <summary>
    /// NPC重走路径
    /// </summary>
    private void ReplayNPCPath()
    {
        Debug.Log("[SpellManager] NPC重走路径");
        StartCoroutine(ReplayNPCPathCoroutine());
    }

    /// <summary>
    /// NPC重走路径协程
    /// </summary>
    private IEnumerator ReplayNPCPathCoroutine()
    {
        if (GameManager.Instance == null) yield break;

        PawnController npcPawn = GameManager.Instance.GetNPCPawn();
        LevelData levelData = GameManager.Instance.GetCurrentLevel();

        if (npcPawn == null || levelData == null) yield break;

        // 重置NPC位置到起点
        npcPawn.SetPosition(levelData.npcStartPosition);

        yield return new WaitForSeconds(0.3f);

        // 播放NPC路径
        List<CardData> npcCards = levelData.npcCardSequence;
        yield return StartCoroutine(npcPawn.ExecuteCards(npcCards));

        // 播放完毕后设置到终点并重置图标
        if (SlotManager.Instance != null)
        {
            Vector2Int npcEndPos = SlotManager.Instance.GetNPCEndPosition();
            npcPawn.SetPosition(npcEndPos);
        }
        npcPawn.ResetToIdle();
    }

    /// <summary>
    /// 随机增加手牌
    /// </summary>
    private void AddRandomCards(int count)
    {
        if (randomCardPool == null || randomCardPool.Count == 0)
        {
            Debug.LogWarning("[SpellManager] 随机卡池为空，无法抽卡！");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // 随机选择一张卡
            int randomIndex = Random.Range(0, randomCardPool.Count);
            CardData randomCard = randomCardPool[randomIndex];

            if (randomCard != null)
            {
                DeckManager.Instance?.AddCardToHand(randomCard);
                Debug.Log($"[SpellManager] 随机抽到: {randomCard.GetDescription()}");
            }
        }
    }

    /// <summary>
    /// 检查卡牌是否可以在USE区域使用（只有法术牌可以）
    /// </summary>
    public bool CanUseInUseArea(CardData cardData)
    {
        return cardData != null && cardData.IsSpellCard;
    }
}