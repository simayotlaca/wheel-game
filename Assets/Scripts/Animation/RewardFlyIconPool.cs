using System;
using PrimeTween;
using UnityEngine;

namespace VertigoWheel
{
public class RewardFlyIconPool : MonoBehaviour
{
    [SerializeField] private RectTransform fly_container;
    [SerializeField] private RewardFlyIcon icon_prefab;

    private ObjectPool<RewardFlyIcon> pool;
    private StackFlyRunner runner;

    private void Awake()
    {
        int icon_count = icon_prefab.IconCount;
        pool = new ObjectPool<RewardFlyIcon>(icon_prefab, fly_container, icon_count);
        runner = new StackFlyRunner(icon_count, HandleRunnerReleased);
    }

    private void OnDestroy()
    {
        CancelAll();
    }

    internal void Play(Sprite sprite, Vector3 src_world, RectTransform target_rt, int amount, Action<int> arrived_cb)
    {
        if (runner.IsActive)
        {
            return;
        }

        runner.ResetIcons();
        for (int i = 0; i < runner.Capacity; i++)
        {
            RewardFlyIcon icon = pool.Acquire();
            icon.Configure(sprite);
            runner.PushIcon(icon);
        }

        StackFlyRunner.Setup setup = new StackFlyRunner.Setup
        {
            source_world = src_world,
            target_rect = target_rt,
            amount = amount,
            on_arrived = arrived_cb,
            motion = icon_prefab.Motion,
        };
        runner.Begin(setup);
    }

    internal void CancelAll()
    {
        runner.Cancel();
        ReleaseAllRunnerIcons(runner);
    }

    private void HandleRunnerReleased(StackFlyRunner released_runner)
    {
        ReleaseAllRunnerIcons(released_runner);
    }

    private void ReleaseAllRunnerIcons(StackFlyRunner released_runner)
    {
        pool.ReleaseAll();
        released_runner.ResetIcons();
    }

}

internal class StackFlyRunner
{
    internal struct Setup
    {
        internal Vector3 source_world;
        internal RectTransform target_rect;
        internal int amount;
        internal Action<int> on_arrived;
        internal RewardFlyMotionSettings motion;
    }

    private struct FlightMotion
    {
        internal Vector3 path_position;
        internal float offset_amount;
        internal float scale;
        internal float alpha;
    }

    private class IconStack
    {
        private RewardFlyIcon[] icons;
        private Vector3[] offsets;
        private int count;

        internal int Count
        {
            get
            {
                return count;
            }
        }

        internal int Capacity
        {
            get
            {
                return icons.Length;
            }
        }

        internal IconStack(int capacity)
        {
            icons = new RewardFlyIcon[capacity];
            offsets = new Vector3[capacity];
        }

        internal void ResetIconArray()
        {
            for (int i = 0; i < count; i++)
            {
                icons[i] = null;
            }
            count = 0;
        }

        internal void Push(RewardFlyIcon icon)
        {
            icons[count] = icon;
            count++;
        }

        internal void LoadOffsets(in RewardFlyMotionSettings motion)
        {
            for (int i = 0; i < count; i++)
            {
                offsets[i] = motion.GetIconOffset(i);
            }
        }

        internal void PlaceAtStart(Vector3 source_world)
        {
            for (int i = 0; i < count; i++)
            {
                RewardFlyIcon icon = icons[i];
                RectTransform rt = icon.Rect;
                rt.SetAsLastSibling();
                rt.position = source_world;
                rt.localScale = Vector3.one;
                icon.CanvasGroup.alpha = 1f;
            }
        }

        internal void ApplyMotion(int index, in FlightMotion motion)
        {
            Vector3 final_pos = motion.path_position + offsets[index] * motion.offset_amount;
            RewardFlyIcon icon = icons[index];
            RectTransform rt = icon.Rect;
            rt.position = final_pos;
            rt.localScale = new Vector3(motion.scale, motion.scale, 1f);
            icon.CanvasGroup.alpha = motion.alpha;
        }
    }

    private class FlightState
    {
        internal Vector3 source_world;
        internal RectTransform target_rect;
        internal int amount;
        internal RewardFlyMotionSettings motion;
        internal Action<int> on_arrived;
        internal Tween tween;
        internal bool active;

        internal void Begin(in Setup setup)
        {
            source_world = setup.source_world;
            target_rect = setup.target_rect;
            amount = setup.amount;
            motion = setup.motion;
            on_arrived = setup.on_arrived;
            active = true;
        }

        internal void Cancel()
        {
            Clear();
        }

        internal void Clear()
        {
            active = false;
            on_arrived = null;
            target_rect = null;
            amount = 0;
        }
    }

    private IconStack stack;
    private Action<float> on_progress;
    private Action on_complete;
    private Action<StackFlyRunner> on_released;

    private FlightState flight;

    internal bool IsActive
    {
        get
        {
            return flight.active;
        }
    }

    internal int Capacity
    {
        get
        {
            return stack.Capacity;
        }
    }

    internal StackFlyRunner(int capacity, Action<StackFlyRunner> released_cb)
    {
        stack = new IconStack(capacity);
        flight = new FlightState();
        on_released = released_cb;
        on_progress = HandleProgress;
        on_complete = HandleComplete;
    }

    internal void ResetIcons()
    {
        stack.ResetIconArray();
    }

    internal void PushIcon(RewardFlyIcon icon)
    {
        stack.Push(icon);
    }

    internal void Begin(in Setup setup)
    {
        flight.Begin(setup);
        stack.LoadOffsets(flight.motion);
        stack.PlaceAtStart(flight.source_world);

        TweenLifetime.StopIfAlive(flight.tween);
        flight.tween = Tween.Custom(
            0f,
            1f,
            flight.motion.duration,
            on_progress,
            Ease.Linear,
            useUnscaledTime: flight.motion.use_unscaled_time);
        flight.tween.OnComplete(on_complete);
    }

    internal void Cancel()
    {
        if (flight.active || stack.Count > 0)
        {
            flight.Cancel();
            TweenLifetime.StopIfAlive(flight.tween);
        }
    }

    private void HandleProgress(float progress)
    {
        FlightMotion motion = SampleMotion(progress);

        for (int i = 0; i < stack.Count; i++)
        {
            stack.ApplyMotion(i, motion);
        }
    }

    private FlightMotion SampleMotion(float progress)
    {
        RewardFlyMotionSettings motion = flight.motion;
        float travel_progress = Mathf.Clamp01(motion.travel_curve.Evaluate(progress));
        Vector3 target = flight.target_rect.position;
        return new FlightMotion
        {
            path_position = Vector3.LerpUnclamped(flight.source_world, target, travel_progress),
            offset_amount = Mathf.Clamp01(motion.offset_curve.Evaluate(progress)),
            scale = Mathf.Max(0f, motion.scale_curve.Evaluate(progress)),
            alpha = Mathf.Clamp01(motion.alpha_curve.Evaluate(progress)),
        };
    }

    private void HandleComplete()
    {
        if (!flight.active)
        {
            return;
        }

        Action<int> arrived_cb = flight.on_arrived;
        int amount = flight.amount;
        FinishFlight();
        arrived_cb?.Invoke(amount);
    }

    private void FinishFlight()
    {
        flight.Clear();
        on_released?.Invoke(this);
    }
}
}
