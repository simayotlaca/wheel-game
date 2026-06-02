using PrimeTween;
using UnityEngine;
using TMPro;

namespace VertigoWheel
{
public class CurrencyHUD : MonoBehaviour
{
    private enum AmountTextMode
    {
        Plain,
        Compact
    }

    #region setup
    [SerializeField] private RunSession controller;
    [SerializeField] private ConfigAnimation anim_config;

    [SerializeField] private CanvasGroup currency_group;
    [SerializeField] private Canvas currency_canvas;
    [SerializeField] private TMP_Text cash_value;
    [SerializeField] private TMP_Text gold_value;

    private Tween cash_tween;
    private Tween gold_tween;

    private int last_cash_text_value = -1;
    private AmountTextMode last_cash_text_mode;
    private int last_gold_text_value = -1;
    private AmountTextMode last_gold_text_mode;

    private bool initial_sync_done;
    #endregion

    public void SetInteractive(bool active)
    {
        currency_group.alpha = 1f;
        currency_group.blocksRaycasts = active;
        currency_group.interactable = active;
    }

    private void Awake()
    {
        if (currency_canvas != null)
        {
            currency_canvas.overrideSorting = true;
        }
    }

    private void OnDestroy()
    {
        KillCountTweens();
    }

    private void OnEnable()
    {
        controller.OnRewardsBanked += HandleRewardsBanked;
        controller.OnRevived += HandleRevived;
    }

    private void Start()
    {
        EnsureInitial();
    }

    private void OnDisable()
    {
        controller.OnRewardsBanked -= HandleRewardsBanked;
        controller.OnRevived -= HandleRevived;
        KillCountTweens();
    }

    private void EnsureInitial()
    {
        if (!initial_sync_done && controller.Inventory != null)
        {
            SyncFromInventory();
            initial_sync_done = true;
        }
    }

    private void HandleRewardsBanked()
    {
        SyncFromInventory();
    }

    private void HandleRevived()
    {
        SyncFromInventory();
    }

    //same idea as reward rows, count with plain numbers first
    //then settle back to compact text after the tween finishes
    private void SyncFromInventory()
    {
        if (!initial_sync_done)
        {
            WriteCashValue(controller.Inventory.Cash, AmountTextMode.Compact);
            WriteGoldValue(controller.Inventory.Gold, AmountTextMode.Compact);
        }
        else
        {
            cash_tween.Stop();
            cash_tween = Tween.Custom(last_cash_text_value, controller.Inventory.Cash, anim_config.currencyCountUpDuration, OnCashTweenValue)
                .OnComplete(OnCashTweenComplete);

            gold_tween.Stop();
            gold_tween = Tween.Custom(last_gold_text_value, controller.Inventory.Gold, anim_config.currencyCountUpDuration, OnGoldTweenValue)
                .OnComplete(OnGoldTweenComplete);
        }
    }

    private void KillCountTweens()
    {
        TweenLifetime.StopIfAlive(cash_tween);
        TweenLifetime.StopIfAlive(gold_tween);
    }

    private void OnCashTweenValue(float value)
    {
        WriteCashValue(Mathf.RoundToInt(value), AmountTextMode.Plain);
    }

    private void OnGoldTweenValue(float value)
    {
        WriteGoldValue(Mathf.RoundToInt(value), AmountTextMode.Plain);
    }

    private void OnCashTweenComplete()
    {
        WriteCashValue(controller.Inventory.Cash, AmountTextMode.Compact);
    }

    private void OnGoldTweenComplete()
    {
        WriteGoldValue(controller.Inventory.Gold, AmountTextMode.Compact);
    }

    private void WriteCashValue(int value, AmountTextMode mode)
    {
        if (value != last_cash_text_value || mode != last_cash_text_mode)
        {
            last_cash_text_value = value;
            last_cash_text_mode = mode;

            if (mode == AmountTextMode.Compact)
            {
                cash_value.text = NumberFormatter.FormatCompact(value);
            }
            else
            {
                cash_value.SetText("{0}", value);
            }
        }
    }

    private void WriteGoldValue(int value, AmountTextMode mode)
    {
        if (value != last_gold_text_value || mode != last_gold_text_mode)
        {
            last_gold_text_value = value;
            last_gold_text_mode = mode;

            if (mode == AmountTextMode.Compact)
            {
                gold_value.text = NumberFormatter.FormatCompact(value);
            }
            else
            {
                gold_value.SetText("{0}", value);
            }
        }
    }
}
}
