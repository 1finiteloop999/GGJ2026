using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 卡槽管理器 - 管理所有卡槽和生成移动指令
/// </summary>
public class SlotManager : MonoBehaviour
{
    public static SlotManager Instance { get; private set; }

    [Header("卡槽列表")]
    [SerializeField] private List<CardSlot> slots = new List<CardSlot>();

    [Header("路径预览")]
    [SerializeField] private LineRenderer pathPreview;
    [SerializeField] private Color previewColor = new Color(1f, 1f, 0f, 0.8f);

    // 事件：当卡槽内容改变时
    public System.Action OnSlotsChanged;

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

    private void Start()
    {
        InitializeSlots();
        SetupPathPreview();
    }

    /// <summary>
    /// 初始化卡槽列表
    /// </summary>
    private void InitializeSlots()
    {
        // 如果没有手动设置，自动查找
        if (slots == null || slots.Count == 0)
        {
            // 先尝试从子物体查找
            slots = GetComponentsInChildren<CardSlot>(true).ToList();

            // 如果还是没有，尝试从整个场景查找
            if (slots.Count == 0)
            {
                slots = FindObjectsOfType<CardSlot>(true).ToList();
            }
        }

        // 移除空引用
        slots.RemoveAll(s => s == null);

        // 按SlotIndex排序确保顺序正确
        slots = slots.OrderBy(s => s.SlotIndex).ToList();

        Debug.Log($"[SlotManager] 初始化完成，共 {slots.Count} 个卡槽");

        if (slots.Count == 0)
        {
            Debug.LogError("[SlotManager] 警告：没有找到任何卡槽！请检查：\n" +
                "1. 卡槽对象是否添加了CardSlot脚本\n" +
                "2. 卡槽是否是SlotManager的子物体，或手动拖入Slots列表");
        }
        else
        {
            foreach (var slot in slots)
            {
                Debug.Log($"[SlotManager] 卡槽 {slot.SlotIndex}: {slot.name}");
            }
        }
    }

    /// <summary>
    /// 手动刷新卡槽列表（编辑器中使用）
    /// </summary>
    public void RefreshSlots()
    {
        InitializeSlots();
    }

    /// <summary>
    /// 设置路径预览LineRenderer
    /// </summary>
    private void SetupPathPreview()
    {
        if (pathPreview == null)
        {
            // 尝试将PathPreview创建为GridBoard的子物体
            Transform parent = null;
            if (GridBoard.Instance != null)
            {
                parent = GridBoard.Instance.transform;
            }

            GameObject previewObj = new GameObject("PathPreview");
            if (parent != null)
            {
                previewObj.transform.SetParent(parent);
                previewObj.transform.localPosition = Vector3.zero;
                previewObj.transform.localScale = Vector3.one;
            }

            pathPreview = previewObj.AddComponent<LineRenderer>();
        }

        pathPreview.startWidth = 0.1f;
        pathPreview.endWidth = 0.1f;
        pathPreview.material = new Material(Shader.Find("Sprites/Default"));
        pathPreview.startColor = previewColor;
        pathPreview.endColor = previewColor;
        pathPreview.sortingOrder = 5;
        pathPreview.positionCount = 0;

        // 使用本地坐标，这样会随父物体一起移动和缩放
        pathPreview.useWorldSpace = false;
    }

    /// <summary>
    /// 当卡槽内容改变时调用
    /// </summary>
    public void OnSlotChanged()
    {
        Debug.Log("[SlotManager] 卡槽内容改变，更新路径预览");
        UpdatePathPreview();
        OnSlotsChanged?.Invoke();
    }

    /// <summary>
    /// 获取当前所有卡槽中的卡牌（按顺序）
    /// </summary>
    public List<CardData> GetSlotCards()
    {
        List<CardData> cards = new List<CardData>();

        foreach (var slot in slots)
        {
            if (slot != null && !slot.IsEmpty && slot.CurrentCard != null && slot.CurrentCard.CardData != null)
            {
                cards.Add(slot.CurrentCard.CardData);
                Debug.Log($"[SlotManager] 卡槽{slot.SlotIndex}: {slot.CurrentCard.CardData.cardName} (类型:{slot.CurrentCard.CardData.cardType})");
            }
        }

        Debug.Log($"[SlotManager] 共获取 {cards.Count} 张卡牌");
        return cards;
    }

    /// <summary>
    /// 解析卡槽生成移动指令列表
    /// 规则：方向牌+步数牌 = 一个完整指令
    /// </summary>
    public List<MoveCommand> GenerateCommands()
    {
        List<MoveCommand> commands = new List<MoveCommand>();
        List<CardData> cards = GetSlotCards();

        Debug.Log($"[SlotManager] 开始解析 {cards.Count} 张卡牌生成指令");

        Direction? currentDirection = null;

        foreach (var card in cards)
        {
            if (card.cardType == CardType.Direction)
            {
                // 如果之前有方向但没步数，默认1步
                if (currentDirection.HasValue)
                {
                    commands.Add(new MoveCommand(currentDirection.Value, 1));
                    Debug.Log($"[SlotManager] 生成指令: {currentDirection.Value} 1步 (默认)");
                }
                currentDirection = card.direction;
                Debug.Log($"[SlotManager] 记录方向: {card.direction}");
            }
            else if (card.cardType == CardType.Step)
            {
                if (currentDirection.HasValue)
                {
                    commands.Add(new MoveCommand(currentDirection.Value, card.stepCount));
                    Debug.Log($"[SlotManager] 生成指令: {currentDirection.Value} {card.stepCount}步");
                    currentDirection = null;
                }
                else
                {
                    Debug.Log($"[SlotManager] 步数牌 {card.stepCount} 被忽略（没有方向）");
                }
            }
        }

        // 处理末尾只有方向没有步数的情况
        if (currentDirection.HasValue)
        {
            commands.Add(new MoveCommand(currentDirection.Value, 1));
            Debug.Log($"[SlotManager] 生成指令: {currentDirection.Value} 1步 (末尾默认)");
        }

        Debug.Log($"[SlotManager] 共生成 {commands.Count} 条移动指令");
        return commands;
    }

    /// <summary>
    /// 更新路径预览
    /// </summary>
    public void UpdatePathPreview()
    {
        if (pathPreview == null || GridBoard.Instance == null) return;

        List<MoveCommand> commands = GenerateCommands();

        if (commands.Count == 0)
        {
            pathPreview.positionCount = 0;
            return;
        }

        // 获取玩家起始位置
        Vector2Int currentPos = GameManager.Instance?.PlayerStartPosition ?? new Vector2Int(0, 0);

        List<Vector3> positions = new List<Vector3>();

        // 使用GridBoard的本地坐标方法
        positions.Add(GridBoard.Instance.GetCellCenterLocal(currentPos.x, currentPos.y));

        foreach (var cmd in commands)
        {
            Vector2Int dir = cmd.direction switch
            {
                Direction.Up => Vector2Int.up,
                Direction.Down => Vector2Int.down,
                Direction.Left => Vector2Int.left,
                Direction.Right => Vector2Int.right,
                _ => Vector2Int.zero
            };

            for (int i = 0; i < cmd.steps; i++)
            {
                Vector2Int nextPos = currentPos + dir;

                // 检查边界
                if (GridBoard.Instance.IsValidPosition(nextPos))
                {
                    currentPos = nextPos;
                    positions.Add(GridBoard.Instance.GetCellCenterLocal(currentPos.x, currentPos.y));
                }
                else
                {
                    break;
                }
            }
        }

        pathPreview.positionCount = positions.Count;
        pathPreview.SetPositions(positions.ToArray());
    }

    /// <summary>
    /// 清空所有卡槽
    /// </summary>
    public void ClearAllSlots()
    {
        foreach (var slot in slots)
        {
            slot.Clear();
        }

        HidePathPreview();
    }

    /// <summary>
    /// 隐藏路径预览
    /// </summary>
    public void HidePathPreview()
    {
        if (pathPreview != null)
        {
            pathPreview.positionCount = 0;
        }
    }

    /// <summary>
    /// 显示路径预览
    /// </summary>
    public void ShowPathPreview()
    {
        UpdatePathPreview();
    }

    /// <summary>
    /// 获取卡槽数量
    /// </summary>
    public int GetSlotCount()
    {
        return slots.Count;
    }
}