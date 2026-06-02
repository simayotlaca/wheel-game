using System;
using PrimeTween;
using UnityEngine;

namespace VertigoWheel
{
public class RewardFlyIconPool : MonoBehaviour
{
    [SerializeField] private RectTransform fly_container;
    [SerializeField] private ConfigAnimation anim_config;

    private ObjectPool<RewardFlyIcon> pool;
    private StackFlyRunner[] runners;
    private Action<StackFlyRunner> on_runner_released;

    public event Action OnAllComplete;

    private void Awake()
    {
        int icons_per_flight = Mathf.Max(1, anim_config.stackFlyMaxVisibleIcons);
        int flights = Mathf.Max(1, anim_config.stackFlyMaxConcurrentFlights);
        on_runner_released = HandleRunnerReleased;
        pool = new ObjectPool<RewardFlyIcon>(anim_config.stackFlyIconPrefab, fly_container, flights * icons_per_flight);
        runners = new StackFlyRunner[flights];
        for (int i = 0; i < flights; i++)
        {
            runners[i] = new StackFlyRunner(icons_per_flight, on_runner_released);
        }
    }

    private void OnEnable()
    {
        pool.DeactivateAll();
    }

    private void OnDisable()
    {
        ReleaseAll();
    }

    public bool AnyActive
    {
        get
        {
            if (runners == null)
            {
                return false;
            }
            for (int i = 0; i < runners.Length; i++)
            {
                if (runners[i].IsActive)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public bool Play(Sprite sprite, Vector3 src_world, RectTransform target_rt, int amount, Action<int> arrived_cb)
    {
        return PlayInternal(sprite, src_world, target_rt, Vector3.zero, amount, arrived_cb);
    }

    public bool Play(Sprite sprite, Vector3 src_world, Vector3 target_world, int amount, Action<int> arrived_cb)
    {
        return PlayInternal(sprite, src_world, null, target_world, amount, arrived_cb);
    }

    public void ReleaseAll()
    {
        if (runners != null)
        {
            for (int i = 0; i < runners.Length; i++)
            {
                runners[i].Cancel();
            }
        }
        if (pool != null)
        {
            pool.DeactivateAll();
        }
        OnAllComplete?.Invoke();
    }

    private bool PlayInternal(Sprite sprite, Vector3 src_world, RectTransform target_rt, Vector3 target_world, int amount, Action<int> arrived_cb)
    {
        if (sprite == null || amount <= 0)
        {
            return false;
        }

        StackFlyRunner runner = AcquireRunner();
        if (runner == null)
        {
            return false;
        }

        int visible_count = ResolveVisibleCount(amount);
        runner.ResetIcons();
        for (int i = 0; i < visible_count; i++)
        {
            RewardFlyIcon icon = pool.Acquire();
            if (icon == null)
            {
                break;
            }
            icon.Configure(sprite, anim_config.stackFlyIconSize, anim_config.stackFlyIconTint);
            runner.PushIcon(icon);
        }

        if (runner.IconCount == 0)
        {
            return false;
        }

        StackFlyRunner.Setup setup = new StackFlyRunner.Setup
        {
            source_world = src_world,
            target_rect = target_rt,
            target_world = target_world,
            duration = anim_config.stackFlyDuration,
            spread_end_t = anim_config.stackFlySpreadEndT,
            travel_start_t = anim_config.stackFlyTravelStartT,
            spread_ease = anim_config.stackFlySpreadEase,
            move_ease = anim_config.stackFlyMoveEase,
            merge_ease = anim_config.stackFlyMergeEase,
            end_scale = anim_config.stackFlyEndScale,
            fade_start_t = anim_config.stackFlyFadeStartT,
            use_unscaled_time = anim_config.stackFlyUseUnscaledTime,
            horizontal_spacing = anim_config.stackFlyHorizontalSpacing,
            vertical_arc = anim_config.stackFlyVerticalArc,
            down_step = anim_config.stackFlyDownStep,
            side_jitter = anim_config.stackFlySideJitter,
            amount = amount,
            on_arrived = arrived_cb,
        };
        runner.Begin(setup);
        return true;
    }

    private int ResolveVisibleCount(int amount)
    {
        int lo = Mathf.Clamp(anim_config.stackFlyMinVisibleIcons, 1, anim_config.stackFlyMaxVisibleIcons);
        int hi = Mathf.Max(lo, anim_config.stackFlyMaxVisibleIcons);
        return Mathf.Clamp(amount, lo, hi);
    }

    private StackFlyRunner AcquireRunner()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            if (!runners[i].IsActive)
            {
                return runners[i];
            }
        }
        return null;
    }

    private void HandleRunnerReleased(StackFlyRunner runner)
    {
        int n = runner.IconCount;
        for (int i = 0; i < n; i++)
        {
            pool.Release(runner.GetIcon(i));
        }
        runner.ResetIcons();
        if (!AnyActive)
        {
            OnAllComplete?.Invoke();
        }
    }
}

public sealed class StackFlyRunner
{
    public struct Setup
    {
        public Vector3 source_world;
        public RectTransform target_rect;
        public Vector3 target_world;
        public float duration;
        public float spread_end_t;
        public float travel_start_t;
        public Ease spread_ease;
        public Ease move_ease;
        public Ease merge_ease;
        public float end_scale;
        public float fade_start_t;
        public bool use_unscaled_time;
        public float horizontal_spacing;
        public float vertical_arc;
        public float down_step;
        public float side_jitter;
        public int amount;
        public Action<int> on_arrived;
    }

    private struct FlightMotion
    {
        public Vector3 path_position;
        public float offset_factor;
        public float scale;
        public float alpha;
    }

    private readonly RewardFlyIcon[] icons;
    private readonly Vector3[] offsets;
    private readonly Action<float> on_progress;
    private readonly Action on_complete;
    private readonly Action<StackFlyRunner> on_released;

    private int count;
    private Vector3 source_world;
    private RectTransform target_rect;
    private Vector3 target_world;
    private float spread_end_t;
    private float travel_start_t;
    private Ease spread_ease;
    private Ease move_ease;
    private Ease merge_ease;
    private float end_scale;
    private float fade_start_t;
    private int amount;
    private Action<int> on_arrived;
    private Tween tween;
    private bool active;
    private bool delivered;

    public bool IsActive => active;
    public int IconCount => count;

    public StackFlyRunner(int capacity, Action<StackFlyRunner> released_cb)
    {
        int cap = Mathf.Max(1, capacity);
        icons = new RewardFlyIcon[cap];
        offsets = new Vector3[cap];
        on_released = released_cb;
        on_progress = HandleProgress;
        on_complete = HandleComplete;
    }

    public void ResetIcons()
    {
        for (int i = 0; i < count; i++)
        {
            icons[i] = null;
        }
        count = 0;
    }

    public bool PushIcon(RewardFlyIcon icon)
    {
        if (icon == null || count >= icons.Length)
        {
            return false;
        }
        icons[count] = icon;
        count++;
        return true;
    }

    public RewardFlyIcon GetIcon(int index)
    {
        return icons[index];
    }

    public void Begin(in Setup setup)
    {
        source_world = setup.source_world;
        target_rect = setup.target_rect;
        target_world = ResolveInitialTarget(setup);
        spread_end_t = setup.spread_end_t;
        travel_start_t = setup.travel_start_t;
        spread_ease = setup.spread_ease;
        move_ease = setup.move_ease;
        merge_ease = setup.merge_ease;
        end_scale = setup.end_scale;
        fade_start_t = setup.fade_start_t;
        amount = setup.amount;
        on_arrived = setup.on_arrived;
        active = true;
        delivered = false;

        BuildStackOffsets(setup.horizontal_spacing, setup.vertical_arc, setup.down_step, setup.side_jitter);
        PlaceIconsAtStart();

        TweenLifetime.StopIfAlive(tween);
        tween = Tween.Custom(0f, 1f, setup.duration, on_progress, Ease.Linear, useUnscaledTime: setup.use_unscaled_time);
        tween.OnComplete(on_complete);
    }

    public void Cancel()
    {
        if (!active)
        {
            return;
        }
        TweenLifetime.StopIfAlive(tween);
        Release();
    }

    private Vector3 ResolveInitialTarget(in Setup setup)
    {
        if (setup.target_rect != null)
        {
            return setup.target_rect.position;
        }
        return setup.target_world;
    }

    private Vector3 ResolveTarget()
    {
        if (target_rect != null)
        {
            return target_rect.position;
        }
        return target_world;
    }

    private void BuildStackOffsets(float h_space, float v_arc, float down_step, float side_jitter)
    {
        float center = (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float rel = i - center;
            float alternate = (i % 2 == 0) ? 1f : -1f;
            float x = alternate * h_space + rel * side_jitter;
            float y = -rel * down_step + Mathf.Abs(rel) * v_arc;
            offsets[i] = new Vector3(x, y, 0f);
        }
    }

    private void PlaceIconsAtStart()
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

    private void HandleProgress(float progress)
    {
        FlightMotion motion = EvaluateMotion(progress);

        for (int i = 0; i < count; i++)
        {
            ApplyMotionToIcon(i, motion);
        }
    }

    private FlightMotion EvaluateMotion(float progress)
    {
        float travel_t;
        float offset_factor;

        if (progress < spread_end_t)
        {
            float local_t = spread_end_t > 0f ? progress / spread_end_t : 1f;
            offset_factor = Easing.Evaluate(local_t, spread_ease);
            travel_t = 0f;
        }
        else if (progress < travel_start_t)
        {
            offset_factor = 1f;
            travel_t = 0f;
        }
        else
        {
            float span = 1f - travel_start_t;
            float local_t = span > 0f ? (progress - travel_start_t) / span : 1f;
            travel_t = Easing.Evaluate(local_t, move_ease);
            offset_factor = 1f - Easing.Evaluate(local_t, merge_ease);
        }

        Vector3 target = ResolveTarget();
        return new FlightMotion
        {
            path_position = Vector3.LerpUnclamped(source_world, target, travel_t),
            offset_factor = offset_factor,
            scale = Mathf.LerpUnclamped(1f, end_scale, travel_t),
            alpha = FadeAlpha(progress),
        };
    }

    private void ApplyMotionToIcon(int index, in FlightMotion motion)
    {
        Vector3 final_pos = motion.path_position + offsets[index] * motion.offset_factor;
        RewardFlyIcon icon = icons[index];
        RectTransform rt = icon.Rect;
        rt.position = final_pos;
        rt.localScale = new Vector3(motion.scale, motion.scale, 1f);
        icon.CanvasGroup.alpha = motion.alpha;
    }

    private float FadeAlpha(float progress)
    {
        if (progress < fade_start_t)
        {
            return 1f;
        }
        float range = 1f - fade_start_t;
        if (range <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(1f - (progress - fade_start_t) / range);
    }

    private void HandleComplete()
    {
        Deliver();
        Release();
    }

    private void Deliver()
    {
        if (delivered)
        {
            return;
        }
        delivered = true;
        Action<int> handler = on_arrived;
        if (handler != null)
        {
            handler(amount);
        }
    }

    private void Release()
    {
        active = false;
        on_arrived = null;
        target_rect = null;
        Action<StackFlyRunner> handler = on_released;
        if (handler != null)
        {
            handler(this);
        }
    }
}
}
