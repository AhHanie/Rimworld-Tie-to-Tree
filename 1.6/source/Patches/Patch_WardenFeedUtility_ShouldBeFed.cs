using HarmonyLib;
using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(WardenFeedUtility), nameof(WardenFeedUtility.ShouldBeFed))]
    public static class Patch_WardenFeedUtility_ShouldBeFed
    {
        public static void Postfix(Pawn p, ref bool __result)
        {
            if (__result || TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.IsBound(p))
            {
                return;
            }

            if (p.IsPrisonerOfColony && p.guest.CanBeBroughtFood && p.InBed())
            {
                __result = true;
            }
        }
    }
}
