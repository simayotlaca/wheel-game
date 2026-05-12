using UnityEngine;

public class BlurBackgroundOverlay : MonoBehaviour
{
    [SerializeField] private RunExitController exitController;
    [SerializeField] private CanvasGroup blurGroup;
    [SerializeField] private float fadeDuration = 0.2f;

    private float target_alpha;
    private bool subscribed;

    void OnEnable()
    {
        Subscribe();

        if (exitController != null) Apply(exitController.State);
        else Apply(ExitFlowState.None);
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        if (blurGroup == null) return;
        float current = blurGroup.alpha;
        if (Mathf.Approximately(current, target_alpha))
        {
            enabled = false;
            return;
        }

        float step = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;
        blurGroup.alpha = Mathf.MoveTowards(current, target_alpha, step);

        bool visible = blurGroup.alpha > 0.01f;
        blurGroup.blocksRaycasts = visible;
        blurGroup.interactable = visible;
    }

    void Subscribe()
    {
        if (subscribed || exitController == null) return;
        exitController.OnStateChanged += Apply;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed || exitController == null) return;
        exitController.OnStateChanged -= Apply;
        subscribed = false;
    }

    void Apply(ExitFlowState state)
    {
        bool show = state == ExitFlowState.CollectConfirm
                 || state == ExitFlowState.FreshStartConfirm;
        target_alpha = show ? 1f : 0f;
        enabled = true;
    }

}
