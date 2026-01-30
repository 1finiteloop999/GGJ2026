using UnityEngine;
using System.Collections;

/// <summary>
/// 棋子控制器 - 控制棋子在棋盘上的移动
/// 支持自定义美术素材和Animator动画
/// </summary>
public class PawnController : MonoBehaviour
{
    [Header("美术资源")]
    [Tooltip("棋子精灵，留空则使用代码生成")]
    [SerializeField] private Sprite pawnSprite;
    [Tooltip("是否使用Animator控制动画")]
    [SerializeField] private bool useAnimator = false;

    [Header("方向精灵（可选 - 不用Animator时使用）")]
    [Tooltip("朝上时的精灵")]
    [SerializeField] private Sprite spriteUp;
    [Tooltip("朝下时的精灵")]
    [SerializeField] private Sprite spriteDown;
    [Tooltip("朝左时的精灵")]
    [SerializeField] private Sprite spriteLeft;
    [Tooltip("朝右时的精灵")]
    [SerializeField] private Sprite spriteRight;

    [Header("棋子设置")]
    [SerializeField] private Color pawnColor = Color.white;
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("棋子相对于格子的缩放比例")]
    [SerializeField] private float scaleRatio = 0.8f;

    [Header("当前状态")]
    [SerializeField] private Vector2Int gridPosition;

    // 组件引用
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // 是否正在移动
    public bool IsMoving { get; private set; }

    // 当前网格位置
    public Vector2Int GridPosition => gridPosition;

    // 当前朝向
    public Direction CurrentDirection { get; private set; } = Direction.Down;

    private void Awake()
    {
        SetupVisual();
    }

    /// <summary>
    /// 设置棋子视觉效果
    /// </summary>
    private void SetupVisual()
    {
        // 获取或创建SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 获取Animator（如果有）
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            useAnimator = true;
        }

        // 设置精灵（只有在没有Animator时才需要手动设置）
        if (!useAnimator)
        {
            if (pawnSprite != null)
            {
                spriteRenderer.sprite = pawnSprite;
            }
            else if (spriteDown != null)
            {
                spriteRenderer.sprite = spriteDown;
            }
            else
            {
                // 没有任何精灵，创建默认圆形
                spriteRenderer.sprite = CreateCircleSprite();
            }
        }

        // 只有在没有使用美术素材时才应用颜色
        if (pawnSprite == null && spriteDown == null && !useAnimator)
        {
            spriteRenderer.color = pawnColor;
        }

        spriteRenderer.sortingOrder = 1;

        // 设置大小
        UpdateScale();
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
    /// 创建默认圆形精灵（后备方案）
    /// </summary>
    private Sprite CreateCircleSprite()
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
    /// 初始化棋子位置和颜色
    /// </summary>
    public void Initialize(Vector2Int startPosition, Color color)
    {
        gridPosition = startPosition;
        pawnColor = color;

        // 只有在使用代码生成精灵时才应用颜色
        if (spriteRenderer != null && pawnSprite == null && spriteDown == null && !useAnimator)
        {
            spriteRenderer.color = pawnColor;
        }

        // 确保棋子是GridBoard的子物体
        EnsureParentedToBoard();

        // 使用本地坐标设置位置
        SetPositionLocal(gridPosition);

        UpdateScale();
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
    /// 设置位置（无动画）- 使用本地坐标
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
    /// 移动指定方向和步数
    /// </summary>
    public void Move(Direction direction, int steps)
    {
        if (IsMoving) return;
        StartCoroutine(MoveCoroutine(direction, steps));
    }

    /// <summary>
    /// 移动协程 - 执行带动画的移动
    /// </summary>
    private IEnumerator MoveCoroutine(Direction direction, int steps)
    {
        IsMoving = true;
        CurrentDirection = direction;

        // 更新朝向
        UpdateDirectionVisual(direction);

        // 触发移动动画
        SetAnimatorMoving(true);

        Vector2Int dirVector = GetDirectionVector(direction);

        for (int i = 0; i < steps; i++)
        {
            Vector2Int targetPos = gridPosition + dirVector;

            // 检查边界
            if (!GridBoard.Instance.IsValidPosition(targetPos))
            {
                Debug.Log($"移动被阻挡：超出边界 {targetPos}");
                break;
            }

            // 执行单步移动动画
            yield return StartCoroutine(MoveOneStep(targetPos));
        }

        // 停止移动动画
        SetAnimatorMoving(false);

        IsMoving = false;
    }

    /// <summary>
    /// 更新朝向视觉效果
    /// </summary>
    private void UpdateDirectionVisual(Direction direction)
    {
        // 方式1: 如果使用Animator，设置方向参数
        if (useAnimator && animator != null)
        {
            // Animator参数: Direction (int: 0=Up, 1=Down, 2=Left, 3=Right)
            animator.SetInteger("Direction", (int)direction);
            return;
        }

        // 方式2: 如果有方向精灵，切换精灵
        if (HasDirectionSprites())
        {
            Sprite targetSprite = direction switch
            {
                Direction.Up => spriteUp ?? spriteDown,
                Direction.Down => spriteDown ?? pawnSprite,
                Direction.Left => spriteLeft ?? spriteRight,
                Direction.Right => spriteRight ?? spriteLeft,
                _ => spriteDown ?? pawnSprite
            };

            if (targetSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = targetSprite;
                // 如果只有左右其中一个精灵，通过翻转实现另一个方向
                if (spriteLeft == null && spriteRight != null && direction == Direction.Left)
                {
                    spriteRenderer.flipX = true;
                }
                else if (spriteRight == null && spriteLeft != null && direction == Direction.Right)
                {
                    spriteRenderer.flipX = true;
                }
                else
                {
                    spriteRenderer.flipX = false;
                }
            }
            return;
        }

        // 方式3: 只有单一精灵，通过翻转来表示左右
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (direction == Direction.Left);
        }
    }

    /// <summary>
    /// 检查是否有方向精灵
    /// </summary>
    private bool HasDirectionSprites()
    {
        return spriteUp != null || spriteDown != null || spriteLeft != null || spriteRight != null;
    }

    /// <summary>
    /// 设置Animator的移动状态
    /// </summary>
    private void SetAnimatorMoving(bool isMoving)
    {
        if (useAnimator && animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }
    }

    /// <summary>
    /// 触发Animator动作（用于动作牌：鞠躬、跳跃等）
    /// </summary>
    public void TriggerAction(string actionName)
    {
        if (useAnimator && animator != null)
        {
            animator.SetTrigger(actionName);
        }
    }

    /// <summary>
    /// 设置表情（用于表情牌）
    /// </summary>
    public void SetExpression(string expressionName)
    {
        if (useAnimator && animator != null)
        {
            animator.SetTrigger(expressionName);
        }
    }

    /// <summary>
    /// 执行一步移动的动画 - 使用本地坐标
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

            // 使用平滑插值
            t = t * t * (3f - 2f * t); // smoothstep

            transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            yield return null;
        }

        // 确保最终位置精确
        gridPosition = targetPos;
        transform.localPosition = targetLocalPos;
    }

    /// <summary>
    /// 执行一系列移动指令
    /// </summary>
    public IEnumerator ExecuteCommands(MoveCommand[] commands)
    {
        foreach (var command in commands)
        {
            yield return StartCoroutine(MoveCoroutine(command.direction, command.steps));
            yield return new WaitForSeconds(0.1f); // 指令间的短暂停顿
        }
    }

    /// <summary>
    /// 获取方向对应的向量
    /// </summary>
    private Vector2Int GetDirectionVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return Vector2Int.up;
            case Direction.Down: return Vector2Int.down;
            case Direction.Left: return Vector2Int.left;
            case Direction.Right: return Vector2Int.right;
            default: return Vector2Int.zero;
        }
    }
}

/// <summary>
/// 移动方向枚举
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