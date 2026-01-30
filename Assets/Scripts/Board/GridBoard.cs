using UnityEngine;

/// <summary>
/// 棋盘系统 - 生成和管理游戏棋盘
/// </summary>
public class GridBoard : MonoBehaviour
{
    [Header("棋盘设置")]
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 6;
    [SerializeField] private float cellSize = 1f;

    [Header("视觉设置")]
    [SerializeField] private Color lightColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color darkColor = new Color(0.7f, 0.7f, 0.7f);

    // 棋盘原点（左下角）的世界坐标
    private Vector3 originPosition;

    // 单例，方便其他脚本访问
    public static GridBoard Instance { get; private set; }

    // 公开属性
    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 计算原点位置，使棋盘居中
        originPosition = transform.position - new Vector3(width * cellSize / 2f, height * cellSize / 2f, 0);
    }

    [Header("美术资源")]
    [Tooltip("格子预制体，留空则使用代码生成")]
    [SerializeField] private GameObject cellPrefab;
    [Tooltip("如果没有预制体，可以指定格子精灵")]
    [SerializeField] private Sprite cellSprite;
    [Tooltip("是否使用棋盘格交替颜色")]
    [SerializeField] private bool useAlternatingColors = true;

    private void Start()
    {
        GenerateBoard();
    }

    /// <summary>
    /// 生成棋盘视觉效果
    /// </summary>
    private void GenerateBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateCell(x, y);
            }
        }
    }

    /// <summary>
    /// 创建单个格子
    /// </summary>
    private void CreateCell(int x, int y)
    {
        GameObject cell;

        // 如果有预制体，使用预制体
        if (cellPrefab != null)
        {
            cell = Instantiate(cellPrefab, transform);
            cell.name = $"Cell_{x}_{y}";
            cell.transform.position = GetCellCenter(x, y);

            // 如果需要交替颜色，修改SpriteRenderer颜色
            if (useAlternatingColors)
            {
                SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = (x + y) % 2 == 0 ? lightColor : darkColor;
                }
            }
        }
        else
        {
            // 没有预制体，用代码生成
            cell = new GameObject($"Cell_{x}_{y}");
            cell.transform.SetParent(transform);
            cell.transform.position = GetCellCenter(x, y);

            SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
            sr.sprite = cellSprite != null ? cellSprite : CreateSquareSprite();
            sr.color = (x + y) % 2 == 0 ? lightColor : darkColor;
            sr.sortingOrder = -1;

            // 设置大小
            cell.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);
        }
    }

    /// <summary>
    /// 创建一个白色方形精灵（后备方案）
    /// </summary>
    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    /// <summary>
    /// 网格坐标转世界坐标（返回格子左下角位置）
    /// </summary>
    public Vector3 GetWorldPosition(int x, int y)
    {
        return originPosition + new Vector3(x * cellSize, y * cellSize, 0);
    }

    /// <summary>
    /// 网格坐标转世界坐标（返回格子中心位置）
    /// </summary>
    public Vector3 GetCellCenter(int x, int y)
    {
        return GetWorldPosition(x, y) + new Vector3(cellSize / 2f, cellSize / 2f, 0);
    }

    /// <summary>
    /// 网格坐标转本地坐标（返回格子中心位置，相对于GridBoard）
    /// 用于LineRenderer等需要本地坐标的情况
    /// </summary>
    public Vector3 GetCellCenterLocal(int x, int y)
    {
        // 本地坐标：从棋盘中心开始计算
        float localX = (x - width / 2f + 0.5f) * cellSize;
        float localY = (y - height / 2f + 0.5f) * cellSize;
        return new Vector3(localX, localY, 0);
    }

    /// <summary>
    /// 世界坐标转网格坐标
    /// </summary>
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - originPosition.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - originPosition.y) / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 检查坐标是否在棋盘范围内
    /// </summary>
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    /// <summary>
    /// 检查坐标是否在棋盘范围内
    /// </summary>
    public bool IsValidPosition(Vector2Int pos)
    {
        return IsValidPosition(pos.x, pos.y);
    }

    /// <summary>
    /// 在编辑器中绘制棋盘边界（方便调试）
    /// </summary>
    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position - new Vector3(width * cellSize / 2f, height * cellSize / 2f, 0);

        Gizmos.color = Color.green;

        // 绘制边界
        Vector3 bottomLeft = origin;
        Vector3 bottomRight = origin + new Vector3(width * cellSize, 0, 0);
        Vector3 topLeft = origin + new Vector3(0, height * cellSize, 0);
        Vector3 topRight = origin + new Vector3(width * cellSize, height * cellSize, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }
}