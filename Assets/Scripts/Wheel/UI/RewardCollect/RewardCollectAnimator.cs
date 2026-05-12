using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class RewardCollectAnimator : MonoBehaviour
{
    [SerializeField] private WheelController controller;
    [SerializeField] private RewardListUI rewardList;
    [SerializeField] private Button exitButton;
    [SerializeField] private SpinRewardFlyAnimator spinFlyAnimator;
    [SerializeField] private RectTransform flyContainer;

    [Header("Animation")]
    [SerializeField] private WheelAnimationConfig animConfig;
    [SerializeField] private Vector2 flyIconSize = new Vector2(64f, 64f);
    [SerializeField] private int poolCapacity = 4;

    private RewardFlyIconPool pool;

    private struct FlySlot { public RewardFlyIcon icon; public Action onLand; public bool used; }
    private FlySlot[] fly_slots;
    private Action[] on_fly_land_actions;

    void Awake()
    {
        if (animConfig == null)
        {
            Debug.LogError("RewardCollectAnimator: animConfig is not assigned.", this);
            enabled = false;
            return;
        }

        Transform container;
        if (flyContainer != null)
            container = flyContainer;
        else
            container = transform;

        int cap = Mathf.Max(1, poolCapacity);
        pool = new RewardFlyIconPool(container, cap);
        fly_slots = new FlySlot[cap];
        on_fly_land_actions = new Action[cap];
        for (int i = 0; i < cap; i++)
        {
            int captured = i;
            on_fly_land_actions[i] = () => OnFlyLanded(captured);
        }
    }

    void Start()
    {
        if (exitButton != null) exit_cg = exitButton.GetComponent<CanvasGroup>();
    }

    ExitVisibility ResolveExitVisibility()
    {
        bool flyBusy = spinFlyAnimator != null && spinFlyAnimator.IsBusy;
        return ExitContext.ResolveVisibility(controller, flyBusy);
    }

    void OnEnable()
    {
        if (pool != null) pool.DeactivateAll();
    }

    void OnDisable()
    {

        if (fly_slots != null)
        {
            for (int i = 0; i < fly_slots.Length; i++)
            {
                if (fly_slots[i].icon != null)
                    Tween.StopAll(onTarget: fly_slots[i].icon.transform);
            }
        }
        if (pool != null) pool.DeactivateAll();
    }

    [SerializeField] private float disabledAlpha = 0.4f;
    [SerializeField] private float fadeDuration = 0.18f;
    private CanvasGroup exit_cg;

    void Update()
    {
        if (exitButton == null) return;
        GameObject go = exitButton.gameObject;
        ExitVisibility v = ResolveExitVisibility();
        bool active = v != ExitVisibility.Hidden;
        if (go.activeSelf != active) go.SetActive(active);
        if (!active) return;
        bool isNormal = v == ExitVisibility.Normal;
        if (exitButton.interactable != isNormal) exitButton.interactable = isNormal;

        if (exit_cg == null) return;
        if (exit_cg.blocksRaycasts != isNormal) exit_cg.blocksRaycasts = isNormal;
        float target = isNormal ? 1f : disabledAlpha;
        if (!Mathf.Approximately(exit_cg.alpha, target))
        {
            float step = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;
            exit_cg.alpha = Mathf.MoveTowards(exit_cg.alpha, target, step);
        }
    }

    public void PlayCollectAndLeave()
    {
        if (controller == null || !controller.CanLeave) return;

        if (rewardList != null) rewardList.HideAll();

        controller.RequestLeave();
        controller.FinishCollecting();
    }

    public void FlyOne(Sprite sprite, Vector3 startWorld, RectTransform target, Action onLand)
    {
        if (pool == null || target == null || sprite == null) { onLand?.Invoke(); return; }

        RewardFlyIcon fly = pool.Acquire();
        if (fly == null) { onLand?.Invoke(); return; }

        fly.Configure(sprite, flyIconSize);
        RectTransform rt = fly.transform as RectTransform;
        if (rt == null) { pool.Release(fly); onLand?.Invoke(); return; }

        rt.SetAsLastSibling();
        rt.position = startWorld;

        int slot = AcquireFlySlot(fly, onLand);
        if (slot < 0)
        {
            pool.Release(fly);
            onLand?.Invoke();
            return;
        }
        float fd = animConfig.currencyFlyDuration;
        rt.localScale = Vector3.one;
        float landScale = animConfig.flyLandScale;
        Tween.Position(rt, target.position, fd, Ease.InOutQuad).OnComplete(on_fly_land_actions[slot]);
        Tween.Scale(rt, new Vector3(landScale, landScale, 1f), fd, Ease.OutQuad);
    }

    int AcquireFlySlot(RewardFlyIcon icon, Action onLand)
    {
        for (int i = 0; i < fly_slots.Length; i++)
        {
            if (fly_slots[i].used) continue;
            fly_slots[i].icon = icon;
            fly_slots[i].onLand = onLand;
            fly_slots[i].used = true;
            return i;
        }
        return -1;
    }

    void OnFlyLanded(int i)
    {
        if (i < 0 || i >= fly_slots.Length || !fly_slots[i].used) return;
        RewardFlyIcon icon = fly_slots[i].icon;
        Action onLand = fly_slots[i].onLand;
        fly_slots[i].icon = null;
        fly_slots[i].onLand = null;
        fly_slots[i].used = false;
        if (icon != null && pool != null) pool.Release(icon);
        onLand?.Invoke();
    }

}
