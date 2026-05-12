using UnityEngine;

public class RewardState : WheelStateBase
{
    private WheelController controller;
    private float wait_timer;

    public RewardState(WheelController controller)
    {
        this.controller = controller;
    }

    public override WheelState GetStateEnum() { return WheelState.Reward; }

    public override bool CheckRevive()
    {
        return controller.LastResultIsDeath && controller.CanAffordRevive();
    }

    public override void Enter()
    {
        wait_timer = 0f;
    }

    public override void Tick()
    {
        wait_timer += Time.deltaTime;
        if (wait_timer >= controller.RewardHoldDuration)
            controller.FinishReward();
    }
}
