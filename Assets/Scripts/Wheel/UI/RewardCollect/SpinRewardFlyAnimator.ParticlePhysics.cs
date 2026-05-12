using PrimeTween;
using UnityEngine;

public partial class SpinRewardFlyAnimator
{
    private const float base_frame_rate = 60f;

    private struct Particle
    {
        public bool used;
        public int burstIdx;
        public Vector3 freePos;
        public Vector3 velocity;
        public Vector3 target;
        public float elapsed;
        public float lifetime;
        public RewardFlyIcon icon;
        public float startScale;
        public float endScale;
    }

    private readonly Particle[] particles = new Particle[GameRules.MaxParticles];

    int AcquireFreeParticle()
    {
        for (int i = 0; i < GameRules.MaxParticles; i++)
            if (!particles[i].used) return i;
        return -1;
    }

    void TickAllParticles(float dt)
    {
        for (int p = 0; p < GameRules.MaxParticles; p++)
        {
            if (!particles[p].used) continue;
            TickParticle(p, dt);
        }
    }

    bool SpawnParticle(int burstIdx)
    {
        int p = AcquireFreeParticle();
        if (p < 0) return false;

        RewardFlyIcon fly = pool.Acquire();
        if (fly == null) return false;

        float size = BurstParticleSize;
        fly.Configure(bursts[burstIdx].sprite, new Vector2(size, size));
        RectTransform rt = fly.Rect;
        if (rt == null) { pool.Release(fly); return false; }

        Vector3 src = bursts[burstIdx].sourceWorld;
        rt.SetAsLastSibling();
        rt.position = src;
        rt.localScale = Vector3.one;

        Vector3 target = src;
        bool haveRowTarget = false;
        if (bursts[burstIdx].rowItem != null && bursts[burstIdx].rowItem.IconWorldPosition != Vector3.zero)
        { target = bursts[burstIdx].rowItem.IconWorldPosition; haveRowTarget = true; }
        else if (rewardList != null && rewardList.TryGetRewardIconPoint(bursts[burstIdx].rewardId, out Vector3 t))
        { target = t; haveRowTarget = true; }

        if (rewardList != null)
        {
            bool offscreen = haveRowTarget && rewardList.IsRowIconOffscreen(target.y);
            if ((!haveRowTarget || offscreen) && rewardList.TryGetOverflowFlyTarget(out Vector3 overflow))
                target = overflow;
        }

        Vector3 toTarget = target - src;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float halfSpread = BurstSpreadDegrees * 0.5f;
        float angleDeg = baseAngle + UnityEngine.Random.Range(-halfSpread, halfSpread);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float speed = UnityEngine.Random.Range(BurstStartSpeedMin, BurstStartSpeedMax);
        Vector3 vel = new Vector3(Mathf.Cos(angleRad) * speed, Mathf.Sin(angleRad) * speed, 0f);

        particles[p].used = true;
        particles[p].burstIdx = burstIdx;
        particles[p].freePos = src;
        particles[p].velocity = vel;
        particles[p].target = target;
        particles[p].elapsed = 0f;
        particles[p].lifetime = BurstLifetime;
        particles[p].icon = fly;
        particles[p].startScale = 1f;
        particles[p].endScale = BurstEndScale;
        return true;
    }

    void TickParticle(int p, float dt)
    {
        if (dt <= 0f) return;

        particles[p].elapsed += dt;
        float life = particles[p].lifetime;
        float t = life > 0f ? Mathf.Clamp01(particles[p].elapsed / life) : 1f;

        float dragPerFrame = BurstDrag;
        float dampStep = Mathf.Pow(dragPerFrame, dt * base_frame_rate);
        particles[p].velocity *= dampStep;

        particles[p].freePos += particles[p].velocity * dt;

        float ramp = BurstAttractorRamp;
        float w = ramp == 1f ? t : Mathf.Pow(t, ramp);

        int trackBurst = particles[p].burstIdx;
        if (bursts[trackBurst].used && bursts[trackBurst].rowItem != null)
        {
            Vector3 cur = bursts[trackBurst].rowItem.IconWorldPosition;
            if (cur != Vector3.zero) particles[p].target = cur;
        }
        Vector3 pos = Vector3.LerpUnclamped(particles[p].freePos, particles[p].target, w);

        RewardFlyIcon fly = particles[p].icon;
        if (fly != null)
        {
            RectTransform rt = fly.Rect;
            if (rt != null)
            {
                rt.position = pos;
                float s = Mathf.LerpUnclamped(particles[p].startScale, particles[p].endScale, w);
                rt.localScale = new Vector3(s, s, 1f);
            }
            CanvasGroup cg = fly.CanvasGroup;
            if (cg != null)
            {

                cg.alpha = t < 0.75f ? 1f : Mathf.Clamp01(1f - (t - 0.75f) / 0.25f);
            }
        }

        float distSq = (pos - particles[p].target).sqrMagnitude;
        float thresh = BurstArrivalThresh;
        if (distSq <= thresh * thresh || t >= 1f)
        {
            OnParticleArrived(p);
        }
    }

    void OnParticleArrived(int p)
    {
        int b = particles[p].burstIdx;
        RewardFlyIcon fly = particles[p].icon;

        if (fly != null && pool != null) pool.Release(fly);
        particles[p].used = false;
        particles[p].icon = null;

        if (!bursts[b].used) return;

        bursts[b].particlesLanded++;

        int landed = bursts[b].particlesLanded;
        int delta = bursts[b].delta;
        int N = bursts[b].particleCount;
        int share = (landed >= N) ? delta : Mathf.Min(delta, Mathf.RoundToInt((float)delta * landed / N));
        int newRunning = bursts[b].baseAmount + share;
        if (newRunning < bursts[b].amountAwarded) newRunning = bursts[b].amountAwarded;
        bursts[b].amountAwarded = newRunning;
        bursts[b].countUpStarted = true;

        if (bursts[b].reward != null)
            ApplyRowPartial(bursts[b].reward, newRunning);

        RewardListItemUI rowItem = bursts[b].rowItem;
        bool isFirst = landed == 1;
        bool isLast  = landed >= N;
        if (rowItem != null && (isFirst || isLast))
        {
            Transform rt = rowItem.ReceiveTransform;
            if (rt != null)
            {
                Tween.StopAll(onTarget: rt);
                float peak = RowPunchScale;
                const float PunchCurvePeak = 0.58f;
                float mag = Mathf.Max(0f, (peak - 1f) / PunchCurvePeak);
                if (isFirst && !isLast) mag *= 0.5f;
                Tween.PunchScale(rt, new Vector3(mag, mag, 0f), RowPunchDuration);
            }
        }

        if (landed == 1 && wheelView != null && !AnyOtherBurstAirborne(b))
            wheelView.UndimAll();
    }
}
