using HarmonyLib;
using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedNow))]
    public static class Patch_RestUtility_CanUseBedNow
    {
        public static bool Prefix(Thing bedThing, Pawn sleeper, ref bool __result)
        {
            Building_TreeRestraint anchor = bedThing as Building_TreeRestraint;
            if (anchor == null)
            {
                return true;
            }

            __result = TreeBindingRegistry.CanUseAnchor(anchor, sleeper);
            return false;
        }
    }
}
