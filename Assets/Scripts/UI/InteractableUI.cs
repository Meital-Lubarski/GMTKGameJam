using General;
using UnityEngine;

/// <summary>
/// The "press E" prompt. It listens to <see cref="EventManager.OnInteractableChanged"/>,
/// which only speaks when the answer really changes, so this does no work at
/// all while the player walks around looking at nothing.
///
/// Put it straight on the object holding the prompt image: the CanvasGroup it
/// fades is added there for it. To fade a whole group of objects instead, put
/// it on their shared parent, or point <see cref="promptGroup"/> somewhere
/// else entirely.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class InteractableUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip(
        "Everything the prompt draws. Left empty, the CanvasGroup on this " +
        "same object is used."
    )]
    [SerializeField] private CanvasGroup promptGroup;

    [Header("Fade")]
    [Tooltip("Seconds the prompt takes to fade in or out. 0 snaps.")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.1f;

    private bool isPromptWanted;

    private void Awake()
    {
        if (promptGroup == null)
        {
            promptGroup = GetComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        EventManager.OnInteractableChanged += OnInteractableChanged;

        /*
         * The event only fires on a change, so a prompt that switches on while
         * the player is already looking at something would otherwise wait for
         * him to look away. The current answer is read straight off the
         * interactor instead.
         */
        PlayerInteractor interactor =
            FindFirstObjectByType<PlayerInteractor>();

        isPromptWanted =
            interactor != null &&
            interactor.CurrentInteractable != null;

        SetAlpha(isPromptWanted ? 1f : 0f);
    }

    private void OnDisable()
    {
        EventManager.OnInteractableChanged -= OnInteractableChanged;
    }

    private void OnInteractableChanged(IInteractable interactable)
    {
        isPromptWanted = interactable != null;

        if (fadeDuration <= 0f)
        {
            SetAlpha(isPromptWanted ? 1f : 0f);
        }
    }

    private void Update()
    {
        if (fadeDuration <= 0f)
        {
            return;
        }

        float targetAlpha = isPromptWanted ? 1f : 0f;

        if (Mathf.Approximately(promptGroup.alpha, targetAlpha))
        {
            return;
        }

        SetAlpha(
            Mathf.MoveTowards(
                promptGroup.alpha,
                targetAlpha,
                Time.unscaledDeltaTime / fadeDuration
            )
        );
    }

    /*
     * Hidden by its alpha rather than by switching the object off: the object
     * stays alive, so this component keeps listening and the fade always has
     * something to run on. An invisible group costs nothing to keep around,
     * as it never changes while it is hidden.
     */
    private void SetAlpha(float alpha)
    {
        promptGroup.alpha = alpha;
    }
}
