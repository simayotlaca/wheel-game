using System.Collections.Generic;
using UnityEngine;

public class MetaProgressPanel : MonoBehaviour
{
    [SerializeField] private WheelController controller;
    [SerializeField] private RectTransform rowsContainer;
    [SerializeField] private MetaProgressRowUI rowPrefab;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private WeaponProgressDefinition[] definitions;
    [SerializeField] private RewardDefinition[] rewards;
    [SerializeField] private int maxVisibleRows = 3;

    private MetaProgressionService service;
    private readonly Dictionary<WeaponProgressDefinition, MetaProgressRowUI> bound_rows = new Dictionary<WeaponProgressDefinition, MetaProgressRowUI>(3);
    private readonly List<MetaProgressRowUI> free_pool = new List<MetaProgressRowUI>(3);
    private readonly List<WeaponProgressDefinition> active_scratch = new List<WeaponProgressDefinition>(3);
    private readonly List<WeaponProgressDefinition> release_scratch = new List<WeaponProgressDefinition>(3);

    private readonly HashSet<string> completing_reward_ids = new HashSet<string>();
    public bool IsAnimatingCompletion => completing_reward_ids.Count > 0;

    void Awake()
    {
        if (definitions == null) { Debug.LogError("[MetaProgressPanel] definitions array not assigned", this); enabled = false; return; }
        if (rewards     == null) { Debug.LogError("[MetaProgressPanel] rewards array not assigned",     this); enabled = false; return; }

        service = new MetaProgressionService(definitions, rewards);
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnRewardEarned   += HandleRewardEarned;
            controller.OnRunEnded       += HandleRunEnded;
            controller.OnRewardsBanked  += HandleRewardsBanked;
        }
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnRewardEarned   -= HandleRewardEarned;
            controller.OnRunEnded       -= HandleRunEnded;
            controller.OnRewardsBanked  -= HandleRewardsBanked;

            if (controller.MetaBusy) controller.SetMetaBusy(false);
        }
        completing_reward_ids.Clear();
    }

    private void HandleRunEnded()
    {
        ResetForRunEnd();
    }

    private void HandleRewardsBanked()
    {
        ResetForRunEnd();
    }

    private void ResetForRunEnd()
    {
        if (service == null) return;
        service.ResetAll();
        completing_reward_ids.Clear();
        if (controller != null && controller.MetaBusy) controller.SetMetaBusy(false);
        Rebuild();
    }

    void Start()
    {
        Rebuild();
    }

    private void HandleRewardEarned(SpinResult result, SliceDefinition slice)
    {
        if (slice == null || slice.reward == null) return;
        string id = slice.reward.rewardId;
        if (string.IsNullOrEmpty(id)) return;

        int amount = result.amount;
        var def = service.AddProgress(id, amount, out int oldVal, out int newVal);
        if (def == null) return;

        DebugLogger.Log($"[MetaProgressPanel] ACTIVATE rewardId={id} amount={amount} current={newVal} target={def.requiredPoints}");

        Rebuild();

        var row = FindRow(def);
        bool willComplete = newVal >= def.requiredPoints && row != null && !completing_reward_ids.Contains(id);

        if (controller != null) controller.SetMetaBusy(true);

        if (row == null)
        {
            if (controller != null) controller.SetMetaBusy(false);
            return;
        }

        if (willComplete)
        {
            DebugLogger.Log($"[MetaProgressPanel] COMPLETE rewardId={id}");
            completing_reward_ids.Add(id);

            var capturedDef = def;
            var capturedId = id;
            row.AnimateTo(oldVal, newVal, () =>
            {
                row.PlayCompletionAndExit(capturedId, () =>
                {
                    service.ResetAndDeactivate(capturedDef);
                    DebugLogger.Log($"[MetaProgressPanel] RESET_DEACTIVATE rewardId={capturedId}");
                    Rebuild();
                    completing_reward_ids.Remove(capturedId);
                    if (completing_reward_ids.Count == 0 && controller != null)
                        controller.SetMetaBusy(false);
                });
            });
        }
        else
        {
            row.AnimateTo(oldVal, newVal, ClearMetaBusyIfIdle);
        }
    }

    private void ClearMetaBusyIfIdle()
    {
        if (completing_reward_ids.Count == 0 && controller != null)
            controller.SetMetaBusy(false);
    }

    private MetaProgressRowUI FindRow(WeaponProgressDefinition def)
    {
        if (def == null) return null;
        bound_rows.TryGetValue(def, out var r);
        return r;
    }

    private void Rebuild()
    {
        if (rowsContainer == null || rowPrefab == null || service == null)
        {
            SetPanelVisible(false);
            return;
        }

        active_scratch.Clear();
        var active = active_scratch;
        for (int i = 0; i < definitions.Length && active.Count < maxVisibleRows; i++)
        {
            var d = definitions[i];
            if (d == null) continue;
            if (service.IsActiveTarget(d)) active.Add(d);
        }

        release_scratch.Clear();
        foreach (var kv in bound_rows)
        {
            if (!active.Contains(kv.Key)) release_scratch.Add(kv.Key);
        }
        for (int i = 0; i < release_scratch.Count; i++)
        {
            var key = release_scratch[i];
            var row = bound_rows[key];
            bound_rows.Remove(key);
            if (row != null)
            {
                row.gameObject.SetActive(false);
                free_pool.Add(row);
            }
        }

        for (int i = 0; i < active.Count; i++)
        {
            var d = active[i];
            if (bound_rows.ContainsKey(d)) continue;

            MetaProgressRowUI row;
            if (free_pool.Count > 0)
            {
                row = free_pool[free_pool.Count - 1];
                free_pool.RemoveAt(free_pool.Count - 1);
            }
            else
            {
                row = Instantiate(rowPrefab, rowsContainer);
            }
            row.gameObject.SetActive(true);
            row.Bind(d, service.GetRewardFor(d), service.CurrentPoints(d));
            bound_rows[d] = row;
        }

        for (int i = 0; i < active.Count; i++)
        {
            if (bound_rows.TryGetValue(active[i], out var row) && row != null)
                row.transform.SetSiblingIndex(i);
        }

        SetPanelVisible(active.Count > 0);
    }

    private void SetPanelVisible(bool on)
    {
        if (rowsContainer != null && rowsContainer.gameObject.activeSelf != on)
            rowsContainer.gameObject.SetActive(on);
    }
}
