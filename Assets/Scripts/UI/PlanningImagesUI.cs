using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 规划阶段图片UI - 管理图片的渐显渐隐
/// 图片直接在场景的Image组件上设置，不从LevelData读取
/// </summary>
public class PlanningImagesUI : MonoBehaviour
{
    public static PlanningImagesUI Instance { get; private set; }

    [Header("Image Components")]
    [Tooltip("First image (set sprite directly on this component)")]
    [SerializeField] private Image image1;

    [Tooltip("Second image (set sprite directly on this component)")]
    [SerializeField] private Image image2;

    [Header("Animation Settings")]
    [Tooltip("Fade in duration")]
    [SerializeField] private float fadeInDuration = 1f;

    [Tooltip("Delay between two images")]
    [SerializeField] private float delayBetweenImages = 0.3f;

    [Tooltip("Fade out duration")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    // Current coroutine
    private Coroutine fadeCoroutine;

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
        // Hide images at start
        HideImagesImmediate();

        // Subscribe to phase change event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    /// <summary>
    /// Handle phase change
    /// </summary>
    private void OnPhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Planning)
        {
            // Show images when entering planning phase
            ShowImages();
        }
        else if (phase == GamePhase.Executing || phase == GamePhase.Result)
        {
            // Hide images when executing or showing results
            HideImages();
        }
    }

    /// <summary>
    /// Show images with fade in animation
    /// </summary>
    public void ShowImages()
    {
        // Stop any running coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Check which images have sprites
        bool hasImage1 = image1 != null && image1.sprite != null;
        bool hasImage2 = image2 != null && image2.sprite != null;

        // Setup images
        if (hasImage1)
        {
            image1.gameObject.SetActive(true);
            SetImageAlpha(image1, 0f);
        }

        if (hasImage2)
        {
            image2.gameObject.SetActive(true);
            SetImageAlpha(image2, 0f);
        }

        // Start fade in animation
        if (hasImage1 || hasImage2)
        {
            fadeCoroutine = StartCoroutine(FadeInSequence(hasImage1, hasImage2));
        }
    }

    /// <summary>
    /// Fade in sequence coroutine
    /// </summary>
    private IEnumerator FadeInSequence(bool hasImage1, bool hasImage2)
    {
        // Fade in image 1
        if (hasImage1 && image1 != null)
        {
            yield return StartCoroutine(FadeImage(image1, 0f, 1f, fadeInDuration));
        }

        // Delay between images
        if (hasImage1 && hasImage2)
        {
            yield return new WaitForSeconds(delayBetweenImages);
        }

        // Fade in image 2
        if (hasImage2 && image2 != null)
        {
            yield return StartCoroutine(FadeImage(image2, 0f, 1f, fadeInDuration));
        }

        fadeCoroutine = null;
    }

    /// <summary>
    /// Hide images with fade out animation
    /// </summary>
    public void HideImages()
    {
        // Stop any running coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutSequence());
    }

    /// <summary>
    /// Fade out sequence coroutine
    /// </summary>
    private IEnumerator FadeOutSequence()
    {
        // Fade out both images simultaneously
        if (image1 != null && image1.gameObject.activeSelf)
        {
            StartCoroutine(FadeImage(image1, image1.color.a, 0f, fadeOutDuration));
        }

        if (image2 != null && image2.gameObject.activeSelf)
        {
            StartCoroutine(FadeImage(image2, image2.color.a, 0f, fadeOutDuration));
        }

        // Wait for fade out to complete
        yield return new WaitForSeconds(fadeOutDuration);

        // Hide game objects
        if (image1 != null) image1.gameObject.SetActive(false);
        if (image2 != null) image2.gameObject.SetActive(false);

        fadeCoroutine = null;
    }

    /// <summary>
    /// Hide images immediately without animation
    /// </summary>
    public void HideImagesImmediate()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (image1 != null)
        {
            SetImageAlpha(image1, 0f);
            image1.gameObject.SetActive(false);
        }

        if (image2 != null)
        {
            SetImageAlpha(image2, 0f);
            image2.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Fade image alpha
    /// </summary>
    private IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration)
    {
        if (image == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease in out
            t = t * t * (3f - 2f * t);

            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            SetImageAlpha(image, alpha);

            yield return null;
        }

        SetImageAlpha(image, toAlpha);
    }

    /// <summary>
    /// Set image alpha
    /// </summary>
    private void SetImageAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}