using System.Globalization;

public static class RewardDisplayFormatter
{
    public static string Format(RewardDefinition reward, int amount)
    {
        if (reward == null || reward.isDeath) return string.Empty;

        if (reward.displayAsMultiplier)
            return "x" + FormatCompact(amount);

        switch (reward.visualCategory)
        {
            case RewardVisualCategory.Coin:
            case RewardVisualCategory.Cash:
                return FormatCompact(amount);

            case RewardVisualCategory.Weapon:
                return "+" + FormatCompact(amount);

            case RewardVisualCategory.Compact:
            case RewardVisualCategory.Chest:
            case RewardVisualCategory.Consumable:
            case RewardVisualCategory.Cosmetic:
                return amount <= 1 ? string.Empty : "x" + amount.ToString(CultureInfo.InvariantCulture);

            default:
                return amount.ToString(CultureInfo.InvariantCulture);
        }
    }

    public static string FormatCompact(int value)
    {
        if (value >= 1_000_000)
            return (value / 1_000_000f).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        if (value >= 1_000)
            return (value / 1_000f).ToString("0.#", CultureInfo.InvariantCulture) + "K";
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
