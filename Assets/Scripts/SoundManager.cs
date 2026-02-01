using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音效管理器 - 管理游戏中的所有音效
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [Tooltip("Audio source for playing sounds")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI Sound Effects")]
    [Tooltip("Button click sound")]
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Card Sound Effects")]
    [Tooltip("Card placed in slot sound")]
    [SerializeField] private AudioClip cardPlaceSound;

    [Header("Deck Sound Effects")]
    [Tooltip("Deck expand sound")]
    [SerializeField] private AudioClip deckExpandSound;

    [Tooltip("Deck collapse sound")]
    [SerializeField] private AudioClip deckCollapseSound;

    [Header("NPC Sound Effects")]
    [Tooltip("NPC dialogue popup sound")]
    [SerializeField] private AudioClip npcDialogueSound;

    [Tooltip("NPC bow action sound")]
    [SerializeField] private AudioClip npcBowSound;

    [Tooltip("NPC jump action sound")]
    [SerializeField] private AudioClip npcJumpSound;

    [Tooltip("NPC sit action sound")]
    [SerializeField] private AudioClip npcSitSound;

    [Tooltip("NPC wave action sound")]
    [SerializeField] private AudioClip npcWaveSound;

    [Tooltip("NPC angry expression sound")]
    [SerializeField] private AudioClip npcAngrySound;

    [Tooltip("NPC laugh expression sound")]
    [SerializeField] private AudioClip npcLaughSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float buttonVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float cardVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float deckVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float npcVolume = 1f;

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

        // Auto create audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
    }

    #region Public Play Methods

    /// <summary>
    /// Play button click sound
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySound(buttonClickSound, buttonVolume);
    }

    /// <summary>
    /// Play card place sound
    /// </summary>
    public void PlayCardPlace()
    {
        PlaySound(cardPlaceSound, cardVolume);
    }

    /// <summary>
    /// Play deck expand sound (no button click)
    /// </summary>
    public void PlayDeckExpand()
    {
        PlaySound(deckExpandSound, deckVolume);
    }

    /// <summary>
    /// Play deck collapse sound (no button click)
    /// </summary>
    public void PlayDeckCollapse()
    {
        PlaySound(deckCollapseSound, deckVolume);
    }

    /// <summary>
    /// Play NPC dialogue popup sound
    /// </summary>
    public void PlayNPCDialogue()
    {
        PlaySound(npcDialogueSound, npcVolume);
    }

    /// <summary>
    /// Play NPC action sound by action type
    /// </summary>
    public void PlayNPCAction(ActionType actionType)
    {
        AudioClip clip = actionType switch
        {
            ActionType.Bow => npcBowSound,
            ActionType.Jump => npcJumpSound,
            ActionType.SitDown => npcSitSound,
            ActionType.Wave => npcWaveSound,
            _ => null
        };

        PlaySound(clip, npcVolume);
    }

    /// <summary>
    /// Play NPC expression sound by expression type
    /// </summary>
    public void PlayNPCExpression(ExpressionType expressionType)
    {
        AudioClip clip = expressionType switch
        {
            ExpressionType.Angry => npcAngrySound,
            ExpressionType.Laugh => npcLaughSound,
            _ => null
        };

        PlaySound(clip, npcVolume);
    }

    #endregion

    /// <summary>
    /// Play a sound clip
    /// </summary>
    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}