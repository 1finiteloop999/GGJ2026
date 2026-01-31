using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 卡槽管理器 - 管理所有卡槽和生成移动指令
/// </summary>
public class SlotManager : MonoBehaviour
{
    public static SlotManager Instance { get; private set; }

    [Header("卡槽设置")]
    [Tooltip("卡槽容器（卡槽的父物体，用于自动查找）")]
    [SerializeField] private Transform slotContainer;

    [Header("卡槽列表")]
    [Tooltip("场景中的卡槽（可手动拖入或自动查找）")]
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
        // 如果没有手动设置卡槽，自动查找
        if (slots == null || slots.Count == 0)
        {
            FindSlotsInScene();
        }

        SetupPathPreview();
    }

    /// <summary>
    /// 初始化卡槽（关卡开始时调用）
    /// </summary>
    public void Initialize()
    {
        // 确保卡槽列表已填充
        if (slots == null || slots.Count == 0)
        {
            FindSlotsInScene();
        }

        // 清空卡槽内容（但不销毁卡槽）
        ClearAllSlotContents();

        Debug.Log($"[SlotManager] 初始化完成，共 {slots.Count} 个卡槽");
    }

    /// <summary>
    /// 自动查找场景中的卡槽
    /// </summary>
    public void FindSlotsInScene()
    {
        slots.Clear();

        // 先尝试从容器查找
        if (slotContainer != null)
        {
            slots = slotContainer.GetComponentsInChildren<CardSlot>(true).ToList();
        }

        // 如果没有容器或容器中没有，从整个场景查找
        if (slots.Count == 0)
        {
            slots = Object.FindObjectsByType<CardSlot>(FindObjectsSortMode.None).ToList();
        }

        // 移除空引用
        slots.RemoveAll(s => s == null);

        // 按SlotIndex排序
        slots = slots.OrderBy(s => s.SlotIndex).ToList();

        Debug.Log($"[SlotManager] 找到 {slots.Count} 个卡槽");
        foreach (var slot in slots)
        {
            Debug.Log($"  - 卡槽 {slot.SlotIndex}: {slot.name} ({slot.Type})");
        }
    }

    /// <summary>
    /// 手动注册卡槽
    /// </summary>
    public void RegisterSlots(List<CardSlot> existingSlots)
    {
        slots.Clear();
        if (existingSlots != null)
        {
            slots.AddRange(existingSlots);
            slots.RemoveAll(s => s == null);
            slots = slots.OrderBy(s => s.SlotIndex).ToList();
        }
        Debug.Log($"[SlotManager] 注册了 {slots.Count} 个卡槽");
    }

    /// <summary>
    /// 手动刷新卡槽列表（编辑器中使用）
    /// </summary>
    public void RefreshSlots()
    {
        FindSlotsInScene();
    }

    [Header("NPC路径预览")]
    [SerializeField] private LineRenderer npcPathPreview;
    [SerializeField] private Color npcPreviewColor = new Color(1f, 0.5f, 0.5f, 0.6f); // 红色半透明

    /// <summary>
    /// 设置路径预览LineRenderer
    /// </summary>
    private void SetupPathPreview()
    {
        Transform parent = null;
        if (GridBoard.Instance != null)
        {
            parent = GridBoard.Instance.transform;
        }

        // 创建NPC路径预览（在下层）
        if (npcPathPreview == null)
        {
            GameObject npcPreviewObj = new GameObject("NPCPathPreview");
            if (parent != null)
            {
                npcPreviewObj.transform.SetParent(parent);
                npcPreviewObj.transform.localPosition = Vector3.zero;
                npcPreviewObj.transform.localScale = Vector3.one;
            }

            npcPathPreview = npcPreviewObj.AddComponent<LineRenderer>();
            npcPathPreview.startWidth = 0.15f;
            npcPathPreview.endWidth = 0.15f;
            npcPathPreview.material = new Material(Shader.Find("Sprites/Default"));
            npcPathPreview.startColor = npcPreviewColor;
            npcPathPreview.endColor = npcPreviewColor;
            npcPathPreview.sortingOrder = 4; // NPC路径在下层
            npcPathPreview.positionCount = 0;
            npcPathPreview.useWorldSpace = false;
        }

        // 创建玩家路径预览（在上层）
        if (pathPreview == null)
        {
            GameObject previewObj = new GameObject("PlayerPathPreview");
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
        pathPreview.sortingOrder = 5; // 玩家路径在上层
        pathPreview.positionCount = 0;

        // 使用本地坐标，这样会随父物体一起移动和缩放
        pathPreview.useWorldSpace = false;
    }

    /// <summary>
    /// 显示NPC路径预览
    /// </summary>
    public void ShowNPCPathPreview()
    {
        if (npcPathPreview == null || GridBoard.Instance == null) return;

        LevelData levelData = GameManager.Instance?.GetCurrentLevel();
        if (levelData == null) return;

        // 生成NPC移动指令
        List<MoveCommand> commands = levelData.GetNPCCommands();

        if (commands.Count == 0)
        {
            npcPathPreview.positionCount = 0;
            return;
        }

        // 获取NPC起始位置
        Vector2Int currentPos = levelData.npcStartPosition;

        List<Vector3> positions = new List<Vector3>();
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

        npcPathPreview.positionCount = positions.Count;
        npcPathPreview.SetPositions(positions.ToArray());

        Debug.Log($"[SlotManager] 显示NPC路径预览，共 {positions.Count} 个点");
    }

    /// <summary>
    /// 隐藏NPC路径预览
    /// </summary>
    public void HideNPCPathPreview()
    {
        if (npcPathPreview != null)
        {
            npcPathPreview.positionCount = 0;
        }
    }

    /// <summary>
    /// 获取NPC路径终点位置
    /// </summary>
    public Vector2Int GetNPCEndPosition()
    {
        LevelData levelData = GameManager.Instance?.GetCurrentLevel();
        if (levelData == null) return Vector2Int.zero;

        Vector2Int currentPos = levelData.npcStartPosition;
        List<MoveCommand> commands = levelData.GetNPCCommands();

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
                if (GridBoard.Instance != null && GridBoard.Instance.IsValidPosition(nextPos))
                {
                    currentPos = nextPos;
                }
                else
                {
                    break;
                }
            }
        }

        return currentPos;
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
    /// 获取当前所有卡槽中的卡牌（按编号顺序）
    /// </summary>
    public List<CardData> GetSlotCards()
    {
        List<CardData> cards = new List<CardData>();

        // 确保按编号顺序
        var orderedSlots = slots.OrderBy(s => s.SlotIndex).ToList();

        foreach (var slot in orderedSlots)
        {
            if (slot != null && !slot.IsEmpty && slot.CurrentCard != null && slot.CurrentCard.CardData != null)
            {
                cards.Add(slot.CurrentCard.CardData);
                Debug.Log($"[SlotManager] 卡槽{slot.SlotIndex}({slot.Type}): {slot.CurrentCard.CardData.GetDescription()}");
            }
        }

        Debug.Log($"[SlotManager] 按编号顺序获取 {cards.Count} 张卡牌");
        return cards;
    }

    /// <summary>
    /// 获取所有卡槽（按编号顺序）
    /// </summary>
    public List<CardSlot> GetOrderedSlots()
    {
        return slots.OrderBy(s => s.SlotIndex).ToList();
    }

    /// <summary>
    /// 解析卡槽生成移动指令列表（用于路径预览）
    /// </summary>
    public List<MoveCommand> GenerateCommands()
    {
        List<MoveCommand> commands = new List<MoveCommand>();
        List<CardData> cards = GetSlotCards();

        Debug.Log($"[SlotManager] 开始解析 {cards.Count} 张卡牌生成移动指令");

        DirectionType currentDirection = DirectionType.None;

        foreach (var card in cards)
        {
            // 跳过停顿、动作、表情卡（不产生移动）
            if (card.IsPauseCard || card.IsActionCard || card.IsExpressionCard)
            {
                continue;
            }

            // 方向卡（只改变方向）
            if (card.IsDirectionCard)
            {
                // 如果之前有方向但没步数，不生成指令（因为方向卡只改变方向）
                currentDirection = card.direction;
                Debug.Log($"[SlotManager] 记录方向: {currentDirection}");
                continue;
            }

            // 步数卡（使用当前方向移动）
            if (card.IsStepCard)
            {
                if (currentDirection != DirectionType.None)
                {
                    Direction dir = ConvertDirection(currentDirection);
                    commands.Add(new MoveCommand(dir, card.stepCount));
                    Debug.Log($"[SlotManager] 生成指令: {dir} {card.stepCount}步");
                }
                else
                {
                    Debug.Log($"[SlotManager] 步数牌 {card.stepCount} 被忽略（没有方向）");
                }
                continue;
            }

            // 方向+步数组合卡
            if (card.direction != DirectionType.None && card.stepCount > 0)
            {
                currentDirection = card.direction;
                Direction dir = ConvertDirection(card.direction);
                commands.Add(new MoveCommand(dir, card.stepCount));
                Debug.Log($"[SlotManager] 生成指令: {dir} {card.stepCount}步 (组合卡)");
                continue;
            }

            // 只有方向没有步数
            if (card.direction != DirectionType.None)
            {
                currentDirection = card.direction;
            }

            // 只有步数没有方向
            if (card.stepCount > 0 && currentDirection != DirectionType.None)
            {
                Direction dir = ConvertDirection(currentDirection);
                commands.Add(new MoveCommand(dir, card.stepCount));
                Debug.Log($"[SlotManager] 生成指令: {dir} {card.stepCount}步");
            }
        }

        Debug.Log($"[SlotManager] 共生成 {commands.Count} 条移动指令");
        return commands;
    }

    /// <summary>
    /// 转换方向类型
    /// </summary>
    private Direction ConvertDirection(DirectionType dirType)
    {
        return dirType switch
        {
            DirectionType.Up => Direction.Up,
            DirectionType.Down => Direction.Down,
            DirectionType.Left => Direction.Left,
            DirectionType.Right => Direction.Right,
            _ => Direction.Down
        };
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
    /// 清空所有卡槽的内容（不销毁卡槽本身）
    /// </summary>
    public void ClearAllSlots()
    {
        ClearAllSlotContents();
    }

    /// <summary>
    /// 清空所有卡槽的内容（不销毁卡槽本身）
    /// </summary>
    public void ClearAllSlotContents()
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.Clear();
            }
        }

        HidePathPreview();
        HideNPCPathPreview();
    }

    /// <summary>
    /// 隐藏玩家路径预览
    /// </summary>
    public void HidePathPreview()
    {
        if (pathPreview != null)
        {
            pathPreview.positionCount = 0;
        }
    }

    /// <summary>
    /// 隐藏所有路径预览（玩家和NPC）
    /// </summary>
    public void HideAllPathPreviews()
    {
        HidePathPreview();
        HideNPCPathPreview();
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