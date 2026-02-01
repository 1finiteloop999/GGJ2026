using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// NPC交互 - 点击显示对话
/// </summary>
public class NPCInteraction : MonoBehaviour, IPointerClickHandler
{
    [Header("Dialogue Settings")]
    [Tooltip("Dialogue panel")]
    [SerializeField] private GameObject dialoguePanel;

    [Tooltip("Dialogue text component")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Tooltip("Stay duration before fade out")]
    [SerializeField] private float stayDuration = 3f;

    [Tooltip("Fade out duration")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Text Library")]
    [Tooltip("Random dialogue texts for this level")]
    [TextArea(2, 4)]
    [SerializeField] private List<string> dialogueTexts = new List<string>();

    // Components
    private CanvasGroup dialogueCanvasGroup;

    // State
    private bool isInteractable = false;
    private Coroutine dialogueCoroutine;

    private void Awake()
    {
        // Setup dialogue panel
        if (dialoguePanel != null)
        {
            dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (dialogueCanvasGroup == null)
            {
                dialogueCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            }
            dialoguePanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Subscribe to phase change
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            // Check current phase
            OnPhaseChanged(GameManager.Instance.CurrentPhase);
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
        // Only interactable during Planning phase
        isInteractable = (phase == GamePhase.Planning);

        // Hide dialogue when not in planning
        if (!isInteractable)
        {
            HideDialogueImmediate();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;

        ShowRandomDialogue();
    }

    #region Dialogue

    /// <summary>
    /// Show random dialogue from text library
    /// </summary>
    private void ShowRandomDialogue()
    {
        if (dialoguePanel == null || dialogueText == null) return;
        if (dialogueTexts.Count == 0) return;

        // Stop any running dialogue coroutine
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        // Pick random text
        int randomIndex = Random.Range(0, dialogueTexts.Count);
        string text = dialogueTexts[randomIndex];

        // Show dialogue
        dialogueText.text = text;
        dialoguePanel.SetActive(true);

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 1f;
        }

        // Play dialogue sound
        SoundManager.Instance?.PlayNPCDialogue();

        // Start stay and fade coroutine
        dialogueCoroutine = StartCoroutine(DialogueStayAndFade());
    }

    /// <summary>
    /// Dialogue stay and fade coroutine
    /// </summary>
    private IEnumerator DialogueStayAndFade()
    {
        // Stay for duration
        yield return new WaitForSeconds(stayDuration);

        // Fade out
        if (dialogueCanvasGroup != null)
        {
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                dialogueCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            dialogueCanvasGroup.alpha = 0f;
        }

        // Hide panel
        dialoguePanel.SetActive(false);
        dialogueCoroutine = null;
    }

    /// <summary>
    /// Hide dialogue immediately
    /// </summary>
    private void HideDialogueImmediate()
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    /// <summary>
    /// Add dialogue text to library
    /// </summary>
    public void AddDialogueText(string text)
    {
        if (!string.IsNullOrEmpty(text) && !dialogueTexts.Contains(text))
        {
            dialogueTexts.Add(text);
        }
    }

    /// <summary>
    /// Clear dialogue texts
    /// </summary>
    public void ClearDialogueTexts()
    {
        dialogueTexts.Clear();
    }

    /// <summary>
    /// Set dialogue texts
    /// </summary>
    public void SetDialogueTexts(List<string> texts)
    {
        dialogueTexts = new List<string>(texts);
    }
}