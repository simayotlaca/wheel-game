using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VertigoWheel
{
public static class PlayerProgress
{
    private const string key_cash        = "start_dollars";
    private const string key_coins       = "start_coins";
    private const string key_banked      = "vw.banked";
    private const string key_progression = "vw.progression";
    private const string key_revive_count = "vw.reviveCount";
    //i keep key_has because zero values and no save look the same otherwise
    private const string key_has         = "vw.has";

    private static bool has_save => PlayerPrefs.GetInt(key_has, 0) == 1;

    public static void Save(int cash, int coins, IReadOnlyDictionary<string, int> banked, int revive_count)
    {
        PlayerPrefs.SetInt(key_cash, cash);
        PlayerPrefs.SetInt(key_coins, coins);
        PlayerPrefs.SetString(key_banked, EncodeBanked(banked));
        PlayerPrefs.SetInt(key_revive_count, Mathf.Max(0, revive_count));
        PlayerPrefs.SetInt(key_has, 1);
        PlayerPrefs.Save();
    }

    public static bool Load(out int cash, out int coins, out Dictionary<string, int> banked, out int revive_count)
    {
        if (!has_save)
        {
            cash = 0; coins = 0; banked = null; revive_count = 0;
            return false;
        }
        cash         = Mathf.Max(0, PlayerPrefs.GetInt(key_cash, 0));
        coins        = Mathf.Max(0, PlayerPrefs.GetInt(key_coins, 0));
        banked       = DecodeBanked(PlayerPrefs.GetString(key_banked, string.Empty));
        revive_count = Mathf.Max(0, PlayerPrefs.GetInt(key_revive_count, 0));
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(key_cash);
        PlayerPrefs.DeleteKey(key_coins);
        PlayerPrefs.DeleteKey(key_banked);
        PlayerPrefs.DeleteKey(key_progression);
        PlayerPrefs.DeleteKey(key_revive_count);
        PlayerPrefs.DeleteKey(key_has);
        PlayerPrefs.Save();
    }

    public static Dictionary<string, int> LoadProgression()
    {
        return DecodeBanked(PlayerPrefs.GetString(key_progression, string.Empty));
    }

    public static void SaveProgression(Dictionary<string, int> progression)
    {
        PlayerPrefs.SetString(key_progression, EncodeBanked(progression));
        PlayerPrefs.Save();
    }

    //i used a small id:amount string here, json felt too much for this little save
    private static string EncodeBanked(IReadOnlyDictionary<string, int> banked)
    {
        if (banked.Count != 0)
        {
            var sb = new StringBuilder(banked.Count * 12);
            bool first = true;
            foreach (var kv in banked)
            {
                if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0)
                {
                    if (!first)
                    {
                        sb.Append(';');
                    }
                    sb.Append(kv.Key); sb.Append(':'); sb.Append(kv.Value);
                    first = false;
                }
            }
            return sb.ToString();
        }
        return string.Empty;
    }

    private static Dictionary<string, int> DecodeBanked(string raw)
    {
        var result = new Dictionary<string, int>(8);
        if (!string.IsNullOrEmpty(raw))
        {
            string[] entries = raw.Split(';');
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (!string.IsNullOrEmpty(entry))
                {
                    int sep = entry.IndexOf(':');
                    //need something on both sides of colon or it gets weird
                    if (sep > 0 && sep < entry.Length - 1)
                    {
                        string id = entry.Substring(0, sep);
                        if (int.TryParse(entry.Substring(sep + 1), out int amount) && amount > 0)
                        {
                            result[id] = amount;
                        }
                    }
                }
            }
        }
        return result;
    }
}
}
