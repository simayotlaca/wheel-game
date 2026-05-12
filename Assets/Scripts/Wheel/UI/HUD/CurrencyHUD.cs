using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] private WheelController controller;
    [SerializeField] private WheelAnimationConfig animConfig;
    [SerializeField] private RectTransform cashGroup;
    [SerializeField] private RectTransform coinGroup;
    [SerializeField] private TMP_Text cashLabel;
    [SerializeField] private TMP_Text coinLabel;

    [Header("Punch")]
    [SerializeField] private float commitPunchAmount = 0.18f;
    [SerializeField] private float commitPunchDuration = 0.28f;
    [SerializeField] private float spendPunchAmount = 0.16f;
    [SerializeField] private float spendPunchDuration = 0.22f;

    private int cash_amount;
    private int coin_amount;
    private IntCountUpTween cash_tween;
    private IntCountUpTween coin_tween;
    private int last_displayed_cash = int.MinValue;
    private int last_displayed_coin = int.MinValue;
    private bool initial_sync_done;

    private bool deferred;

    private Tween cash_punch_tween;
    private Tween coin_punch_tween;

    public RectTransform RootTarget => transform as RectTransform;

    public RectTransform CashTarget
    {
        get
        {
            if (cashGroup != null) return cashGroup;
            if (cashLabel != null) return cashLabel.rectTransform;
            return RootTarget;
        }
    }

    public RectTransform GoldTarget
    {
        get
        {
            if (coinGroup != null) return coinGroup;
            if (coinLabel != null) return coinLabel.rectTransform;
            return RootTarget;
        }
    }

    void Awake()
    {
        if (animConfig == null)
        {
            Debug.LogError("CurrencyHUD: animConfig is not assigned.", this);
            enabled = false;
            return;
        }
        SnapLabelsToTarget();
    }

    void Update()
    {

        if (!initial_sync_done && controller != null && controller.Inventory != null)
        {
            SyncFromInventory();
            SnapLabelsToTarget();
            initial_sync_done = true;
        }

        if (!cash_tween.IsActive && !coin_tween.IsActive) return;

        float now = Time.unscaledTime;
        float dur = animConfig.countUpDuration;
        if (cash_tween.Tick(now, dur)) WriteCashLabel(cash_tween.Current);
        if (coin_tween.Tick(now, dur)) WriteCoinLabel(coin_tween.Current);
    }

    void OnDestroy()
    {
        KillPunchTweens();
    }

    void OnEnable()
    {
        deferred = false;
        if (controller != null)
        {
            controller.OnRewardsBanked += HandleRewardsBanked;
            controller.OnRevived += HandleRevived;
        }
        SyncFromInventory();
        SnapLabelsToTarget();
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnRewardsBanked -= HandleRewardsBanked;
            controller.OnRevived -= HandleRevived;
        }

        KillPunchTweens();
        initial_sync_done = false;
    }

    void HandleRewardsBanked()
    {
        if (deferred) return;
        int prev_cash = cash_amount;
        int prev_coin = coin_amount;
        SyncFromInventory();
        SnapLabelsToTarget();
        if (cash_amount > prev_cash) PlayCashCommitFeedback();
        if (coin_amount > prev_coin) PlayCoinCommitFeedback();
    }

    public void SetDeferredUpdate(bool deferred)
    {
        this.deferred = deferred;
        if (!deferred)
        {
            SyncFromInventory();
            BeginCashCountUp();
            BeginCoinCountUp();
        }
    }

    public void AddCash(int delta)
    {
        if (delta <= 0) return;
        cash_amount += delta;
        BeginCashCountUp();
    }

    public void AddGold(int delta)
    {
        if (delta <= 0) return;
        coin_amount += delta;
        BeginCoinCountUp();
    }

    void HandleRevived()
    {
        int prev_coin = coin_amount;
        SyncFromInventory();
        if (coin_amount != prev_coin) BeginCoinCountUp();
        if (coin_amount < prev_coin) PlayCoinSpendFeedback();
    }

    void SyncFromInventory()
    {
        if (controller == null || controller.Inventory == null) return;
        RewardInventory inventory = controller.Inventory;
        cash_amount = inventory[CurrencyType.cash];
        coin_amount = inventory[CurrencyType.gold];
    }

    public void PlayCashCommitFeedback() =>
        Punch(CashTarget, commitPunchAmount, commitPunchDuration, ref cash_punch_tween);

    public void PlayCoinCommitFeedback() =>
        Punch(GoldTarget, commitPunchAmount, commitPunchDuration, ref coin_punch_tween);

    public void PlayCoinSpendFeedback() =>
        Punch(GoldTarget, spendPunchAmount, spendPunchDuration, ref coin_punch_tween);

    private void Punch(RectTransform target, float amount, float duration, ref Tween tween)
    {
        if (target == null) return;
        if (tween.isAlive) tween.Stop();
        target.localScale = Vector3.one;
        tween = Tween.PunchScale(target, new Vector3(amount, amount, 0f), duration);
    }

    private void KillPunchTweens()
    {
        if (cash_punch_tween.isAlive) cash_punch_tween.Stop();
        if (coin_punch_tween.isAlive) coin_punch_tween.Stop();
    }

    void SnapLabelsToTarget()
    {
        cash_tween = default;
        coin_tween = default;
        SnapCashLabel();
        SnapCoinLabel();
    }

    void SnapCashLabel()
    {
        if (cash_tween.SetTarget(cash_amount, Time.unscaledTime))
            WriteCashLabel(cash_tween.Current);
    }

    void SnapCoinLabel()
    {
        if (coin_tween.SetTarget(coin_amount, Time.unscaledTime))
            WriteCoinLabel(coin_tween.Current);
    }

    void BeginCashCountUp()
    {
        cash_tween.SetTarget(cash_amount, Time.unscaledTime);
    }

    void BeginCoinCountUp()
    {
        coin_tween.SetTarget(coin_amount, Time.unscaledTime);
    }

    void WriteCashLabel(int value)
    {
        if (cashLabel == null || value == last_displayed_cash) return;
        cashLabel.text = NumberFormatter.FormatCompact(value);
        last_displayed_cash = value;
    }

    void WriteCoinLabel(int value)
    {
        if (coinLabel == null || value == last_displayed_coin) return;
        coinLabel.text = NumberFormatter.FormatCompact(value);
        last_displayed_coin = value;
    }
}
