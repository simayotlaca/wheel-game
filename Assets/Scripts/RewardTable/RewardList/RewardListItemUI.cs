using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VertigoWheel
{
public class RewardListItemUI : MonoBehaviour
{
    private enum AmountTextMode
    {
        Plain,
        Compact
    }

    [SerializeField] private Image icon_image;
    [SerializeField] private TMP_Text reward_amount_value;

    private RewardDefinition reward_definition;
    private int last_written_amount;
    private AmountTextMode last_amount_text_mode;
    private int tween_target_amount;
    private float next_text_pulse_time;
    private ConfigAnimation anim_config;
    private CurrencyConfig currency_config;
    private RewardTableConfig.CategorySortPriorities sort_priorities;
    private Vector3 amount_rest_scale = Vector3.one;

    private Tween count_tween;
    private System.Action<float> on_count_tween_value;
    private System.Action on_count_complete;

    public RewardDefinition Reward => reward_definition;

    public int CategoryPriority { get; private set; }

    public Vector3 IconWorldPosition => icon_image.transform.position;

    void Awake()
    {
        amount_rest_scale = reward_amount_value.transform.localScale;

        on_count_tween_value = OnCountTweenValue;
        on_count_complete = OnCountTweenComplete;
    }

    public void SetCurrencyConfig(CurrencyConfig config)
    {
        currency_config = config;
    }

    public void SetAnimationConfig(ConfigAnimation config)
    {
        anim_config = config;
    }

    public void SetSortPriorities(RewardTableConfig.CategorySortPriorities priorities)
    {
        sort_priorities = priorities;
    }

    //while it counts up i show normal numbers, feels easier to read while moving
    //then i switch to compact text at the end because long numbers get ugly
    public void SetData(RewardDefinition reward, int amount)
    {
        if (reward == null)
        {
            gameObject.SetActive(false);
        }
        else
        {
            bool reward_changed = reward_definition != reward;
            if (reward_changed)
            {
                reward_definition = reward;
                CategoryPriority = GameRules.ResolveRewardListPriority(reward, currency_config, sort_priorities);
                Sprite spr = reward.icon;
                if (reward.listIcon != null)
                {
                    spr = reward.listIcon;
                }
                icon_image.sprite = spr;
                icon_image.enabled = spr != null;

                RewardListVisualLayout layout = anim_config.ResolveRewardListLayout(reward);
                icon_image.rectTransform.sizeDelta = layout.iconSize;
                icon_image.rectTransform.anchoredPosition = layout.iconAnchoredPos;
                reward_amount_value.rectTransform.anchoredPosition = layout.amountAnchoredPos;
                reward_amount_value.fontSize = layout.amountFontSize;

                count_tween.Stop();
                last_written_amount = 0;
                next_text_pulse_time = 0f;
            }

            if (amount != last_written_amount)
            {
                count_tween.Stop();
                tween_target_amount = amount;

                count_tween = Tween.Custom((float)last_written_amount, (float)amount, duration: anim_config.rewardAmountCountUpDuration, onValueChange: on_count_tween_value)
                    .OnComplete(on_count_complete);
            }
        }
    }

    void OnDisable()
    {
        count_tween.Stop();
        Tween.StopAll(onTarget: reward_amount_value.transform);
        reward_amount_value.transform.localScale = amount_rest_scale;
    }

    private void OnCountTweenValue(float val)
    {
        WriteAmountTweenValue(Mathf.RoundToInt(val));
    }

    private void OnCountTweenComplete()
    {
        WriteAmountFinalValue(tween_target_amount);
    }

    void WriteAmountTweenValue(int value)
    {
        WriteAmountValue(value, AmountTextMode.Plain);
    }

    void WriteAmountFinalValue(int value)
    {
        WriteAmountValue(value, AmountTextMode.Compact);
    }

    private void WriteAmountValue(int value, AmountTextMode mode)
    {
        if (value != last_written_amount || mode != last_amount_text_mode)
        {
            bool is_incremented = value > last_written_amount;
            last_written_amount = value;
            last_amount_text_mode = mode;

            if (mode == AmountTextMode.Compact)
            {
                reward_amount_value.text = NumberFormatter.FormatThousands(value);
            }
            else
            {
                reward_amount_value.SetText("{0}", value);
            }

            if (is_incremented)
            {
                TryFireAmountPunch(value);
            }
        }
    }

    //i rate limit this punch because fast ticks were restarting it too much
    void TryFireAmountPunch(int value)
    {
        if (value > 0)
        {
            float now = Time.time;
            if (now >= next_text_pulse_time)
            {
                next_text_pulse_time = now + anim_config.rewardTextTickPulseMinInterval;
                float duration = anim_config.rewardTextTickPulseDuration;

                float mag = anim_config.ResolveRewardTextTickPunchStrength();
                if (mag > 0f && duration > 0f)
                {
                    Transform tt = reward_amount_value.transform;
                    Tween.StopAll(onTarget: tt);
                    Tween.PunchScale(tt, new Vector3(mag, mag, 0f), duration);
                }
            }
        }
    }

    public void Clear()
    {
        reward_definition = null;
        CategoryPriority = GameRules.ResolveRewardListPriority(null, currency_config, sort_priorities);
        last_written_amount = 0;
        next_text_pulse_time = 0f;
        count_tween.Stop();
        icon_image.enabled = false;

        Tween.StopAll(onTarget: reward_amount_value.transform);
        reward_amount_value.transform.localScale = amount_rest_scale;
        reward_amount_value.SetText("");
    }
}
}
