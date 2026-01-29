using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 棋盘管理器 - 管理棋盘格子和角色移动
/// </summary>
public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }
    
    [Header("棋盘设置")]
    [SerializeField] private int boardWidth = 8;
    [SerializeField] private int boardHeight = 8;
    [SerializeField] private float cellSize = 60f;
    
    [Header("引用")]
    [SerializeField] private RectTransform boardContainer;
    [SerializeField] private RectTransform playerToken;       // 玩家棋子
    [SerializeField] private GameObject cellPrefab;           // 格子预制体
    [SerializeField] private LineRenderer pathRenderer;       // 路径线渲染器（可选）
    
    [Header("动画设置")]
    [SerializeField] private float moveStepDuration = 0.3f;   // 每格移动时间
    
    // 格子数组
    private GameObject[,] cells;
    
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
        // 初始化棋盘
        InitializeBoard();
        
        // 设置玩家初始位置
        if (playerToken != null)
        {
            SetPlayerPosition(Vector2Int.zero);
        }
    }
    
    #region 棋盘初始化
    
    /// <summary>
    /// 初始化棋盘格子
    /// </summary>
    private void InitializeBoard()
    {
        if (boardContainer == null || cellPrefab == null)
        {
            Debug.LogWarning("BoardManager: 缺少引用，跳过棋盘初始化");
            return;
        }
        
        cells = new GameObject[boardWidth, boardHeight];
        
        // 计算棋盘起始位置（居中）
        float startX = -(boardWidth - 1) * cellSize / 2f;
        float startY = -(boardHeight - 1) * cellSize / 2f;
        
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                GameObject cell = Instantiate(cellPrefab, boardContainer);
                RectTransform rt = cell.GetComponent<RectTransform>();
                
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(startX + x * cellSize, startY + y * cellSize);
                    rt.sizeDelta = new Vector2(cellSize - 2, cellSize - 2);  // 留点间隙
                }
                
                // 棋盘格交替颜色
                Image img = cell.GetComponent<Image>();
                if (img != null)
                {
                    bool isLight = (x + y) % 2 == 0;
                    img.color = isLight ? new Color(0.9f, 0.9f, 0.85f) : new Color(0.7f, 0.7f, 0.65f);
                }
                
                cells[x, y] = cell;
            }
        }
    }
    
    #endregion
    
    #region 位置转换
    
    /// <summary>
    /// 棋盘坐标转UI坐标
    /// </summary>
    public Vector2 BoardToUIPosition(Vector2Int boardPos)
    {
        float startX = -(boardWidth - 1) * cellSize / 2f;
        float startY = -(boardHeight - 1) * cellSize / 2f;
        
        return new Vector2(startX + boardPos.x * cellSize, startY + boardPos.y * cellSize);
    }
    
    /// <summary>
    /// 检查位置是否在棋盘内
    /// </summary>
    public bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < boardWidth && pos.y >= 0 && pos.y < boardHeight;
    }
    
    /// <summary>
    /// 限制位置在棋盘内
    /// </summary>
    public Vector2Int ClampPosition(Vector2Int pos)
    {
        return new Vector2Int(
            Mathf.Clamp(pos.x, 0, boardWidth - 1),
            Mathf.Clamp(pos.y, 0, boardHeight - 1)
        );
    }
    
    #endregion
    
    #region 玩家移动
    
    /// <summary>
    /// 设置玩家位置（无动画）
    /// </summary>
    public void SetPlayerPosition(Vector2Int pos)
    {
        if (playerToken == null) return;
        
        pos = ClampPosition(pos);
        playerToken.anchoredPosition = BoardToUIPosition(pos);
    }
    
    /// <summary>
    /// 播放移动动画
    /// </summary>
    public IEnumerator PlayMoveAnimation(Vector2Int from, Vector2Int to)
    {
        if (playerToken == null)
        {
            Debug.LogWarning("BoardManager: 没有玩家棋子");
            yield break;
        }
        
        // 限制目标位置在棋盘内
        to = ClampPosition(to);
        
        // 计算移动路径（逐格移动）
        Vector2Int current = from;
        
        while (current != to)
        {
            // 计算下一步
            Vector2Int direction = new Vector2Int(
                System.Math.Sign(to.x - current.x),
                System.Math.Sign(to.y - current.y)
            );
            
            // 先横向移动，再纵向移动（或者可以改成对角线移动）
            if (direction.x != 0)
            {
                current.x += direction.x;
            }
            else if (direction.y != 0)
            {
                current.y += direction.y;
            }
            
            // 移动到下一格
            Vector2 targetPos = BoardToUIPosition(current);
            
            playerToken.DOAnchorPos(targetPos, moveStepDuration).SetEase(Ease.Linear);
            
            yield return new WaitForSeconds(moveStepDuration);
            
            // 到达格子时的小弹跳
            playerToken.DOPunchScale(Vector3.one * 0.1f, 0.1f, 5, 0.5f);
        }
        
        // 最终位置的弹跳效果
        playerToken.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);
    }
    
    /// <summary>
    /// 显示移动路径预览
    /// </summary>
    public void ShowPathPreview(Vector2Int from, Vector2Int to)
    {
        // TODO: 使用 LineRenderer 或 UI 元素显示路径
        if (pathRenderer != null)
        {
            // 实现路径预览
        }
    }
    
    /// <summary>
    /// 隐藏路径预览
    /// </summary>
    public void HidePathPreview()
    {
        if (pathRenderer != null)
        {
            pathRenderer.positionCount = 0;
        }
    }
    
    #endregion
    
    #region 格子高亮
    
    /// <summary>
    /// 高亮指定格子
    /// </summary>
    public void HighlightCell(Vector2Int pos, Color color)
    {
        if (!IsValidPosition(pos)) return;
        
        Image img = cells[pos.x, pos.y]?.GetComponent<Image>();
        if (img != null)
        {
            img.DOColor(color, 0.2f);
        }
    }
    
    /// <summary>
    /// 重置格子颜色
    /// </summary>
    public void ResetCellColor(Vector2Int pos)
    {
        if (!IsValidPosition(pos)) return;
        
        Image img = cells[pos.x, pos.y]?.GetComponent<Image>();
        if (img != null)
        {
            bool isLight = (pos.x + pos.y) % 2 == 0;
            Color originalColor = isLight ? new Color(0.9f, 0.9f, 0.85f) : new Color(0.7f, 0.7f, 0.65f);
            img.DOColor(originalColor, 0.2f);
        }
    }
    
    #endregion
}
