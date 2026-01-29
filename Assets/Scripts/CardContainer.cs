using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 卡牌容器 - 自动居中排列卡牌
/// 简化版：不使用Canvas Override Sorting，改用SiblingIndex
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardContainer : MonoBehaviour
{
    [Header("布局设置")]
    [SerializeField] private float cardSpacing = 130f;
    [SerializeField] private float maxWidth = 1000f;
    [SerializeField] private bool autoAdjustSpacing = true;
    [SerializeField] private float minSpacing = 80f;

    [Header("动画设置")]
    [SerializeField] private float arrangeDuration = 0.25f;
    [SerializeField] private Ease arrangeEase = Ease.OutBack;

    [Header("容器类型")]
    [SerializeField] private ContainerType containerType = ContainerType.CardPool;

    private List<CardUI> cards = new List<CardUI>();
    private RectTransform rectTransform;

    public enum ContainerType { CardPool, PlayArea }

    public ContainerType GetContainerType() => containerType;
    public List<CardUI> GetCards() => new List<CardUI>(cards);
    public int CardCount => cards.Count;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 添加卡牌
    /// </summary>
    public void AddCard(CardUI card, bool animate = true)
    {
        if (card == null || cards.Contains(card)) return;

        cards.Add(card);
        card.transform.SetParent(transform, false);
        card.SetContainer(this);
        card.transform.localScale = Vector3.one;
        card.transform.localRotation = Quaternion.identity;

        ArrangeCards(animate);
    }

    /// <summary>
    /// 移除卡牌
    /// </summary>
    public void RemoveCard(CardUI card, bool animate = true)
    {
        if (card == null || !cards.Contains(card)) return;

        cards.Remove(card);
        ArrangeCards(animate);
    }

    /// <summary>
    /// 插入卡牌
    /// </summary>
    public void InsertCard(CardUI card, int index, bool animate = true)
    {
        if (card == null) return;

        if (cards.Contains(card))
            cards.Remove(card);

        index = Mathf.Clamp(index, 0, cards.Count);
        cards.Insert(index, card);
        card.transform.SetParent(transform, false);
        card.SetContainer(this);
        card.transform.localScale = Vector3.one;
        card.transform.localRotation = Quaternion.identity;

        ArrangeCards(animate);
    }

    /// <summary>
    /// 获取插入索引
    /// </summary>
    public int GetInsertIndex(Vector3 worldPosition)
    {
        if (cards.Count == 0) return 0;

        Vector3 localPos = transform.InverseTransformPoint(worldPosition);

        for (int i = 0; i < cards.Count; i++)
        {
            if (localPos.x < cards[i].transform.localPosition.x)
                return i;
        }

        return cards.Count;
    }

    /// <summary>
    /// 重新排列所有卡牌（居中排列）
    /// </summary>
    public void ArrangeCards(bool animate = true)
    {
        if (cards.Count == 0) return;

        // 获取容器宽度
        float containerWidth = maxWidth;
        if (rectTransform != null)
        {
            Canvas.ForceUpdateCanvases();
            float w = rectTransform.rect.width;
            if (w > 50) containerWidth = w;
        }

        // 计算间距
        float actualSpacing = cardSpacing;
        if (autoAdjustSpacing && cards.Count > 1)
        {
            float needed = (cards.Count - 1) * cardSpacing;
            float available = containerWidth * 0.85f;
            if (needed > available)
            {
                actualSpacing = available / (cards.Count - 1);
                actualSpacing = Mathf.Max(actualSpacing, minSpacing);
            }
        }

        // 居中计算
        float totalWidth = (cards.Count - 1) * actualSpacing;
        float startX = -totalWidth / 2f;

        // 排列
        for (int i = 0; i < cards.Count; i++)
        {
            Vector3 targetPos = new Vector3(startX + i * actualSpacing, 0, 0);

            // ★ 关键：设置排列位置
            cards[i].SetArrangedPosition(targetPos);

            // ★ 使用 SiblingIndex 控制层级，不用 Canvas
            cards[i].SetBaseSortOrder(i);

            if (animate && !cards[i].IsHovering)
            {
                cards[i].transform.DOKill();
                cards[i].transform.DOLocalMove(targetPos, arrangeDuration).SetEase(arrangeEase);
            }
            else if (!cards[i].IsHovering)
            {
                cards[i].transform.localPosition = targetPos;
            }
        }

        Debug.Log($"[CardContainer] 排列 {cards.Count} 张卡牌，间距={actualSpacing:F0}");
    }

    public void Clear() => cards.Clear();

    public void ClearAndDestroy()
    {
        foreach (var card in cards)
            if (card != null) Destroy(card.gameObject);
        cards.Clear();
    }

    public bool Contains(CardUI card) => cards.Contains(card);

    public void RefreshLayout() => ArrangeCards(false);
}