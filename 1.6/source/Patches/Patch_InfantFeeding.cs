using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    [HarmonyPatch]
    public static class Patch_InfantFeeding
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(WorkGiver_Breastfeed), nameof(WorkGiver_Breastfeed.JobOnThing));
            yield return AccessTools.Method(typeof(WorkGiver_BottleFeedBaby), nameof(WorkGiver_BottleFeedBaby.JobOnThing));
            yield return AccessTools.Method(typeof(WorkGiver_BreastfeedCarryToMom), nameof(WorkGiver_BreastfeedCarryToMom.JobOnThing));
        }

        public static void Postfix(Thing t, ref Job __result)
        {
            if (__result == null || TreeBindingRegistry.Count == 0)
            {
                return;
            }

            Pawn baby = t as Pawn;
            if (baby == null || !TreeBindingRegistry.IsBound(baby))
            {
                return;
            }

            JobDef def = __result.def;
            if (def == JobDefOf.Breastfeed)
            {
                Job replacement = JobMaker.MakeJob(TietoTreeDefOf.TietoTree_FeedTiedInfant, baby);
                replacement.count = 1;
                __result = replacement;
            }
            else if (def == JobDefOf.BottleFeedBaby)
            {
                Job replacement = JobMaker.MakeJob(TietoTreeDefOf.TietoTree_FeedTiedInfant, baby, __result.targetB);
                replacement.count = 1;
                __result = replacement;
            }
            else if (def == JobDefOf.BreastfeedCarryToMom)
            {
                __result = null;
            }
        }
    }
}
