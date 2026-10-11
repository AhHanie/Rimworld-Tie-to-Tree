using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(WorkGiver_Warden_TakeToBed), nameof(WorkGiver_Warden_TakeToBed.TryMakeJob))]
    public static class Patch_WardenTakeToBed_TryMakeJob
    {
        public static bool Prefix(Thing t, ref Job __result)
        {
            if (TreeBindingRegistry.Count == 0)
            {
                return true;
            }

            Pawn prisoner = t as Pawn;
            if (prisoner == null || !TreeBindingRegistry.IsBound(prisoner))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }
}
