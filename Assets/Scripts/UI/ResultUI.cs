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

    [Header("分数显示")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI maxScoreText;
    [SerializeField] private TextMeshProUGUI percentText;

    [Header("评级显示")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Image rankImage;

    [Header("评级精灵（可选）")]
    [SerializeField] private Sprite spriteF;
    [SerializeField] private Sprite spriteA;
    [SerializeField] private Sprite spriteS;
    [SerializeField] private Sprite spriteSS;
    [SerializeField] private Sprite spriteSSS;

    [Header("评级颜色")]
    [SerializeField] private Color colorF = Color.gray;
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorS = Color.green;
    [SerializeField] private Color colorSS = Color.cyan;
    [SerializeField] private Color colorSSS = Color.yellow;

    [Header("通关状态")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject passedEffect;
    [SerializeField] private GameObject failedEffect;

    [Header("详细信息")]
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Transform stepResultContainer;
    [SerializeField] private GameObject stepResultPrefab;

    [Header("按钮")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;

    [Header("动画设置")]
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
        // 绑定按钮事件
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    /// <summary>
    /// 显示结果
    /// </summary>
    public void ShowResult(CompareResult result, LevelData levelData)
    {
        gameObject.SetActive(true);

        // 显示评级
        ShowRank(result.rank);

        // 显示通关状态
        ShowPassStatus(result.isPassed);

        // 显示分数
        if (useScoreAnimation)
        {
            StartCoroutine(AnimateScore(result.totalScore, result.maxScore));
        }
        else
        {
            ShowScoreImmediate(result.totalScore, result.maxScore);
        }

        // 显示详细信息
        ShowDetails(result, levelData);

        // 控制下一关按钮
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(result.isPassed);
        }
    }

    /// <summary>
    /// 显示评级
    /// </summary>
    private void ShowRank(RankType rank)
    {
        if (rankText != null)
        {
            rankText.text = rank.ToString();
            rankText.color = GetRankColor(rank);
        }

        if (rankImage != null)
        {
            Sprite rankSprite = GetRankSprite(rank);
            if (rankSprite != null)
            {
                rankImage.sprite = rankSprite;
                rankImage.color = Color.white;
            }
            else
            {
                rankImage.color = GetRankColor(rank);
            }
        }
    }

    /// <summary>
    /// 获取评级颜色
    /// </summary>
    private Color GetRankColor(RankType rank)
    {
        return rank switch
        {
            RankType.F => colorF,
            RankType.A => colorA,
            RankType.S => colorS,
            RankType.SS => colorSS,
            RankType.SSS => colorSSS,
            _ => Color.white
        };
    }

    /// <summary>
    /// 获取评级精灵
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
    /// 显示通关状态
    /// </summary>
    private void ShowPassStatus(bool passed)
    {
        if (statusText != null)
        {
            statusText.text = passed ? "通关！" : "未通关";
            statusText.color = passed ? Color.green : Color.red;
        }

        if (passedEffect != null)
        {
            passedEffect.SetActive(passed);
        }

        if (failedEffect != null)
        {
            failedEffect.SetActive(!passed);
        }
    }

    /// <summary>
    /// 立即显示分数
    /// </summary>
    private void ShowScoreImmediate(int score, int maxScore)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        if (maxScoreText != null)
        {
            maxScoreText.text = $"/ {maxScore}";
        }

        if (percentText != null)
        {
            float percent = maxScore > 0 ? (float)score / maxScore * 100f : 0f;
            percentText.text = $"{percent:F0}%";
        }
    }

    /// <summary>
    /// 分数动画
    /// </summary>
    private IEnumerator AnimateScore(int targetScore, int maxScore)
    {
        float elapsed = 0f;
        int currentScore = 0;

        while (elapsed < scoreAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scoreAnimationDuration;

            // 使用缓动函数
            t = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic

            currentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, t));

            if (scoreText != null)
            {
                scoreText.text = currentScore.ToString();
            }

            if (maxScoreText != null)
            {
                maxScoreText.text = $"/ {maxScore}";
            }

            if (percentText != null)
            {
                float percent = maxScore > 0 ? (float)currentScore / maxScore * 100f : 0f;
                percentText.text = $"{percent:F0}%";
            }

            yield return null;
        }

        // 确保最终值精确
        ShowScoreImmediate(targetScore, maxScore);
    }

    /// <summary>
    /// 显示详细信息
    /// </summary>
    private void ShowDetails(CompareResult result, LevelData levelData)
    {
        if (detailText != null)
        {
            string details = $"正确: {result.correctCount}/{result.totalSteps}\n";

            if (levelData != null)
            {
                details += $"\n分数线:\n";
                details += $"A: {levelData.scoreA}  S: {levelData.scoreS}\n";
                details += $"SS: {levelData.scoreSS}  SSS: {levelData.scoreSSS}";
            }

            detailText.text = details;
        }

        // 如果有步骤结果容器和预制体，显示每步详情
        if (stepResultContainer != null && stepResultPrefab != null)
        {
            // 清空旧的
            foreach (Transform child in stepResultContainer)
            {
                Destroy(child.gameObject);
            }

            // 创建新的
            foreach (var stepResult in result.stepResults)
            {
                GameObject stepObj = Instantiate(stepResultPrefab, stepResultContainer);

                // 尝试设置文本
                TextMeshProUGUI stepText = stepObj.GetComponentInChildren<TextMeshProUGUI>();
                if (stepText != null)
                {
                    string npcName = stepResult.npcCard != null ? stepResult.npcCard.GetDescription() : "-";
                    string playerName = stepResult.playerCard != null ? stepResult.playerCard.GetDescription() : "-";
                    string mark = stepResult.isCorrect ? "✓" : "✗";
                    stepText.text = $"{stepResult.stepIndex + 1}. {npcName} vs {playerName} {mark} +{stepResult.scoreGained}";
                    stepText.color = stepResult.isCorrect ? Color.green : Color.red;
                }

                // 尝试设置图片颜色
                Image stepImage = stepObj.GetComponent<Image>();
                if (stepImage != null)
                {
                    stepImage.color = stepResult.isCorrect ? new Color(0.5f, 1f, 0.5f, 0.3f) : new Color(1f, 0.5f, 0.5f, 0.3f);
                }
            }
        }
    }

    /// <summary>
    /// 重试按钮点击
    /// </summary>
    private void OnRetryClicked()
    {
        GameManager.Instance?.OnRestartButton();
    }

    /// <summary>
    /// 下一关按钮点击
    /// </summary>
    private void OnNextClicked()
    {
        GameManager.Instance?.OnNextLevelButton();
    }

    /// <summary>
    /// 隐藏结果UI
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}