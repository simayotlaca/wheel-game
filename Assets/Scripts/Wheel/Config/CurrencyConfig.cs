using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyConfig", menuName = "Wheel/CurrencyConfig")]
public class CurrencyConfig : ScriptableObject
{
    [Header("Currency IDs")]
    [Tooltip("Reward ID treated as paper cash by RewardInventory.BankPending. " +
             "Banked cash flows into the Cash counter shown by the cash HUD.")]
    public string cashCurrencyId = "cash";

    [Tooltip("Reward ID for the gold currency — banked into the Gold counter " +
             "used by ZoneHUD count-up and the revive-cost spend path.")]
    public string goldCurrencyId = "gold";

    [Header("Initial Wallet (first launch only)")]
    [Tooltip("Cash the player starts with on a brand-new save. Subsequent " +
             "launches restore from PlayerProgress instead of re-seeding.")]
    [Min(0)] public int initialCash = 0;

    [Tooltip("Gold the player starts with on a brand-new save. Default seeds " +
             "exactly one revive so the first death isn't a hard wall.")]
    [Min(0)] public int initialGold = 200;

    [Tooltip("Wipe PlayerProgress at launch so the wallet re-seeds from " +
             "initialCash/initialGold every Play. Dev-only — leave OFF for builds.")]
    public bool resetSaveOnLaunch = false;

    [Header("Meta Progress Demo")]
    [Tooltip("Threshold for the CASH row in the right-side meta progression " +
             "panel. Tiny by default so the very first collect crosses it and " +
             "fires the UNLOCKED animation — a sanity check, not real spec.")]
    [Min(1)] public int demoCashProgressTarget = 10000;
}
