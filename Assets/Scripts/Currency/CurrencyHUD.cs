using PrimeTween;
using UnityEngine;
using TMPro;

namespace VertigoWheel
{
public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] private RunSession controller;

    [SerializeField] private CanvasGroup currency_group;
    [SerializeField] private TMP_Text cash_value;
    [SerializeField] private TMP_Text gold_value;

    private Tween cash_tween;
    private Tween gold_tween;

    private AmountTextDisplay cash_display;
    private AmountTextDisplay gold_display;
    private bool has_currency_state;

    private RunEventPass event_pass;

    internal void SetInteractive(bool active)
    {
        currency_group.blocksRaycasts = active;
        currency_group.interactable = active;
    }

    private void Awake()
    {
        event_pass = new RunEventPass(controller.Events);
        cash_display = new AmountTextDisplay(cash_value);
        gold_display = new AmountTextDisplay(gold_value);
    }

    private void OnEnable()
    {
        event_pass.Subscribe<RunCurrencyChangedEvent>(HandleCurrencyChanged);
    }

    private void OnDisable()
    {
        event_pass.ReleaseAll();
        TweenLifetime.StopIfAlive(cash_tween);
        TweenLifetime.StopIfAlive(gold_tween);
    }

    private void HandleCurrencyChanged(RunCurrencyChangedEvent evt)
    {
        if (!has_currency_state)
        {
            has_currency_state = true;
            cash_display.Write(evt.cash);
            gold_display.Write(evt.gold);
            return;
        }

        float duration = controller.CurrencyHudTiming.currency_count_up_duration;
        StartCurrencyTween(ref cash_tween, cash_display, evt.cash, duration);
        StartCurrencyTween(ref gold_tween, gold_display, evt.gold, duration);
    }

    private static void StartCurrencyTween(ref Tween tween, AmountTextDisplay display, int target, float duration)
    {
        TweenLifetime.StopIfAlive(tween);
        if (target != display.Value)
        {
            tween = Tween.Custom(display, (float)display.Value, (float)target, duration, WriteCurrencyTweenValue);
        }
    }

    private static void WriteCurrencyTweenValue(AmountTextDisplay display, float value)
    {
        display.Write(Mathf.RoundToInt(value));
    }

    private class AmountTextDisplay
    {
        private TMP_Text label;
        private int value = -1;

        internal int Value
        {
            get
            {
                return value;
            }
        }

        internal AmountTextDisplay(TMP_Text label)
        {
            this.label = label;
        }

        internal void Write(int next)
        {
            if (next != value)
            {
                value = next;
                TextTransformer.SetCompactNumber(label, next);
            }
        }
    }
}
}
