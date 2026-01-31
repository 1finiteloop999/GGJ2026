using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UI管理器 - 管理游戏界面显示
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("点数显示")]
    [SerializeField] private TextMeshProUGUI pointsText;

    [Header("按钮")]
    [SerializeField] private Button startPlanningButton;
    [SerializeField] private Button executeButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button restartButton;

    [Header("结果面板")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;

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
    }

    private void Start()
    {
        // 订阅事件
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnPointsChanged += UpdatePointsDisplay;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        // 绑定按钮
        BindButtons();
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnPointsChanged -= UpdatePointsDisplay;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtons()
    {
        if (startPlanningButton != null)
        {
            startPlanningButton.onClick.AddListener(() => GameManager.Instance?.OnStartPlanningButton());
        }

        if (executeButton != null)
        {
            executeButton.onClick.AddListener(() => GameManager.Instance?.OnExecuteButton());
        }

        // Replay按钮 - 重新加载场景
        if (replayButton != null)
        {
            replayButton.onClick.AddListener(ReloadScene);
        }

        // Restart按钮 - 重新加载场景
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(ReloadScene);
        }
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    private void ReloadScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// 更新点数显示
    /// </summary>
    public void UpdatePointsDisplay(int points)
    {
        if (pointsText != null)
        {
            pointsText.text = $"点数: {points}";
        }
    }

    /// <summary>
    /// 游戏阶段变化
    /// </summary>
    private void OnPhaseChanged(GamePhase phase)
    {
        UpdateButtonStates(phase);
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates(GamePhase phase)
    {
        // 开始推演按钮 - 只在观看阶段可用
        if (startPlanningButton != null)
        {
            startPlanningButton.gameObject.SetActive(phase == GamePhase.Watching);
        }

        // 执行按钮 - 只在规划阶段可用
        if (executeButton != null)
        {
            executeButton.gameObject.SetActive(phase == GamePhase.Planning);
        }

        // 重看按钮 - 在规划阶段可用
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(phase == GamePhase.Planning);
        }

        // 重新开始按钮 - 在结算阶段可用
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(phase == GamePhase.Result);
        }

        // 结果面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(phase == GamePhase.Result);
        }
    }

    /// <summary>
    /// 显示结果
    /// </summary>
    public void ShowResult(bool passed, int mimicryPercent)
    {
        if (resultText != null)
        {
            if (passed)
            {
                resultText.text = $"通关！\n模仿度: {mimicryPercent}%";
                resultText.color = Color.green;
            }
            else
            {
                resultText.text = $"未通过\n模仿度: {mimicryPercent}%";
                resultText.color = Color.red;
            }
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
    }
}