public class DeathState : WheelStateBase
{
    private WheelController controller;

    public DeathState(WheelController controller)
    {
        this.controller = controller;
    }

    public override WheelState GetStateEnum() { return WheelState.DeathGameOver; }

    public override bool CheckRevive() { return controller.CanAffordRevive(); }
}
