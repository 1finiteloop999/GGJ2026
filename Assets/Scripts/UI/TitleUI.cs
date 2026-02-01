using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// 封面UI - 管理Start和Quit按钮及开篇动画
/// </summary>
public class TitleUI : MonoBehaviour
{
    [Header("Buttons")]
    [Tooltip("Start Button")]
    [SerializeField] private Button startButton;
    
    [Tooltip("Quit Button")]
    [SerializeField] private Button quitButton;
    
    [Header("Opening Panel")]
    [Tooltip("Opening story panel")]
    [SerializeField] private GameObject openingPanel;
    
    [Tooltip("First line of text")]
    [SerializeField] private TextMeshProUGUI text1;
    
    [Tooltip("Second line of text")]
    [SerializeField] private TextMeshProUGUI text2;
    
    [Header("Animation Settings")]
    [Tooltip("Fade in duration")]
    [SerializeField] private float fadeInDuration = 1f;
    
    [Tooltip("Delay between texts")]
    [SerializeField] private float delayBetweenTexts = 1f;
    
    [Tooltip("Stay duration after both texts appear")]
    [SerializeField] private float stayDuration = 1f;
    
    [Tooltip("Fade out duration")]
    [SerializeField] private float fadeOutDuration = 1f;
    
    [Header("Scene Settings")]
    [Tooltip("Next scene name to load")]
    [SerializeField] private string nextSceneName = "Game";
    
    [Tooltip("Or use scene index (set to -1 to use name)")]
    [SerializeField] private int nextSceneIndex = -1;

    private void Start()
    {
        // Bindbutton events
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        // Hide opening panel at start
        if (openingPanel != null)
        {
            openingPanel.SetActive(false);
        }
        
        // Initialize text alpha to 0
        if (text1 != null)
        {
            SetTextAlpha(text1, 0f);
        }
        
        if (text2 != null)
        {
            SetTextAlpha(text2, 0f);
        }
    }

    /// <summary>
    /// Start button clicked
    /// </summary>
    private void OnStartClicked()
    {
        // Disable buttons to prevent double click
        if (startButton != null) startButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;
        
        // Start opening sequence
        StartCoroutine(PlayOpeningSequence());
    }

    /// <summary>
    /// Quit button clicked
    /// </summary>
    private void OnQuitClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Play opening sequence animation
    /// </summary>
    private IEnumerator PlayOpeningSequence()
    {
        // Show opening panel
        if (openingPanel != null)
        {
            openingPanel.SetActive(true);
        }
        
        // Fade in first text
        if (text1 != null)
        {
            yield return StartCoroutine(FadeText(text1, 0f, 1f, fadeInDuration));
        }
        
        // Wait before showing second text
        yield return new WaitForSeconds(delayBetweenTexts);
        
        // Fade in second text
        if (text2 != null)
        {
            yield return StartCoroutine(FadeText(text2, 0f, 1f, fadeInDuration));
        }
        
        // Stay for a moment
        yield return new WaitForSeconds(stayDuration);
        
        // Fade out both texts simultaneously
        if (text1 != null && text2 != null)
        {
            StartCoroutine(FadeText(text1, 1f, 0f, fadeOutDuration));
            yield return StartCoroutine(FadeText(text2, 1f, 0f, fadeOutDuration));
        }
        else if (text1 != null)
        {
            yield return StartCoroutine(FadeText(text1, 1f, 0f, fadeOutDuration));
        }
        else if (text2 != null)
        {
            yield return StartCoroutine(FadeText(text2, 1f, 0f, fadeOutDuration));
        }
        
        // Load next scene
        LoadNextScene();
    }

    /// <summary>
    /// Fade text alpha
    /// </summary>
    private IEnumerator FadeText(TextMeshProUGUI text, float fromAlpha, float toAlpha, float duration)
    {
        if (text == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Ease in out
            t = t * t * (3f - 2f * t);
            
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            SetTextAlpha(text, alpha);
            
            yield return null;
        }
        
        SetTextAlpha(text, toAlpha);
    }

    /// <summary>
    /// Set text alpha
    /// </summary>
    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text != null)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }

    /// <summary>
    /// Load next scene
    /// </summary>
    private void LoadNextScene()
    {
        if (nextSceneIndex >= 0)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Load next scene by build index
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentIndex + 1);
        }
    }
}
