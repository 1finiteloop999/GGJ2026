using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏暂停UI - 管理帮助面板和暂停面板
/// </summary>
public class GamePauseUI : MonoBehaviour
{
    public static GamePauseUI Instance { get; private set; }
    
    [Header("Help Button")]
    [Tooltip("Question mark button")]
    [SerializeField] private Button helpButton;
    
    [Tooltip("Help/Tutorial panel")]
    [SerializeField] private GameObject helpPanel;
    
    [Tooltip("Continue button in help panel")]
    [SerializeField] private Button helpContinueButton;
    
    [Header("Pause Button")]
    [Tooltip("Pause button")]
    [SerializeField] private Button pauseButton;
    
    [Tooltip("Pause panel")]
    [SerializeField] private GameObject pausePanel;
    
    [Tooltip("Continue button in pause panel")]
    [SerializeField] private Button pauseContinueButton;
    
    [Tooltip("Exit button in pause panel")]
    [SerializeField] private Button exitButton;
    
    [Header("Exit Settings")]
    [Tooltip("Scene to load when exit (leave empty to load scene index 0)")]
    [SerializeField] private string exitSceneName = "";
    
    [Tooltip("Scene index to load when exit (-1 to use scene name)")]
    [SerializeField] private int exitSceneIndex = 0;
    
    // State
    private bool isPaused = false;
    private float previousTimeScale = 1f;

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
        // Bind help button
        if (helpButton != null)
        {
            helpButton.onClick.AddListener(OnHelpClicked);
        }
        
        if (helpContinueButton != null)
        {
            helpContinueButton.onClick.AddListener(OnHelpContinueClicked);
        }
        
        // Bind pause button
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
        }
        
        if (pauseContinueButton != null)
        {
            pauseContinueButton.onClick.AddListener(OnPauseContinueClicked);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
        }
        
        // Hide panels at start
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Ensure time scale is restored when destroyed
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }

    #region Help Panel

    /// <summary>
    /// Help button clicked - show help panel
    /// </summary>
    private void OnHelpClicked()
    {
        ShowHelpPanel();
    }

    /// <summary>
    /// Show help panel and pause game
    /// </summary>
    public void ShowHelpPanel()
    {
        if (helpPanel == null) return;
        
        PauseGame();
        helpPanel.SetActive(true);
        
        // Hide pause panel if visible
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Continue button in help panel clicked
    /// </summary>
    private void OnHelpContinueClicked()
    {
        HideHelpPanel();
    }

    /// <summary>
    /// Hide help panel and resume game
    /// </summary>
    public void HideHelpPanel()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
        
        ResumeGame();
    }

    #endregion

    #region Pause Panel

    /// <summary>
    /// Pause button clicked - show pause panel
    /// </summary>
    private void OnPauseClicked()
    {
        ShowPausePanel();
    }

    /// <summary>
    /// Show pause panel and pause game
    /// </summary>
    public void ShowPausePanel()
    {
        if (pausePanel == null) return;
        
        PauseGame();
        pausePanel.SetActive(true);
        
        // Hide help panel if visible
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Continue button in pause panel clicked
    /// </summary>
    private void OnPauseContinueClicked()
    {
        HidePausePanel();
    }

    /// <summary>
    /// Hide pause panel and resume game
    /// </summary>
    public void HidePausePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        ResumeGame();
    }

    /// <summary>
    /// Exit button clicked - exit to main menu or title
    /// </summary>
    private void OnExitClicked()
    {
        // Resume time before loading new scene
        Time.timeScale = 1f;
        isPaused = false;
        
        // Load exit scene
        if (exitSceneIndex >= 0)
        {
            SceneManager.LoadScene(exitSceneIndex);
        }
        else if (!string.IsNullOrEmpty(exitSceneName))
        {
            SceneManager.LoadScene(exitSceneName);
        }
        else
        {
            // Default to first scene
            SceneManager.LoadScene(0);
        }
    }

    #endregion

    #region Pause/Resume

    /// <summary>
    /// Pause the game
    /// </summary>
    private void PauseGame()
    {
        if (isPaused) return;
        
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        
        Debug.Log("[GamePauseUI] Game paused");
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    private void ResumeGame()
    {
        if (!isPaused) return;
        
        Time.timeScale = previousTimeScale;
        isPaused = false;
        
        Debug.Log("[GamePauseUI] Game resumed");
    }

    /// <summary>
    /// Check if game is paused
    /// </summary>
    public bool IsPaused => isPaused;

    #endregion

    #region Keyboard Support

    private void Update()
    {
        // ESC key to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                // If help panel is open, close it
                if (helpPanel != null && helpPanel.activeSelf)
                {
                    HideHelpPanel();
                }
                // If pause panel is open, close it
                else if (pausePanel != null && pausePanel.activeSelf)
                {
                    HidePausePanel();
                }
            }
            else
            {
                // Open pause panel
                ShowPausePanel();
            }
        }
    }

    #endregion
}
