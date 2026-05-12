using UnityEngine;
using UnityEngine.UI;

public class RewardListUI : MonoBehaviour
{
    private const int default_priority = 99;

    [SerializeField] private WheelController controller;
    [SerializeField] private RectTransform container;
    [SerializeField] private RewardListItemUI itemPrefab;

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private int maxPoolSize = 16;

    private RewardItemPool pool;

    private bool deferred_mode;

    public int ActiveCount => pool != null ? pool.ActiveCount : 0;
    public RewardListItemUI GetActiveItem(int index) => pool != null ? pool.GetActive(index) : null;
    public void HideAll() { if (pool != null) pool.ReleaseAll(); }
    public RectTransform Container => container;

    public void SetDeferredUpdate(bool deferred) { deferred_mode = deferred; }

    public bool TryGetRewardIconPoint(string rewardId, out Vector3 world)
    {
        RewardListItemUI it = FindActiveItem(rewardId);
        if (it == null) { world = Vector3.zero; return false; }
        world = it.IconWorldPosition;
        return true;
    }

    private readonly Vector3[] _viewportCornersScratch = new Vector3[4];

    public bool TryGetOverflowFlyTarget(out Vector3 world)
    {
        world = Vector3.zero;
        if (scrollRect == null || scrollRect.viewport == null) return false;
        scrollRect.viewport.GetWorldCorners(_viewportCornersScratch);

        world = (_viewportCornersScratch[0] + _viewportCornersScratch[3]) * 0.5f;
        return true;
    }

    public bool IsRowIconOffscreen(float worldY)
    {
        if (scrollRect == null || scrollRect.viewport == null) return false;
        scrollRect.viewport.GetWorldCorners(_viewportCornersScratch);
        float bottomY = _viewportCornersScratch[0].y;
        float topY    = _viewportCornersScratch[1].y;

        const float Eps = 1f;
        return worldY < bottomY - Eps || worldY > topY + Eps;
    }

    private RewardListItemUI FindActiveItem(string rewardId)
    {
        if (pool == null || string.IsNullOrEmpty(rewardId)) return null;
        int n = pool.ActiveCount;
        for (int i = 0; i < n; i++)
        {
            RewardListItemUI it = pool.GetActive(i);
            if (it != null && it.RewardId == rewardId) return it;
        }
        return null;
    }

    void Awake()
    {
        if (controller == null) throw new System.InvalidOperationException("RewardListUI: controller not wired.");
        if (controller.Config == null) throw new System.InvalidOperationException("RewardListUI: WheelController config not assigned.");
        if (controller.Config.currencyConfig == null) throw new System.InvalidOperationException("RewardListUI: CurrencyConfig not assigned in WheelConfig.");
        if (container == null)  Debug.LogError("RewardListUI: container not wired.", this);
        if (itemPrefab == null) Debug.LogError("RewardListUI: itemPrefab not wired.", this);
        pool = new RewardItemPool(itemPrefab, container, maxPoolSize);
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnRewardEarned += HandleRewardEarned;
            controller.OnRunEnded += HandleRunEnded;
            controller.OnRewardsBanked += HandleRewardsBanked;
        }
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnRewardEarned -= HandleRewardEarned;
            controller.OnRunEnded -= HandleRunEnded;
            controller.OnRewardsBanked -= HandleRewardsBanked;
        }
    }

    void HandleRewardEarned(SpinResult result, SliceDefinition slice)
    {
        if (deferred_mode) return;
        if (slice == null || slice.reward == null) return;
        if (controller == null || controller.Inventory == null) return;
        if (!controller.Inventory.Pending.TryGetValue(slice.reward.rewardId, out int totalAmount)) return;
        ApplyEarnedReward(slice.reward, totalAmount);
    }

    public RewardListItemUI ReserveOrGetRow(RewardDefinition reward)
    {
        if (pool == null || reward == null || string.IsNullOrEmpty(reward.rewardId)) return null;

        int active = pool.ActiveCount;
        for (int i = 0; i < active; i++)
        {
            RewardListItemUI existing = pool.GetActive(i);
            if (existing != null && existing.RewardId == reward.rewardId) return existing;
        }

        RewardListItemUI item = pool.Acquire();
        if (item != null)
        {
            item.SetCurrencyConfig(controller.Config.currencyConfig);
            item.Reserve(reward);
        }
        SortActiveRowsByCategory();
        PinScrollToTop();
        return item;
    }

    public void ApplyEarnedReward(RewardDefinition reward, int totalAmount)
    {
        if (pool == null || reward == null) return;
        if (string.IsNullOrEmpty(reward.rewardId)) return;

        int active = pool.ActiveCount;
        for (int i = 0; i < active; i++)
        {
            RewardListItemUI existing = pool.GetActive(i);
            if (existing != null && existing.RewardId == reward.rewardId)
            {
                existing.SetData(reward, totalAmount);
                return;
            }
        }

        RewardListItemUI item = pool.Acquire();
        if (item != null)
        {
            item.SetCurrencyConfig(controller.Config.currencyConfig);
            item.SetData(reward, totalAmount);
        }
        SortActiveRowsByCategory();

        PinScrollToTop();
    }

    private int[] _sortIdxScratch;
    private int[] _sortPrioScratch;
    private void SortActiveRowsByCategory()
    {
        if (pool == null) return;
        int n = pool.ActiveCount;
        if (n <= 1) return;

        if (_sortIdxScratch == null || _sortIdxScratch.Length < n)
        {
            _sortIdxScratch = new int[Mathf.Max(maxPoolSize, n)];
            _sortPrioScratch = new int[Mathf.Max(maxPoolSize, n)];
        }

        for (int i = 0; i < n; i++)
        {
            _sortIdxScratch[i] = i;
            RewardListItemUI it = pool.GetActive(i);
            _sortPrioScratch[i] = it != null ? it.CategoryPriority : default_priority;
        }

        for (int i = 1; i < n; i++)
        {
            int kp = _sortPrioScratch[i];
            int ki = _sortIdxScratch[i];
            int j = i - 1;
            while (j >= 0 && _sortPrioScratch[j] > kp)
            {
                _sortPrioScratch[j + 1] = _sortPrioScratch[j];
                _sortIdxScratch[j + 1]  = _sortIdxScratch[j];
                j--;
            }
            _sortPrioScratch[j + 1] = kp;
            _sortIdxScratch[j + 1]  = ki;
        }

        bool reorderNeeded = false;
        for (int sortedPos = 0; sortedPos < n; sortedPos++)
        {
            RewardListItemUI it = pool.GetActive(_sortIdxScratch[sortedPos]);
            if (it == null)
            {
                continue;
            }

            int currentSibling = it.transform.GetSiblingIndex();
            if (currentSibling == sortedPos)
            {
                continue;
            }
            else
            {
                reorderNeeded = true;
                break;
            }
        }

        if (reorderNeeded == false)
        {
            return;
        }

        for (int sortedPos = 0; sortedPos < n; sortedPos++)
        {
            RewardListItemUI it = pool.GetActive(_sortIdxScratch[sortedPos]);
            if (it == null)
            {
                continue;
            }
            it.transform.SetSiblingIndex(sortedPos);
        }
    }

    void HandleRunEnded()
    {
        if (pool != null)
            pool.ReleaseAll();
    }

    void HandleRewardsBanked()
    {
        if (pool != null)
            pool.ReleaseAll();
    }

    private void PinScrollToTop()
    {
        if (scrollRect == null) return;
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
