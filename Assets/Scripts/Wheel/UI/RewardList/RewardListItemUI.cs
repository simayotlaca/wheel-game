using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardListItemUI : MonoBehaviour
{
    private const int default_priority = 99;
    private const float not_pulsed_yet = -999f;

    [SerializeField] private WheelAnimationConfig animConfig;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image iconFrame;
    [SerializeField] private TMP_Text rewardName_value;
    [SerializeField] private TMP_Text rewardAmount_value;

    private string _rewardId;
    private string _amountFormat = "{0}";
    private int _lastWrittenAmount;
    private float _lastTextPulseTime = not_pulsed_yet;
    private CurrencyConfig currency_config;

    private IntCountUpTween count_tween;
    public bool IsAnimating => count_tween.IsActive;

    public string RewardId => _rewardId;

    public int CategoryPriority { get; private set; } = default_priority;

    public Sprite IconSprite => iconImage != null ? iconImage.sprite : null;
    public Vector3 IconWorldPosition => iconImage != null
        ? iconImage.transform.position
        : transform.position;

    public Transform ReceiveTransform
    {
        get
        {
            if (iconFrame != null) return iconFrame.transform;
            if (iconImage != null) return iconImage.transform;
            return transform;
        }
    }

    void Awake()
    {
        if (animConfig == null)
            throw new System.InvalidOperationException("RewardListItemUI: animConfig not wired.");
        if (iconImage == null)          Debug.LogError("RewardListItemUI: iconImage not wired.", this);
        if (rewardAmount_value == null) Debug.LogError("RewardListItemUI: rewardAmount_value not wired.", this);

        enabled = false;
    }

    public void SetCurrencyConfig(CurrencyConfig config)
    {
        if (config == null)
            throw new System.ArgumentNullException(nameof(config));
        currency_config = config;
    }

    public void SetData(RewardDefinition reward, int amount)
    {
        if (reward == null) { gameObject.SetActive(false); return; }

        bool idChanged = _rewardId != reward.rewardId;
        if (idChanged)
        {
            _rewardId = reward.rewardId;
            _amountFormat = reward.displayAsMultiplier ? "x{0}" : "{0}";
            CategoryPriority = ResolveCategoryPriority(reward);
            if (iconImage != null)
            {
                Sprite spr = reward.listIcon != null ? reward.listIcon
                           : reward.wheelIcon != null ? reward.wheelIcon
                           : reward.icon;
                iconImage.sprite = spr;
                iconImage.enabled = spr != null;
                ResetIconLayout();
            }
            if (iconFrame != null) iconFrame.enabled = false;
            if (rewardName_value != null) rewardName_value.text = reward.displayName;
            count_tween = default;
            _lastWrittenAmount = 0;
            _lastTextPulseTime = not_pulsed_yet;
        }

        if (count_tween.SetTarget(amount, Time.unscaledTime))
            WriteAmount(count_tween.Current);
        if (count_tween.IsActive) enabled = true;
    }

    public void Reserve(RewardDefinition reward) { SetData(reward, 0); }

    void Update()
    {
        if (count_tween.Tick(Time.unscaledTime, animConfig.countUpDuration))
            WriteAmount(count_tween.Current);
        if (!count_tween.IsActive) enabled = false;
    }

    void OnDisable()
    {
        if (rewardAmount_value != null)
        {
            Tween.StopAll(onTarget: rewardAmount_value.transform);
            rewardAmount_value.transform.localScale = Vector3.one;
        }
        if (iconFrame != null)
        {
            Tween.StopAll(onTarget: iconFrame.transform);
            iconFrame.transform.localScale = Vector3.one;
        }
    }

    const float IconBoxSize = 104f;

    void ResetIconLayout()
    {
        if (iconImage == null) return;
        RectTransform rt = iconImage.rectTransform;
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.sizeDelta = new Vector2(IconBoxSize, IconBoxSize);
        iconImage.preserveAspect = true;
    }

    void WriteAmount(int value)
    {
        bool incremented = value > _lastWrittenAmount;
        _lastWrittenAmount = value;

        if (rewardAmount_value != null)
        {

            rewardAmount_value.SetText(_amountFormat, value);
        }

        if (incremented && rewardAmount_value != null && value > 0)
        {
            float minInterval = animConfig.textTickPulseMinInterval;
            float now = Time.unscaledTime;
            if (now - _lastTextPulseTime >= minInterval)
            {
                _lastTextPulseTime = now;
                float peak     = animConfig.textTickPulseScale;
                float duration = animConfig.textTickPulseDuration;
                const float PunchCurvePeak = 0.58f;

                float mag = Mathf.Max(0f, (peak - 1f) / PunchCurvePeak);
                if (mag > 0f && duration > 0f)
                {
                    Transform tt = rewardAmount_value.transform;
                    Tween.StopAll(onTarget: tt);
                    Tween.PunchScale(tt, new Vector3(mag, mag, 0f), duration);
                }
            }
        }

    }

    int ResolveCategoryPriority(RewardDefinition r)
    {
        if (r == null) return default_priority;
        if (currency_config == null)
            throw new System.InvalidOperationException("RewardListItemUI: CurrencyConfig not assigned.");
        if (!string.IsNullOrEmpty(r.rewardId))
        {
            if (r.rewardId == currency_config.goldCurrencyId) return 0;
            if (r.rewardId == currency_config.cashCurrencyId) return 1;
        }
        switch (r.visualCategory)
        {
            case RewardVisualCategory.Coin:       return 0;
            case RewardVisualCategory.Cash:       return 1;
            case RewardVisualCategory.Weapon:     return 2;
            case RewardVisualCategory.Character:  return 3;
            case RewardVisualCategory.Compact:
            case RewardVisualCategory.Chest:
            case RewardVisualCategory.Consumable:
            case RewardVisualCategory.Cosmetic:   return 4;
            default:                              return 5;
        }
    }

    public void Clear()
    {
        _rewardId = null;
        _amountFormat = "{0}";
        CategoryPriority = default_priority;
        _lastWrittenAmount = 0;
        _lastTextPulseTime = not_pulsed_yet;
        count_tween = default;
        if (iconImage != null) iconImage.enabled = false;

        if (rewardAmount_value != null)
        {
            Tween.StopAll(onTarget: rewardAmount_value.transform);
            rewardAmount_value.transform.localScale = Vector3.one;
        }
        if (iconFrame != null) iconFrame.transform.localScale = Vector3.one;
        enabled = false;
    }
}
