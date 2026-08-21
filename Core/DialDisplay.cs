using UnityEngine;

namespace DescendersModMenu
{
    /// <summary>Shared dial labels: stock/default level shows as 0%.</summary>
    public static class DialDisplay
    {
        public static string OffsetPercent(int level, int defaultLevel, int minLevel, int maxLevel)
        {
            if (level == defaultLevel) return "0%";
            float pct;
            if (level > defaultLevel)
            {
                int span = maxLevel - defaultLevel;
                pct = span <= 0 ? 0f : (level - defaultLevel) / (float)span * 100f;
            }
            else
            {
                int span = defaultLevel - minLevel;
                pct = span <= 0 ? 0f : (level - defaultLevel) / (float)span * 100f;
            }
            return Mathf.RoundToInt(pct).ToString("+0;-0") + "%";
        }
    }
}
