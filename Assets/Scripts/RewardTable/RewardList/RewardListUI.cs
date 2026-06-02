using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
public class RewardListUI : MonoBehaviour
{
    #region setup
    [SerializeField] private RunSession controller;
    [SerializeField] private RectTransform container;
    [SerializeField] private RewardListItemUI item_prefab;

    [SerializeField] private ScrollRect scroll_rect;
    [SerializeField] private RectTransform overflow_fly_target;

    [SerializeField] private RunExitController run_exit_controller;

    [SerializeField] private ConfigAnimation anim_config;

    private readonly List<RewardListItemUI> active_items = new List<RewardListItemUI>();
    private ObjectPool<RewardListItemUI> row_pool;
    #endregion

    public void HideAll()
    {
        ClearRows();
        RefreshScrollEnabledState();
    }

    public bool TryGetFlyTarget(RewardDefinition reward, out Vector3 world)
    {
        world = Vector3.zero;
        if (!ShouldShowInRewardList(reward))
        {
            return false;
        }

        RewardListItemUI item = ReserveOrGetRow(reward);
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        if (item != null && item.IconWorldPosition != Vector3.zero)
        {
            world = item.IconWorldPosition;
            return true;
        }

        return TryGetOverflowFlyTarget(out world);
    }

    private bool TryGetOverflowFlyTarget(out Vector3 world)
    {
        world = Vector3.zero;
        if (overflow_fly_target != null)
        {
            world = overflow_fly_target.position;
            return true;
        }
        return false;
    }

    private RewardListItemUI FindActiveItem(RewardDefinition reward)
    {
        if (reward != null)
        {
            int n = active_items.Count;
            for (int i = 0; i < n; i++)
            {
                RewardListItemUI it = active_items[i];
                if (it.Reward == reward)
                {
                    return it;
                }
            }
        }
        return null;
    }

    void Awake()
    {
        row_pool = new ObjectPool<RewardListItemUI>(item_prefab, container, 1);
    }

    void OnEnable()
    {
        controller.OnRunEnded += HandleRunCleared;
        controller.OnRewardsBanked += HandleRunCleared;
        run_exit_controller.OnStateChanged += HandleExitStateChanged;
    }

    void OnDisable()
    {
        controller.OnRunEnded -= HandleRunCleared;
        controller.OnRewardsBanked -= HandleRunCleared;
        run_exit_controller.OnStateChanged -= HandleExitStateChanged;
    }

    private void HandleExitStateChanged(ExitFlowState state)
    {
        scroll_rect.enabled = state == ExitFlowState.None;
    }

    private bool ShouldShowInRewardList(RewardDefinition reward)
    {
        return reward != null && !string.IsNullOrEmpty(reward.rewardId);
    }

    private RewardListItemUI ReserveOrGetRow(RewardDefinition reward)
    {
        if (ShouldShowInRewardList(reward))
        {
            RewardListItemUI existing = FindActiveItem(reward);
            if (existing == null)
            {
                return AcquireRow(reward, 0);
            }
            return existing;
        }
        return null;
    }

    public void ApplyEarnedReward(RewardDefinition reward, int total_amt)
    {
        if (ShouldShowInRewardList(reward))
        {
            RewardListItemUI existing = FindActiveItem(reward);
            if (existing != null)
            {
                existing.SetData(reward, total_amt);
            }
            else
            {
                AcquireRow(reward, total_amt);
            }
        }
    }

    private RewardListItemUI AcquireRow(RewardDefinition reward, int amount)
    {
        row_pool.EnsureCapacity(active_items.Count + 1);
        RewardListItemUI item = row_pool.Acquire();
        if (item != null)
        {
            active_items.Add(item);
            item.SetAnimationConfig(anim_config);
            item.SetCurrencyConfig(controller.Config.currency_config);
            item.SetSortPriorities(controller.Config.rewardTable.sortPriorities);
            item.SetData(reward, amount);
        }
        SortActiveRowsByCategory();
        PinScrollToTop();
        RefreshScrollEnabledState();
        return item;
    }

    private void SortActiveRowsByCategory()
    {
        active_items.Sort((a, b) => a.CategoryPriority.CompareTo(b.CategoryPriority));

        for (int i = 0; i < active_items.Count; i++)
        {
            active_items[i].transform.SetSiblingIndex(i);
        }
    }

    void HandleRunCleared()
    {
        ClearRows();
        RefreshScrollEnabledState();
    }

    private void ClearRows()
    {
        for (int i = 0; i < active_items.Count; i++)
        {
            active_items[i].Clear();
        }

        row_pool.ReleaseAll();
        active_items.Clear();
    }

    private void PinScrollToTop()
    {
        scroll_rect.velocity = Vector2.zero;
        scroll_rect.verticalNormalizedPosition = 1f;
    }

    //scroll rect layout is on prefab, here i just keep it from drifting when list is empty
    //i didnt want another forced rebuild only to check overflow
    private void RefreshScrollEnabledState()
    {
        if (scroll_rect == null)
        {
            return;
        }

        if (!scroll_rect.vertical)
        {
            scroll_rect.vertical = true;
        }

        if (active_items.Count == 0)
        {
            PinScrollToTop();
            if (container != null)
            {
                Vector2 pos = container.anchoredPosition;
                if (!Mathf.Approximately(pos.y, 0f))
                {
                    pos.y = 0f;
                    container.anchoredPosition = pos;
                }
            }
        }
    }
}
}
