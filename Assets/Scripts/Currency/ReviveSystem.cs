namespace VertigoWheel
{
internal class ReviveSystem
{
    private int revive_count;
    private CurrencyRules currency_rules;

    internal ReviveSystem(CurrencyRules rules)
    {
        currency_rules = rules;
    }

    internal int CurrentCost
    {
        get
        {
            return currency_rules.ReviveCostForCount(revive_count);
        }
    }

    internal bool CanAfford(CurrencyWallet inventory)
    {
        return inventory.Gold >= CurrentCost;
    }

    internal bool TryRevive(CurrencyWallet inventory)
    {
        if (inventory.TrySpendGold(CurrentCost))
        {
            revive_count++;
            return true;
        }
        return false;
    }

    internal void Reset()
    {
        revive_count = 0;
    }
}
}
