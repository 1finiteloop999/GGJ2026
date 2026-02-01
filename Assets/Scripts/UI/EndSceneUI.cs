using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// 结束场景UI - 管理结束画面的图片和文本序列
/// </summary>
public class EndSceneUI : MonoBehaviour
{
    [Header("Images")]
    [Tooltip("Image 1 - shows first")]
    [SerializeField] private Image image1;
    
    [Tooltip("Image 2 - shows on first click (can have child text)")]
    [SerializeField] private Image image2;
    
    [Tooltip("Image 3 - shows on second click")]
    [SerializeField] private Image image3;
    
    [Header("Text")]
    [Tooltip("Final text - shows on third click")]
    [SerializeField] private TextMeshProUGUI finalText;
    
    [Header("Animation Settings")]
    [Tooltip("Fade in duration")]
    [SerializeField] private float fadeInDuration = 1f;
    
    [Tooltip("Fade out duration")]
    [SerializeField] private float fadeOutDuration = 1f;
    
    [Header("Scene Settings")]
    [Tooltip("Title scene name")]
    [SerializeField] private string titleSceneName = "Title";
    
    [Tooltip("Or use scene index (set to -1 to use name)")]
    [SerializeField] private int titleSceneIndex = 0;
    
    // Current state
    private int currentStep = 0;
    private bool isAnimating = false;
    
    // CanvasGroups for fading
    private CanvasGroup image1Group;
    private CanvasGroup image2Group;
    private CanvasGroup image3Group;
    private CanvasGroup textGroup;

    private void Start()
    {
        // Setup canvas groups
        SetupCanvasGroups();
        
        // Hide all elements
        HideAllImmediate();
        
        // Start sequence - show image 1
        StartCoroutine(StartSequence());
    }

    private void Update()
    {
        // Check for click (mouse or touch)
        if (Input.GetMouseButtonDown(0) && !isAnimating)
        {
            OnClick();
        }
    }

    /// <summary>
    /// Setup canvas groups for fading
    /// </summary>
    private void SetupCanvasGroups()
    {
        image1Group = GetOrAddCanvasGroup(image1?.gameObject);
        image2Group = GetOrAddCanvasGroup(image2?.gameObject);
        image3Group = GetOrAddCanvasGroup(image3?.gameObject);
        textGroup = GetOrAddCanvasGroup(finalText?.gameObject);
    }

    /// <summary>
    /// Get or add canvas group to game object
    /// </summary>
    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        
        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = go.AddComponent<CanvasGroup>();
        }
        return group;
    }

    /// <summary>
    /// Hide all elements immediately
    /// </summary>
    private void HideAllImmediate()
    {
        SetAlpha(image1Group, 0f);
        SetAlpha(image2Group, 0f);
        SetAlpha(image3Group, 0f);
        SetAlpha(textGroup, 0f);
        
        if (image1 != null) image1.gameObject.SetActive(false);
        if (image2 != null) image2.gameObject.SetActive(false);
        if (image3 != null) image3.gameObject.SetActive(false);
        if (finalText != null) finalText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Start the sequence - show image 1
    /// </summary>
    private IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Show image 1
        if (image1 != null)
        {
            image1.gameObject.SetActive(true);
            yield return StartCoroutine(FadeIn(image1Group));
        }
        
        currentStep = 1;
    }

    /// <summary>
    /// Handle click
    /// </summary>
    private void OnClick()
    {
        switch (currentStep)
        {
            case 1:
                // First click - show image 2
                StartCoroutine(Step1_ShowImage2());
                break;
                
            case 2:
                // Second click - hide image 1 & 2, show image 3
                StartCoroutine(Step2_ShowImage3());
                break;
                
            case 3:
                // Third click - hide image 3, show final text
                StartCoroutine(Step3_ShowFinalText());
                break;
                
            case 4:
                // Fourth click - go to title
                StartCoroutine(Step4_GoToTitle());
                break;
        }
    }

    /// <summary>
    /// Step 1: Show image 2
    /// </summary>
    private IEnumerator Step1_ShowImage2()
    {
        isAnimating = true;
        
        if (image2 != null)
        {
            image2.gameObject.SetActive(true);
            yield return StartCoroutine(FadeIn(image2Group));
        }
        
        currentStep = 2;
        isAnimating = false;
    }

    /// <summary>
    /// Step 2: Hide image 1 & 2, show image 3
    /// </summary>
    private IEnumerator Step2_ShowImage3()
    {
        isAnimating = true;
        
        // Fade out image 1 and 2 simultaneously
        Coroutine fade1 = null;
        Coroutine fade2 = null;
        
        if (image1 != null && image1.gameObject.activeSelf)
        {
            fade1 = StartCoroutine(FadeOut(image1Group));
        }
        
        if (image2 != null && image2.gameObject.activeSelf)
        {
            fade2 = StartCoroutine(FadeOut(image2Group));
        }
        
        yield return new WaitForSeconds(fadeOutDuration);
        
        // Hide image 1 and 2
        if (image1 != null) image1.gameObject.SetActive(false);
        if (image2 != null) image2.gameObject.SetActive(false);
        
        // Show image 3
        if (image3 != null)
        {
            image3.gameObject.SetActive(true);
            yield return StartCoroutine(FadeIn(image3Group));
        }
        
        currentStep = 3;
        isAnimating = false;
    }

    /// <summary>
    /// Step 3: Hide image 3, show final text
    /// </summary>
    private IEnumerator Step3_ShowFinalText()
    {
        isAnimating = true;
        
        // Fade out image 3
        if (image3 != null && image3.gameObject.activeSelf)
        {
            yield return StartCoroutine(FadeOut(image3Group));
            image3.gameObject.SetActive(false);
        }
        
        // Show final text
        if (finalText != null)
        {
            finalText.gameObject.SetActive(true);
            yield return StartCoroutine(FadeIn(textGroup));
        }
        
        currentStep = 4;
        isAnimating = false;
    }

    /// <summary>
    /// Step 4: Go to title scene
    /// </summary>
    private IEnumerator Step4_GoToTitle()
    {
        isAnimating = true;
        
        // Optional: fade out final text
        if (finalText != null && finalText.gameObject.activeSelf)
        {
            yield return StartCoroutine(FadeOut(textGroup));
        }
        
        // Load title scene
        if (titleSceneIndex >= 0)
        {
            SceneManager.LoadScene(titleSceneIndex);
        }
        else if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    #region Fade Animations

    /// <summary>
    /// Fade in animation
    /// </summary>
    private IEnumerator FadeIn(CanvasGroup group)
    {
        if (group == null) yield break;
        
        float elapsed = 0f;
        group.alpha = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            
            // Ease out
            t = t * t * (3f - 2f * t);
            
            group.alpha = t;
            yield return null;
        }
        
        group.alpha = 1f;
    }

    /// <summary>
    /// Fade out animation
    /// </summary>
    private IEnumerator FadeOut(CanvasGroup group)
    {
        if (group == null) yield break;
        
        float elapsed = 0f;
        float startAlpha = group.alpha;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            
            group.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        group.alpha = 0f;
    }

    /// <summary>
    /// Set alpha directly
    /// </summary>
    private void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
        {
            group.alpha = alpha;
        }
    }

    #endregion
}
