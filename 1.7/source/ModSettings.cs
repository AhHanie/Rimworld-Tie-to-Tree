using UnityEngine;
using Verse;

namespace Tie_to_Tree
{
    public class ModSettings : Verse.ModSettings
    {
        public const float DefaultEscapeChancePerDay = 0.03f;

        public static float EscapeChancePerDay = DefaultEscapeChancePerDay;

        public static bool EnableDebugLogging;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EscapeChancePerDay, "escapeChancePerDay", DefaultEscapeChancePerDay);
            Scribe_Values.Look(ref EnableDebugLogging, "enableDebugLogging", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EscapeChancePerDay = Mathf.Clamp01(EscapeChancePerDay);
            }
        }
    }
}
