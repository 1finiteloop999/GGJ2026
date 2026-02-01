using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 结算UI - 显示关卡结果
/// </summary>
public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance { get; private set; }

    [Header("Rank Display")]
    [Tooltip("Rank image (F/A/S/SS/SSS)")]
    [SerializeField] private Image rankImage;

    [Header("Rank Sprites")]
    [SerializeField] private Sprite spriteF;
    [SerializeField] private Sprite spriteA;
    [SerializeField] private Sprite spriteS;
    [SerializeField] private Sprite spriteSS;
    [SerializeField] private Sprite spriteSSS;

    [Header("Score Display")]
    [Tooltip("Score text (e.g. '17')")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Score Slider")]
    [Tooltip("Score progress slider (non-interactable)")]
    [SerializeField] private Slider scoreSlider;

    [Header("Buttons")]
    [Tooltip("Restart current level")]
    [SerializeField] private Button restartButton;

    [Tooltip("Go to next level")]
    [SerializeField] private Button nextButton;

    [Header("Animation Settings")]
    [SerializeField] private float scoreAnimationDuration = 1f;
    [SerializeField] private bool useScoreAnimation = true;

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
        // Bind button events
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }

        // Make slider non-interactable
        if (scoreSlider != null)
        {
            scoreSlider.interactable = false;
        }
    }

    /// <summary>
    /// Show result
    /// </summary>
    public void ShowResult(CompareResult result, LevelData levelData)
    {
        gameObject.SetActive(true);

        // Show rank image
        ShowRank(result.rank);

        // Show score with animation
        if (useScoreAnimation)
        {
            StartCoroutine(AnimateScore(result.totalScore, result.maxScore));
        }
        else
        {
            ShowScoreImmediate(result.totalScore, result.maxScore);
        }

        // Control next button visibility (only show if passed)
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(result.isPassed);
        }
    }

    /// <summary>
    /// Show rank image
    /// </summary>
    private void ShowRank(RankType rank)
    {
        if (rankImage != null)
        {
            Sprite rankSprite = GetRankSprite(rank);
            if (rankSprite != null)
            {
                rankImage.sprite = rankSprite;
                rankImage.SetNativeSize(); // Set to original image size
                rankImage.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Get rank sprite
    /// </summary>
    private Sprite GetRankSprite(RankType rank)
    {
        return rank switch
        {
            RankType.F => spriteF,
            RankType.A => spriteA,
            RankType.S => spriteS,
            RankType.SS => spriteSS,
            RankType.SSS => spriteSSS,
            _ => null
        };
    }

    /// <summary>
    /// Show score immediately
    /// </summary>
    private void ShowScoreImmediate(int score, int maxScore)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        if (scoreSlider != null)
        {
            scoreSlider.maxValue = maxScore > 0 ? maxScore : 1;
            // Clamp value to max to prevent overflow
            scoreSlider.value = Mathf.Min(score, scoreSlider.maxValue);
        }
    }

    /// <summary>
    /// Animate score
    /// </summary>
    private IEnumerator AnimateScore(int targetScore, int maxScore)
    {
        float elapsed = 0f;
        int currentScore = 0;

        // Set slider max value
        if (scoreSlider != null)
        {
            scoreSlider.maxValue = maxScore > 0 ? maxScore : 1;
            scoreSlider.value = 0;
        }

        // Clamp target score for slider animation
        int clampedTargetScore = Mathf.Min(targetScore, maxScore);

        while (elapsed < scoreAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scoreAnimationDuration;

            // Ease out cubic
            t = 1f - Mathf.Pow(1f - t, 3f);

            currentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, t));

            if (scoreText != null)
            {
                scoreText.text = currentScore.ToString();
            }

            if (scoreSlider != null)
            {
                // Clamp slider value to max
                scoreSlider.value = Mathf.Min(currentScore, scoreSlider.maxValue);
            }

            yield return null;
        }

        // Ensure final values are exact
        ShowScoreImmediate(targetScore, maxScore);
    }

    /// <summary>
    /// Restart button clicked
    /// </summary>
    private void OnRestartClicked()
    {
        GameManager.Instance?.OnRestartButton();
    }

    /// <summary>
    /// Next button clicked
    /// </summary>
    private void OnNextClicked()
    {
        GameManager.Instance?.OnNextLevelButton();
    }

    /// <summary>
    /// Hide result UI
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}