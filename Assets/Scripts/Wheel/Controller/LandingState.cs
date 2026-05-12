using UnityEngine;

public class LandingState : WheelStateBase
{
    private WheelController controller;
    private float wait_timer;

    public LandingState(WheelController controller)
    {
        this.controller = controller;
    }

    public override WheelState GetStateEnum() { return WheelState.Landing; }

    public override void Enter()
    {
        wait_timer = 0f;
    }

    public override void Tick()
    {
        wait_timer += Time.deltaTime;
        if (wait_timer >= controller.PopupShowDuration)
            controller.FinishLanding();
    }
}
