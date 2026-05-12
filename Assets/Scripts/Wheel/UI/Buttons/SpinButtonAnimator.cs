using UnityEngine;
using UnityEngine.UI;

public class SpinButtonAnimator : MonoBehaviour
{
    public enum State
    {
        Ready,
        Spinning,
        Cooldown,
        Skippable,
    }

    [SerializeField] private Button button;

    private static readonly Color NormalTint      = Color.white;
    private static readonly Color HighlightedTint = new Color(0.96f, 0.96f, 1.00f, 1f);
    private static readonly Color PressedTint     = new Color(0.78f, 0.80f, 0.92f, 1f);
    private static readonly Color DisabledTint    = new Color(0.55f, 0.58f, 0.70f, 1f);

    private State _state = State.Ready;

    void Awake()
    {
        if (button == null) Debug.LogError("SpinButtonAnimator: button not wired.", this);
        ConfigureColorBlock();
        ApplyState(_state);
    }

    public void SetState(State newState)
    {
        if (_state == newState) return;
        _state = newState;
        ApplyState(newState);
    }

    public State CurrentState => _state;

    private void ApplyState(State s)
    {
        if (button == null) return;
        bool interactable = (s == State.Ready || s == State.Skippable);
        if (button.interactable != interactable)
            button.interactable = interactable;
    }

    private void ConfigureColorBlock()
    {
        if (button == null) return;
        ColorBlock cb = button.colors;
        cb.normalColor      = NormalTint;
        cb.highlightedColor = HighlightedTint;
        cb.pressedColor     = PressedTint;
        cb.selectedColor    = HighlightedTint;
        cb.disabledColor    = DisabledTint;
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        button.colors = cb;
    }

    void Reset()
    {
        button = GetComponent<Button>();
    }
}
