public class ReadyState : WheelStateBase
{
    private WheelController controller;

    public ReadyState(WheelController controller)
    {
        this.controller = controller;
    }

    public override WheelState GetStateEnum() { return WheelState.Idle; }

    public override bool CheckSpin()
    {

        return !controller.IsCollecting && !controller.MetaBusy;
    }

    public override bool CheckLeave()
    {
        return !controller.IsCollecting && !controller.MetaBusy;
    }
}
