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

    [Header("Sound Effects")]
    [Tooltip("Button click sound")]
    [SerializeField] private AudioClip buttonClickSound;

    [Tooltip("Card placed in slot sound")]
    [SerializeField] private AudioClip cardPlaceSound;

    [Tooltip("Deck expand sound")]
    [SerializeField] private AudioClip deckExpandSound;

    [Tooltip("Deck collapse sound")]
    [SerializeField] private AudioClip deckCollapseSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float buttonVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float cardVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float deckVolume = 1f;

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