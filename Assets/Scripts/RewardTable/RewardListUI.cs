using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
public class RewardListUI : MonoBehaviour
{
    [SerializeField] private RunSession controller;
    [SerializeField] private RectTransform container;
    [SerializeField] private RewardListItemUI item_prefab;

    [SerializeField] private ScrollRect scroll_rect;

    private List<RewardListItemUI> active_items = new List<RewardListItemUI>();
    private ObjectPool<RewardListItemUI> row_pool;
    private RunEventPass event_pass;

    internal void PrepareFlyTarget(RewardDefinition reward)
    {
        bool acquired_new_row;
        RewardListItemUI item = FindOrAcquireRow(reward, out acquired_new_row);
        if (acquired_new_row)
        {
            item.PrepareTarget(reward);
            ShowNewRow(item);
        }

        RefreshLayoutForTargeting();
    }

    internal RectTransform GetFlyTarget(RewardDefinition reward)
    {
        RewardListItemUI item = FindActiveItem(reward);
        return item.IconRectTransform;
    }

    private RewardListItemUI FindActiveItem(RewardDefinition reward)
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
        return null;
    }

    void Awake()
    {
        event_pass = new RunEventPass(controller.Events);
        row_pool = new ObjectPool<RewardListItemUI>(item_prefab, container, 0);
    }

    void OnEnable()
    {
        event_pass.Subscribe<RunPendingClearedEvent>(HandleRunCleared);
        event_pass.Subscribe<ExitFlowStateChangedEvent>(HandleExitStateChanged);
    }

    void OnDisable()
    {
        event_pass.ReleaseAll();
    }

    private void HandleExitStateChanged(ExitFlowStateChangedEvent evt)
    {
        scroll_rect.enabled = evt.current_state == ExitFlowState.None;
    }

    internal void ApplyEarnedReward(RewardDefinition reward, int total_amt, Action count_complete)
    {
        bool acquired_new_row;
        RewardListItemUI item = FindOrAcquireRow(reward, out acquired_new_row);
        item.SetData(reward, total_amt, count_complete);
        if (acquired_new_row)
        {
            ShowNewRow(item);
        }
    }

    private RewardListItemUI FindOrAcquireRow(RewardDefinition reward, out bool acquired_new_row)
    {
        RewardListItemUI item = FindActiveItem(reward);
        acquired_new_row = item == null;
        if (acquired_new_row)
        {
            return AcquireRow();
        }

        item.SetVisible(true);
        return item;
    }

    private RewardListItemUI AcquireRow()
    {
        row_pool.EnsureCapacity(active_items.Count + 1);
        RewardListItemUI item = row_pool.Acquire();
        active_items.Add(item);
        ConfigureRow(item);
        return item;
    }

    private void ShowNewRow(RewardListItemUI item)
    {
        item.SetVisible(true);
        SortActiveRowsByCategory();
        PinScrollToTop();
        RefreshScrollEnabledState();
    }

    private void ConfigureRow(RewardListItemUI item)
    {
        item.SetCurrencyConfig(controller.CurrencyConfig);
        item.ConfigureTiming(controller.RewardListItemTiming);
    }

    private void SortActiveRowsByCategory()
    {
        active_items.Sort(CompareByCategoryPriority);

        for (int i = 0; i < active_items.Count; i++)
        {
            active_items[i].transform.SetSiblingIndex(i);
        }
    }

    private static int CompareByCategoryPriority(RewardListItemUI a, RewardListItemUI b)
    {
        return a.CategoryPriority.CompareTo(b.CategoryPriority);
    }

    void HandleRunCleared(RunPendingClearedEvent _)
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

    private void RefreshLayoutForTargeting()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        Canvas.ForceUpdateCanvases();
    }

    private void RefreshScrollEnabledState()
    {
        if (active_items.Count == 0)
        {
            PinScrollToTop();
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
