using HarmonyLib;
using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(WardenFeedUtility), nameof(WardenFeedUtility.ShouldBeFed))]
    public static class Patch_WardenFeedUtility_ShouldBeFed
    {
        [HarmonyInfix(typeof(HealthAIUtility), nameof(HealthAIUtility.ShouldSeekMedicalRest))]
        public static bool Postfix(bool __result, [HarmonyOuter] Pawn p)
        {
            return __result || TreeBindingRegistry.IsBound(p);
        }
    }
}
