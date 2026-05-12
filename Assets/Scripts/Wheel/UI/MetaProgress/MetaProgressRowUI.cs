using System;
using System.Collections;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetaProgressRowUI : MonoBehaviour
{
    private const byte glow_alpha = 70;
    private const float shake_speed = 60f;

    [SerializeField] private Image weaponIcon;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image barFill;
    [SerializeField] private TMP_Text amountLabel;
    [SerializeField] private GameObject unlockedBadge;
    [SerializeField] private TMP_Text unlockedBadgeLabel;
    [SerializeField] private RectTransform rowRoot;
    [SerializeField] private float animDuration = 0.6f;

    [SerializeField] private Image rarityEdge;
    [SerializeField] private Image iconGlow;
    [SerializeField] private Image iconFrame;
    [SerializeField] private Image border;
    [SerializeField] private Image flashOverlay;
    [SerializeField] private Image barHighlight;

    private WeaponProgressDefinition def;
    private int displayed_points;
    private int anim_from_points;
    private int anim_to_points;
    private float anim_elapsed;
    private bool animating;
    private Action anim_on_complete;
    private Tween pulse_tween;

    public WeaponProgressDefinition Definition => def;

    public void Bind(WeaponProgressDefinition d, RewardDefinition reward, int currentPoints)
    {
        def = d;
        if (d == null) return;

        Color32 rarity = RarityColor(reward);

        if (weaponIcon != null)
        {
            weaponIcon.sprite = reward == null ? null : (reward.wheelIcon != null ? reward.wheelIcon : reward.icon);
            weaponIcon.preserveAspect = true;

            weaponIcon.color = Color.white;
        }
        if (nameLabel != null) nameLabel.text = d.displayName;

        if (rarityEdge != null) rarityEdge.color = rarity;
        if (barFill   != null) barFill.color   = rarity;
        if (iconGlow  != null)
        {

            Color32 g = rarity; g.a = glow_alpha; iconGlow.color = g;
        }

        if (border != null)
        {
            Color32 b = rarity; b.a = 0; border.color = b;
        }
        if (flashOverlay != null)
        {
            Color32 f = flashOverlay.color; f.a = 0; flashOverlay.color = f;
        }
        if (unlockedBadgeLabel != null)
            unlockedBadgeLabel.color = rarity;

        displayed_points = currentPoints;
        animating = false;
        SetAmount(currentPoints);
        SetBarFill(currentPoints);
        if (unlockedBadge != null) unlockedBadge.SetActive(currentPoints >= d.requiredPoints);
    }

    private static Color32 RarityColor(RewardDefinition reward)
    {
        if (reward == null) return new Color32(0x8A, 0x90, 0x9A, 255);
        switch (reward.minZoneTier)
        {
            case RewardTier.Super: return new Color32(0xF4, 0x8A, 0x3A, 255);
            case RewardTier.Safe:  return new Color32(0x4C, 0x9C, 0xF0, 255);
            default:               return new Color32(0x8A, 0x90, 0x9A, 255);
        }
    }

    public void AnimateTo(int oldPoints, int newPoints, Action onComplete = null)
    {
        if (def == null) { onComplete?.Invoke(); return; }
        if (newPoints == oldPoints)
        {
            SetAmount(newPoints);
            SetBarFill(newPoints);
            onComplete?.Invoke();
            return;
        }

        anim_on_complete = onComplete;
        anim_from_points = oldPoints;
        anim_to_points = newPoints;
        anim_elapsed = 0f;
        animating = true;
        enabled = true;

        PlayPulse();
    }

    void Update()
    {
        if (!animating || def == null) { enabled = false; return; }
        anim_elapsed += Time.deltaTime;
        float t = animDuration <= 0f ? 1f : Mathf.Clamp01(anim_elapsed / animDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        int v = Mathf.RoundToInt(Mathf.Lerp(anim_from_points, anim_to_points, eased));
        if (v != displayed_points)
        {
            displayed_points = v;
            SetAmount(v);
            SetBarFill(v);
        }
        if (t >= 1f)
        {
            animating = false;
            displayed_points = anim_to_points;
            SetAmount(anim_to_points);
            SetBarFill(anim_to_points);
            bool wasUnlocked = anim_from_points >= def.requiredPoints;
            bool nowUnlocked = anim_to_points >= def.requiredPoints;
            if (unlockedBadge != null) unlockedBadge.SetActive(nowUnlocked);
            if (!wasUnlocked && nowUnlocked) PlayPulse();
            var cb = anim_on_complete;
            anim_on_complete = null;
            cb?.Invoke();
        }
    }

    private void SetAmount(int current)
    {
        if (amountLabel == null || def == null) return;

        amountLabel.SetText("{0} / {1}", current, def.requiredPoints);
    }

    private void SetBarFill(int current)
    {
        if (barFill == null || def == null || def.requiredPoints <= 0) return;
        float t = Mathf.Clamp01((float)current / def.requiredPoints);
        Vector3 s = barFill.transform.localScale;
        s.x = t;
        barFill.transform.localScale = s;
    }

    public void PlayCompletionAndExit(string logId, Action onDone)
    {
        DebugLogger.Log($"[MetaProgressPanel] SHOW_UNLOCK rewardId={logId}");

        if (flashOverlay != null)
        {
            Sequence.Create()
                .Chain(Tween.Alpha(flashOverlay, 0f, 0.85f, 0.06f))
                .Chain(Tween.Alpha(flashOverlay, 0.85f, 0f, 0.14f));
        }

        Sequence.Create()
            .ChainDelay(0.06f)
            .ChainCallback(() =>
            {
                if (weaponIcon != null)
                    Tween.PunchScale(weaponIcon.rectTransform, new Vector3(0.18f, 0.18f, 0f), 0.22f);
                if (border != null)
                    Tween.Alpha(border, 0f, 1f, 0.15f);
            })
            .ChainDelay(0.14f)
            .ChainCallback(() =>
            {
                if (unlockedBadge != null)
                {
                    unlockedBadge.SetActive(true);
                    if (unlockedBadgeLabel is Graphic g)
                        Tween.Alpha(g, 0f, 1f, 0.12f);
                    var badgeRT = unlockedBadge.GetComponent<RectTransform>();
                    if (badgeRT != null)
                        Tween.PunchScale(badgeRT, new Vector3(0.25f, 0.25f, 0f), 0.20f);
                }
            })
            .ChainDelay(0.16f)
            .ChainCallback(() =>
            {
                if (rowRoot != null) StartCoroutine(ShakeAnchored(rowRoot, 4f, 0.12f));
            })
            .ChainDelay(0.97f)
            .ChainCallback(() =>
            {
                DebugLogger.Log($"[MetaProgressPanel] FLY_OUT rewardId={logId}");
                if (rowRoot != null)
                    Tween.Scale(rowRoot, Vector3.zero, 0.35f, Ease.InQuad);
            })
            .ChainDelay(0.35f)
            .ChainCallback(() =>
            {
                if (rowRoot != null)
                {
                    rowRoot.localScale = Vector3.one;
                    rowRoot.anchoredPosition = Vector2.zero;
                }
                if (border != null)       { Color32 c = border.color; c.a = 0; border.color = c; }
                if (flashOverlay != null) { Color32 c = flashOverlay.color; c.a = 0; flashOverlay.color = c; }
                if (unlockedBadge != null) unlockedBadge.SetActive(false);
                onDone?.Invoke();
            });
    }

    private static IEnumerator ShakeAnchored(RectTransform rt, float magnitude, float dur)
    {
        if (rt == null || dur <= 0f) yield break;
        Vector2 origin = rt.anchoredPosition;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / dur);
            float dx = Mathf.Sin(t * shake_speed) * magnitude * k;
            rt.anchoredPosition = origin + new Vector2(dx, 0f);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    private void PlayPulse()
    {
        if (rowRoot == null) return;
        if (pulse_tween.isAlive) pulse_tween.Stop();
        rowRoot.localScale = Vector3.one * 1.05f;
        pulse_tween = Tween.Scale(rowRoot, Vector3.one, 0.30f, Ease.OutBack);
    }
}
