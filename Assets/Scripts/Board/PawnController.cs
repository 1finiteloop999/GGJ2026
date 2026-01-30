using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 移动方向枚举（用于移动逻辑）
/// </summary>
public enum Direction
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}

/// <summary>
/// 移动指令结构
/// </summary>
[System.Serializable]
public struct MoveCommand
{
    public Direction direction;
    public int steps;

    public MoveCommand(Direction dir, int step)
    {
        direction = dir;
        steps = step;
    }

    public override string ToString()
    {
        string dirName = direction switch
        {
            Direction.Up => "上",
            Direction.Down => "下",
            Direction.Left => "左",
            Direction.Right => "右",
            _ => "?"
        };
        return $"{dirName} {steps}步";
    }
}

/// <summary>
/// 棋子控制器 - 控制棋子在棋盘上的移动、动作和表情
/// </summary>
public class PawnController : MonoBehaviour
{
    [Header("===== 视觉数据 =====")]
    [Tooltip("角色的所有图标数据（拖入对应的PawnVisualData）")]
    [SerializeField] private PawnVisualData visualData;

    [Header("===== 后备设置（如果没有VisualData）=====")]
    [Tooltip("默认精灵")]
    [SerializeField] private Sprite defaultSprite;
    [Tooltip("棋子颜色")]
    [SerializeField] private Color pawnColor = Color.white;

    [Header("===== 移动设置 =====")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("棋子相对于格子的缩放比例")]
    [SerializeField] private float scaleRatio = 0.8f;

    [Header("===== 动作/表情设置 =====")]
    [Tooltip("动作/表情显示时长（秒）")]
    [SerializeField] private float actionDuration = 1f;
    [Tooltip("停顿时长（秒）")]
    [SerializeField] private float pauseDuration = 0.5f;

    [Header("===== 当前状态 =====")]
    [SerializeField] private Vector2Int gridPosition;

    // 组件引用
    private SpriteRenderer spriteRenderer;

    // 状态
    public bool IsMoving { get; private set; }
    public bool IsPerformingAction { get; private set; }
    public Vector2Int GridPosition => gridPosition;
    public DirectionType CurrentDirection { get; private set; } = DirectionType.Down;

    private void Awake()
    {
        SetupVisual();
    }

    /// <summary>
    /// 设置棋子视觉效果
    /// </summary>
    private void SetupVisual()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 设置初始精灵
        if (visualData != null && visualData.spriteIdle != null)
        {
            spriteRenderer.sprite = visualData.spriteIdle;
        }
        else if (defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }
        else
        {
            spriteRenderer.sprite = CreateDefaultSprite();
            spriteRenderer.color = pawnColor;
        }

        spriteRenderer.sortingOrder = 1;
        UpdateScale();
    }

    /// <summary>
    /// 创建默认圆形精灵
    /// </summary>
    private Sprite CreateDefaultSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                colors[y * size + x] = distance < radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>
    /// 更新棋子缩放
    /// </summary>
    private void UpdateScale()
    {
        float cellSize = GridBoard.Instance != null ? GridBoard.Instance.CellSize : 1f;
        transform.localScale = Vector3.one * cellSize * scaleRatio;
    }

    /// <summary>
    /// 初始化棋子
    /// </summary>
    public void Initialize(Vector2Int startPosition, Color color)
    {
        gridPosition = startPosition;
        pawnColor = color;

        // 如果没有视觉数据，使用颜色
        if (visualData == null && defaultSprite == null && spriteRenderer != null)
        {
            spriteRenderer.color = pawnColor;
        }

        EnsureParentedToBoard();
        SetPositionLocal(gridPosition);
        UpdateScale();

        // 重置到默认图标
        ResetToIdle();
    }

    /// <summary>
    /// 确保棋子是GridBoard的子物体
    /// </summary>
    private void EnsureParentedToBoard()
    {
        if (GridBoard.Instance != null && transform.parent != GridBoard.Instance.transform)
        {
            transform.SetParent(GridBoard.Instance.transform);
        }
    }

    /// <summary>
    /// 设置位置（无动画）
    /// </summary>
    public void SetPosition(Vector2Int newPosition)
    {
        if (GridBoard.Instance != null && GridBoard.Instance.IsValidPosition(newPosition))
        {
            gridPosition = newPosition;
            SetPositionLocal(gridPosition);
        }
    }

    /// <summary>
    /// 使用本地坐标设置位置
    /// </summary>
    private void SetPositionLocal(Vector2Int pos)
    {
        if (GridBoard.Instance != null)
        {
            transform.localPosition = GridBoard.Instance.GetCellCenterLocal(pos.x, pos.y);
        }
    }

    /// <summary>
    /// 重置到默认图标
    /// </summary>
    public void ResetToIdle()
    {
        if (spriteRenderer == null) return;

        if (visualData != null && visualData.spriteIdle != null)
        {
            spriteRenderer.sprite = visualData.spriteIdle;
        }
        else if (defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }

    #region 卡牌执行

    /// <summary>
    /// 执行一系列卡牌
    /// </summary>
    public IEnumerator ExecuteCards(List<CardData> cards)
    {
        DirectionType currentMoveDirection = DirectionType.None;

        foreach (var card in cards)
        {
            if (card == null) continue;

            Debug.Log($"[Pawn] 执行卡牌: {card.GetDescription()}");

            // 1. 停顿卡
            if (card.IsPauseCard)
            {
                yield return StartCoroutine(PerformPause());
                continue;
            }

            // 2. 动作卡
            if (card.IsActionCard)
            {
                yield return StartCoroutine(PerformAction(card.actionType));
                continue;
            }

            // 3. 表情卡
            if (card.IsExpressionCard)
            {
                yield return StartCoroutine(PerformExpression(card.expressionType));
                continue;
            }

            // 4. 方向卡（只改变方向，不移动）
            if (card.IsDirectionCard)
            {
                currentMoveDirection = card.direction;
                UpdateDirectionVisual(card.direction);
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // 5. 步数卡（使用当前方向移动）
            if (card.IsStepCard)
            {
                if (currentMoveDirection != DirectionType.None)
                {
                    yield return StartCoroutine(MoveInDirection(currentMoveDirection, card.stepCount));
                }
                continue;
            }

            // 6. 方向+步数组合卡
            if (card.HasMovement && card.direction != DirectionType.None && card.stepCount > 0)
            {
                currentMoveDirection = card.direction;
                yield return StartCoroutine(MoveInDirection(card.direction, card.stepCount));
                continue;
            }

            // 如果有方向但没步数，只更新方向
            if (card.direction != DirectionType.None)
            {
                currentMoveDirection = card.direction;
                UpdateDirectionVisual(card.direction);
            }

            // 如果有步数但没方向，使用当前方向
            if (card.stepCount > 0 && currentMoveDirection != DirectionType.None)
            {
                yield return StartCoroutine(MoveInDirection(currentMoveDirection, card.stepCount));
            }

            yield return new WaitForSeconds(0.1f);
        }

        // 执行完毕，重置图标
        ResetToIdle();
    }

    /// <summary>
    /// 执行移动指令（兼容旧接口）
    /// </summary>
    public IEnumerator ExecuteCommands(MoveCommand[] commands)
    {
        foreach (var command in commands)
        {
            DirectionType dirType = command.direction switch
            {
                Direction.Up => DirectionType.Up,
                Direction.Down => DirectionType.Down,
                Direction.Left => DirectionType.Left,
                Direction.Right => DirectionType.Right,
                _ => DirectionType.None
            };

            yield return StartCoroutine(MoveInDirection(dirType, command.steps));
            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    #region 移动

    /// <summary>
    /// 向指定方向移动
    /// </summary>
    private IEnumerator MoveInDirection(DirectionType direction, int steps)
    {
        if (direction == DirectionType.None || steps <= 0) yield break;

        IsMoving = true;
        CurrentDirection = direction;
        UpdateDirectionVisual(direction);

        Vector2Int dirVector = GetDirectionVector(direction);

        for (int i = 0; i < steps; i++)
        {
            Vector2Int targetPos = gridPosition + dirVector;

            if (!GridBoard.Instance.IsValidPosition(targetPos))
            {
                Debug.Log($"[Pawn] 移动被阻挡：超出边界 {targetPos}");
                break;
            }

            yield return StartCoroutine(MoveOneStep(targetPos));
        }

        IsMoving = false;
    }

    /// <summary>
    /// 执行一步移动的动画
    /// </summary>
    private IEnumerator MoveOneStep(Vector2Int targetPos)
    {
        Vector3 startLocalPos = transform.localPosition;
        Vector3 targetLocalPos = GridBoard.Instance.GetCellCenterLocal(targetPos.x, targetPos.y);

        float elapsed = 0f;
        float duration = 1f / moveSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); // smoothstep

            transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            yield return null;
        }

        gridPosition = targetPos;
        transform.localPosition = targetLocalPos;
    }

    /// <summary>
    /// 更新方向视觉效果
    /// </summary>
    private void UpdateDirectionVisual(DirectionType direction)
    {
        if (spriteRenderer == null || visualData == null) return;

        Sprite dirSprite = visualData.GetDirectionSprite(direction);
        if (dirSprite != null)
        {
            spriteRenderer.sprite = dirSprite;
        }
    }

    /// <summary>
    /// 获取方向向量
    /// </summary>
    private Vector2Int GetDirectionVector(DirectionType direction)
    {
        return direction switch
        {
            DirectionType.Up => Vector2Int.up,
            DirectionType.Down => Vector2Int.down,
            DirectionType.Left => Vector2Int.left,
            DirectionType.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };
    }

    #endregion

    #region 动作和表情

    /// <summary>
    /// 执行停顿
    /// </summary>
    private IEnumerator PerformPause()
    {
        Debug.Log("[Pawn] 执行停顿");
        yield return new WaitForSeconds(pauseDuration);
    }

    /// <summary>
    /// 执行动作
    /// </summary>
    private IEnumerator PerformAction(ActionType action)
    {
        if (action == ActionType.None) yield break;

        IsPerformingAction = true;
        Debug.Log($"[Pawn] 执行动作: {action}");

        // 切换到动作图标
        if (visualData != null && spriteRenderer != null)
        {
            Sprite actionSprite = visualData.GetActionSprite(action);
            if (actionSprite != null)
            {
                spriteRenderer.sprite = actionSprite;
            }
        }

        // 等待动作时长
        yield return new WaitForSeconds(actionDuration);

        // 恢复默认图标
        ResetToIdle();

        IsPerformingAction = false;
    }

    /// <summary>
    /// 执行表情
    /// </summary>
    private IEnumerator PerformExpression(ExpressionType expression)
    {
        if (expression == ExpressionType.None) yield break;

        IsPerformingAction = true;
        Debug.Log($"[Pawn] 执行表情: {expression}");

        // 切换到表情图标
        if (visualData != null && spriteRenderer != null)
        {
            Sprite expressionSprite = visualData.GetExpressionSprite(expression);
            if (expressionSprite != null)
            {
                spriteRenderer.sprite = expressionSprite;
            }
        }

        // 等待表情时长
        yield return new WaitForSeconds(actionDuration);

        // 恢复默认图标
        ResetToIdle();

        IsPerformingAction = false;
    }

    #endregion

    /// <summary>
    /// 简单移动（兼容旧接口）
    /// </summary>
    public void Move(Direction direction, int steps)
    {
        if (IsMoving) return;

        DirectionType dirType = direction switch
        {
            Direction.Up => DirectionType.Up,
            Direction.Down => DirectionType.Down,
            Direction.Left => DirectionType.Left,
            Direction.Right => DirectionType.Right,
            _ => DirectionType.None
        };

        StartCoroutine(MoveInDirection(dirType, steps));
    }
}