#if UNITY_EDITOR
using UnityEngine;

internal static class WheelConfigSceneBuilder
{

    public static WheelConfig Build()
    {
        MakeRewards();
        Zones zones = MakeZones();
        return MakeWheelConfig(zones);
    }

    private struct Rewards
    {
        public RewardDefinition death, gold, cash, armor, knife, rifle,
                                smg, sniper, chestS, chestB, chestSuper;
    }

    private static Rewards MakeRewards() => new Rewards
    {
        death       = Reward("death",       "DEATH",        true,  "ui_card_icon_death",                              RewardVisualCategory.Death),
        gold       = Reward("gold",       "Gold",        false, "UI_icon_gold",                                    RewardVisualCategory.Coin),
        cash       = Reward("cash",       "Cash",        false, "UI_icon_cash",                                    RewardVisualCategory.Cash),
        armor      = Reward("armor",      "Armor",       false, "UI_Icons_Armor_Points",        RewardVisualCategory.Compact),
        knife      = Reward("knife",      "Knife",       false, "UI_Icons_Knife_Points",        RewardVisualCategory.Weapon),
        rifle      = Reward("rifle",      "Rifle",       false, "UI_Icons_Rifle_Points",        RewardVisualCategory.Weapon),
        smg        = Reward("smg",        "SMG",         false, "UI_Icons_SMG_Points",          RewardVisualCategory.Weapon),
        sniper     = Reward("sniper",     "Sniper",      false, "UI_Icons_Sniper_Points",       RewardVisualCategory.Weapon),
        chestS     = Reward("chestS",     "Small Chest", false, "ui_icon_chest_small_noligt",                      RewardVisualCategory.Chest),
        chestB     = Reward("chestB",     "Big Chest",   false, "ui_icon_chest_big_nolight",                       RewardVisualCategory.Chest),
        chestSuper = Reward("chestSuper", "Super Chest", false, "ui_icon_chest_super_nolight",                     RewardVisualCategory.Chest),
    };

    private static RewardDefinition Reward(string id, string display, bool isDeath, string sprite, RewardVisualCategory category)
    {
        string folder = FolderForVisualCategory(category);
        string dir = $"{WheelSceneSetup.CONFIGS}/Rewards/{folder}";
        WheelSceneSetup.EnsureDir(dir);
        string path = $"{dir}/Reward_{id}.asset";
        var r = WheelSceneSetup.Load<RewardDefinition>(path) ?? WheelSceneSetup.Create<RewardDefinition>(path);
        r.rewardId = id;
        r.displayName = display;
        r.isDeath = isDeath;
        r.icon = WheelSceneSetup.Spr(sprite);
        r.visualCategory = category;
        WheelSceneSetup.Dirty(r);
        return r;
    }

    private struct Zones { public ZoneConfig normal, safe, super; }

    private static Zones MakeZones()
    {
        var normal = ZoneCfg("NormalZone", ZoneType.Normal, "",
            WheelSceneSetup.Spr("ui_spin_bronze_base"), null, WheelSceneSetup.Spr("ui_spin_bronze_indicator"),
            Color.white);

        var safe = ZoneCfg("SafeZone", ZoneType.Safe, "SAFE SPIN",
            WheelSceneSetup.Spr("ui_spin_silver_base"), null, WheelSceneSetup.Spr("ui_spin_silver_indicator"),
            new Color(0.82f, 0.91f, 1f));

        var zuper = ZoneCfg("SuperZone", ZoneType.Super, "SUPER SPIN",
            WheelSceneSetup.Spr("ui_spin_golden_base"), null, WheelSceneSetup.Spr("ui_spin_golden_indicator"),
            new Color(1f, 0.92f, 0.5f),
            subtitle: "Up To x10 Rewards");

        return new Zones { normal = normal, safe = safe, super = zuper };
    }

    private static ZoneConfig ZoneCfg(string name, ZoneType type, string header,
        Sprite wheelBase, Sprite wheelFrame, Sprite wheelIndicator,
        Color tint, string subtitle = null)
    {
        string dir = $"{WheelSceneSetup.CONFIGS}/Zones";
        WheelSceneSetup.EnsureDir(dir);
        string path = $"{dir}/{name}.asset";
        var z = WheelSceneSetup.Load<ZoneConfig>(path) ?? WheelSceneSetup.Create<ZoneConfig>(path);
        z.type = type;
        z.headerLabel = header;
        z.subtitle = subtitle;
        z.wheelBase = wheelBase;
        z.wheelFrame = wheelFrame;
        z.wheelIndicator = wheelIndicator;
        z.frameTint = tint;

        WheelSceneSetup.Dirty(z);
        return z;
    }

    private static WheelConfig MakeWheelConfig(Zones zones)
    {
        string dir = $"{WheelSceneSetup.CONFIGS}/Core";
        WheelSceneSetup.EnsureDir(dir);
        string path = $"{dir}/WheelConfig.asset";
        var cfg = WheelSceneSetup.Load<WheelConfig>(path) ?? WheelSceneSetup.Create<WheelConfig>(path);
        cfg.safeZoneInterval = 5;
        cfg.superZoneInterval = 30;
        cfg.normalZone = zones.normal;
        cfg.safeZone = zones.safe;
        cfg.superZone = zones.super;
        cfg.spinDuration = 3f;
        cfg.minFullRotations = 4f;
        cfg.maxFullRotations = 6f;
        cfg.rewardPopupShowDuration = 0.35f;
        cfg.rewardPopupHoldDuration = 1.0f;
        cfg.reviveCurrencyCost = 25;
        WheelSceneSetup.Dirty(cfg);
        return cfg;
    }

    private static string FolderForVisualCategory(RewardVisualCategory vc)
    {
        switch (vc)
        {
            case RewardVisualCategory.Death:
            case RewardVisualCategory.Cash:
            case RewardVisualCategory.Coin:      return "Currency";
            case RewardVisualCategory.Weapon:    return "Weapons";
            case RewardVisualCategory.Chest:     return "Chests";
            case RewardVisualCategory.Throwable: return "Throwables";
            case RewardVisualCategory.Consumable:return "Consumables";
            case RewardVisualCategory.Cosmetic:
            case RewardVisualCategory.Compact:   return "Cosmetics";
            default: return "Currency";
        }
    }
}
#endif
