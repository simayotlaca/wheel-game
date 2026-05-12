using UnityEngine;

public partial class SpinRewardFlyAnimator
{
    private enum BurstPhase { Free, Queued, Spawning, Awaiting, PostLand }

    private struct Burst
    {
        public bool used;
        public BurstPhase phase;
        public float fireAt;
        public string rewardId;
        public RewardDefinition reward;
        public Sprite sprite;
        public Vector3 sourceWorld;
        public RewardListItemUI rowItem;
        public int particleCount;
        public int particlesSpawned;
        public int particlesLanded;
        public int delta;
        public int baseAmount;
        public int totalAmount;
        public int amountAwarded;
        public bool countUpStarted;
        public float startedAt;
    }

    private readonly Burst[] bursts = new Burst[GameRules.MaxSimultaneousBursts];

    int AcquireFreeBurst()
    {
        for (int i = 0; i < GameRules.MaxSimultaneousBursts; i++)
            if (!bursts[i].used) return i;
        return -1;
    }

    void TickAllBursts(float now)
    {
        for (int b = 0; b < GameRules.MaxSimultaneousBursts; b++)
        {
            if (!bursts[b].used) continue;

            if (!bursts[b].countUpStarted && bursts[b].phase == BurstPhase.Spawning
                && now - bursts[b].startedAt >= CountUpStartOffset)
            {
                bursts[b].countUpStarted = true;
            }

            switch (bursts[b].phase)
            {
                case BurstPhase.Queued:
                    if (now >= bursts[b].fireAt)
                    {
                        bursts[b].phase = BurstPhase.Spawning;
                        bursts[b].startedAt = now;
                        bursts[b].fireAt = now;
                    }
                    break;
                case BurstPhase.Spawning:
                    while (bursts[b].particlesSpawned < bursts[b].particleCount
                           && now >= bursts[b].fireAt)
                    {
                        if (!SpawnParticle(b))
                        {

                            break;
                        }
                        bursts[b].particlesSpawned++;
                        bursts[b].fireAt = now + BurstStaggerStep;
                    }
                    if (bursts[b].particlesSpawned >= bursts[b].particleCount)
                        bursts[b].phase = BurstPhase.Awaiting;
                    break;
                case BurstPhase.Awaiting:
                    if (bursts[b].particlesLanded >= bursts[b].particleCount)
                    {

                        if (bursts[b].amountAwarded != bursts[b].totalAmount && bursts[b].reward != null)
                            ApplyRowPartial(bursts[b].reward, bursts[b].totalAmount);
                        bursts[b].phase = BurstPhase.PostLand;
                        bursts[b].fireAt = now + PostLandHoldSeconds;
                    }
                    break;
                case BurstPhase.PostLand:

                    bool rowDone = bursts[b].rowItem == null || !bursts[b].rowItem.IsAnimating;
                    if (rowDone || now >= bursts[b].fireAt)
                    {
                        ClearBurst(b);
                        if (!AnyBurstActive() && wheelView != null) wheelView.UndimAll();
                    }
                    break;
            }
        }
    }

    bool AnyOtherBurstAirborne(int except)
    {
        for (int b = 0; b < GameRules.MaxSimultaneousBursts; b++)
            if (b != except && bursts[b].used
                && (bursts[b].phase == BurstPhase.Spawning || bursts[b].phase == BurstPhase.Awaiting))
                return true;
        return false;
    }

    bool AnyBurstActive()
    {
        for (int b = 0; b < GameRules.MaxSimultaneousBursts; b++)
            if (bursts[b].used) return true;
        return false;
    }

    void ClearBurst(int b)
    {
        bursts[b].used = false;
        bursts[b].phase = BurstPhase.Free;
        bursts[b].fireAt = 0f;
        bursts[b].rewardId = null;
        bursts[b].reward = null;
        bursts[b].sprite = null;
        bursts[b].sourceWorld = Vector3.zero;
        bursts[b].rowItem = null;
        bursts[b].particleCount = 0;
        bursts[b].particlesSpawned = 0;
        bursts[b].particlesLanded = 0;
        bursts[b].delta = 0;
        bursts[b].baseAmount = 0;
        bursts[b].totalAmount = 0;
        bursts[b].amountAwarded = 0;
        bursts[b].countUpStarted = false;
        bursts[b].startedAt = 0f;
    }
}
