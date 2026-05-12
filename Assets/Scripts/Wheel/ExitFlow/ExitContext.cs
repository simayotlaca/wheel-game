public enum ExitKind
{
    None,
    FreshStart,
    SafeExit,
}

public enum ExitVisibility
{
    Hidden,
    Disabled,
    Normal,
}

public static class ExitContext
{
    public static ExitKind Classify(WheelController controller)
    {
        if (controller == null) return ExitKind.None;
        if (controller.State != WheelState.Idle) return ExitKind.None;
        if (controller.check_revive_lock) return ExitKind.None;

        if (controller.MetaBusy) return ExitKind.None;

        if (!IsExitAllowedZone(controller)) return ExitKind.None;

        bool hasPending = controller.Inventory != null
                       && controller.Inventory.Pending.Count > 0;
        return hasPending ? ExitKind.SafeExit : ExitKind.FreshStart;
    }

    private static bool IsExitAllowedZone(WheelController controller)
    {
        if (controller == null || controller.Zones == null) return false;
        ZoneType t = controller.Zones.CurrentZoneType;
        if (t == ZoneType.Safe || t == ZoneType.Super) return true;

        if (controller.Config == null)
            throw new System.InvalidOperationException("ExitContext: WheelController config not assigned.");
        RunExitRules rules = controller.Config.ExitRules;
        if (rules == null)
            throw new System.InvalidOperationException("ExitContext: RunExitRules not assigned in WheelConfig.");
        return rules.AllowExitOnNormalZones;
    }

    public static bool IsExitAllowed(WheelController controller)
        => Classify(controller) != ExitKind.None;

    public static ExitVisibility ResolveVisibility(WheelController controller, bool flyBusy)
    {
        if (controller == null) return ExitVisibility.Hidden;

        if (controller.IsInDeathFlow) return ExitVisibility.Hidden;
        if (controller.check_revive_lock) return ExitVisibility.Hidden;
        if (controller.State != WheelState.Idle || flyBusy || controller.MetaBusy)
            return ExitVisibility.Disabled;

        if (!IsExitAllowedZone(controller)) return ExitVisibility.Disabled;
        return ExitVisibility.Normal;
    }
}
