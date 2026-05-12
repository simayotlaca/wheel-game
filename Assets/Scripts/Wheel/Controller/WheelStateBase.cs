public abstract class WheelStateBase
{
    public abstract WheelState GetStateEnum();

    public virtual bool CheckSpin()      { return false; }
    public virtual bool CheckSkipSpin() { return false; }
    public virtual bool CheckLeave()     { return false; }
    public virtual bool CheckRevive()    { return false; }

    public virtual void Enter() {}
    public virtual void Tick()  {}
    public virtual void Exit()  {}
}
