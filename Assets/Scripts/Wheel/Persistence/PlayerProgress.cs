using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class PlayerProgress
{
    private const string KeyCash   = "vw.cash";
    private const string KeyCoins  = "vw.coins";
    private const string KeyBanked = "vw.banked";
    private const string KeyHas    = "vw.has";

    public static bool HasSave => PlayerPrefs.GetInt(KeyHas, 0) == 1;

    public static void Save(int cash, int coins, IReadOnlyDictionary<string, int> banked)
    {
        PlayerPrefs.SetInt(KeyCash, cash);
        PlayerPrefs.SetInt(KeyCoins, coins);
        PlayerPrefs.SetString(KeyBanked, EncodeBanked(banked));
        PlayerPrefs.SetInt(KeyHas, 1);
        PlayerPrefs.Save();
    }

    public static bool Load(out int cash, out int coins, out Dictionary<string, int> banked)
    {
        if (!HasSave)
        {
            cash = 0; coins = 0; banked = null;
            return false;
        }
        cash   = Mathf.Max(0, PlayerPrefs.GetInt(KeyCash, 0));
        coins  = Mathf.Max(0, PlayerPrefs.GetInt(KeyCoins, 0));
        banked = DecodeBanked(PlayerPrefs.GetString(KeyBanked, string.Empty));
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KeyCash);
        PlayerPrefs.DeleteKey(KeyCoins);
        PlayerPrefs.DeleteKey(KeyBanked);
        PlayerPrefs.DeleteKey(KeyHas);
        PlayerPrefs.Save();
    }

    private static string EncodeBanked(IReadOnlyDictionary<string, int> banked)
    {
        if (banked == null || banked.Count == 0) return string.Empty;
        var sb = new StringBuilder(banked.Count * 12);
        bool first = true;
        foreach (var kv in banked)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Value <= 0) continue;
            if (!first) sb.Append(';');
            sb.Append(kv.Key); sb.Append(':'); sb.Append(kv.Value);
            first = false;
        }
        return sb.ToString();
    }

    private static Dictionary<string, int> DecodeBanked(string raw)
    {
        var result = new Dictionary<string, int>(8);
        if (string.IsNullOrEmpty(raw)) return result;

        string[] entries = raw.Split(';');
        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i];
            if (string.IsNullOrEmpty(entry)) continue;
            int sep = entry.IndexOf(':');
            if (sep <= 0 || sep >= entry.Length - 1) continue;
            string id = entry.Substring(0, sep);
            if (!int.TryParse(entry.Substring(sep + 1), out int amount)) continue;
            if (amount <= 0) continue;
            result[id] = amount;
        }
        return result;
    }
}
