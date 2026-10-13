using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(WorkGiver_Breastfeed), nameof(WorkGiver_Breastfeed.JobOnThing))]
    public static class Patch_InfantFeeding_Breastfeed
    {
        [HarmonyInfix(typeof(ChildcareUtility), nameof(ChildcareUtility.MakeBreastfeedJob))]
        public static Job Postfix(Job __result, Pawn baby)
        {
            if (!TreeBindingRegistry.IsBound(baby))
            {
                return __result;
            }

            Job replacement = JobMaker.MakeJob(TietoTreeDefOf.TietoTree_FeedTiedInfant, baby);
            replacement.count = 1;
            return replacement;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_BottleFeedBaby), nameof(WorkGiver_BottleFeedBaby.JobOnThing))]
    public static class Patch_InfantFeeding_BottleFeed
    {
        [HarmonyInfix(typeof(ChildcareUtility), nameof(ChildcareUtility.MakeBottlefeedJob))]
        public static Job Postfix(Job __result, Pawn baby, Thing foodSource)
        {
            if (!TreeBindingRegistry.IsBound(baby))
            {
                return __result;
            }

            Job replacement = JobMaker.MakeJob(TietoTreeDefOf.TietoTree_FeedTiedInfant, baby, foodSource);
            replacement.count = 1;
            return replacement;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_BreastfeedCarryToMom), nameof(WorkGiver_BreastfeedCarryToMom.JobOnThing))]
    public static class Patch_InfantFeeding_CarryToMom
    {
        [HarmonyInfix(typeof(ChildcareUtility), nameof(ChildcareUtility.MakeBreastfeedCarryToMomJob))]
        public static Job Postfix(Job __result, Pawn baby)
        {
            return TreeBindingRegistry.IsBound(baby) ? null : __result;
        }
    }
}
