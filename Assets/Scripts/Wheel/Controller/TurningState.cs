public class TurningState : WheelStateBase
{
    private WheelController controller;

    public TurningState(WheelController controller)
    {
        this.controller = controller;
    }

    public override WheelState GetStateEnum() { return WheelState.Spinning; }

    public override bool CheckSkipSpin() { return true; }
}
